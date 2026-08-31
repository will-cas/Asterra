using System;
using System.Collections.Generic;
using System.IO;
using Asterra.Core.World;
using UnityEngine;

namespace Asterra.Gameplay.Content
{
    /// <summary>
    /// Discovers built-in + designer maps. Custom maps: Assets/Asterra/Shared/Maps/*.map.json
    /// </summary>
    public static class MapCatalog
    {
        public const string MundorCapitalId = "mundor_capital";
        public const string OutcastCampId = "outcast_camp";
        public const string RiverCrossingId = "river_crossing";
        public const string FrozenWastesId = "frozen_wastes";
        public const string LushForestId = "lush_forest";
        public const string TwinCitiesId = "twin_cities";
        public const string AncientRelicId = "ancient_relic";

        public readonly struct Choice
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly bool IsBuiltin;
            public readonly SkirmishMapId BuiltinId;

            public Choice(string id, string displayName, bool isBuiltin, SkirmishMapId builtinId)
            {
                Id = id;
                DisplayName = displayName;
                IsBuiltin = isBuiltin;
                BuiltinId = builtinId;
            }
        }

        public static string SharedMapsDirectory
        {
            get
            {
#if UNITY_EDITOR
                return Path.Combine(Application.dataPath, "Asterra", "Shared", "Maps");
#else
                return Path.Combine(Application.streamingAssetsPath, "Asterra", "Maps");
#endif
            }
        }

        public static string StreamingMapsDirectory =>
            Path.Combine(Application.streamingAssetsPath, "Asterra", "Maps");

        public static IReadOnlyList<Choice> ListChoices()
        {
            var list = new List<Choice>(12)
            {
                BuiltinChoice(SkirmishMapId.LushForest),
                BuiltinChoice(SkirmishMapId.RiverCrossing),
                BuiltinChoice(SkirmishMapId.OutcastCamp),
                BuiltinChoice(SkirmishMapId.TwinCities),
                BuiltinChoice(SkirmishMapId.FrozenWastes),
                BuiltinChoice(SkirmishMapId.AncientRelic),
                BuiltinChoice(SkirmishMapId.MundorCapital),
            };

            foreach (var path in EnumerateMapFiles())
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var def = JsonUtility.FromJson<MapDefinition>(json);
                    if (def == null || string.IsNullOrEmpty(def.id))
                        continue;
                    if (IsBuiltinId(def.id))
                        continue;
                    string name = string.IsNullOrEmpty(def.displayName) ? def.id : def.displayName;
                    list.Add(new Choice(def.id, name + " ★", false, SkirmishMapId.LushForest));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Asterra] Failed to read map {path}: {e.Message}");
                }
            }

            return list;
        }

        public static Choice Next(Choice current)
        {
            var all = ListChoices();
            if (all.Count == 0)
                return BuiltinChoice(SkirmishMapId.LushForest);
            int idx = 0;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Id == current.Id)
                {
                    idx = i;
                    break;
                }
            }

            return all[(idx + 1) % all.Count];
        }

        public static Choice BuiltinChoice(SkirmishMapId id)
        {
            var def = BuiltinMaps.Definition(id);
            return new Choice(def.id, def.displayName, true, id);
        }

        public static Choice FromId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BuiltinChoice(SkirmishMapId.LushForest);
            var all = ListChoices();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Id == id)
                    return all[i];
            }

            if (TryParseBuiltin(id, out var builtin))
                return BuiltinChoice(builtin);
            return BuiltinChoice(SkirmishMapId.LushForest);
        }

        public static bool IsBuiltinId(string id)
        {
            return TryParseBuiltin(id, out _);
        }

        public static bool TryParseBuiltin(string id, out SkirmishMapId map)
        {
            switch (id)
            {
                case MundorCapitalId:
                    map = SkirmishMapId.MundorCapital;
                    return true;
                case OutcastCampId:
                    map = SkirmishMapId.OutcastCamp;
                    return true;
                case RiverCrossingId:
                    map = SkirmishMapId.RiverCrossing;
                    return true;
                case FrozenWastesId:
                    map = SkirmishMapId.FrozenWastes;
                    return true;
                case LushForestId:
                    map = SkirmishMapId.LushForest;
                    return true;
                case TwinCitiesId:
                    map = SkirmishMapId.TwinCities;
                    return true;
                case AncientRelicId:
                    map = SkirmishMapId.AncientRelic;
                    return true;
                default:
                    map = SkirmishMapId.LushForest;
                    return false;
            }
        }

        public static bool TryLoad(string id, out MapDefinition definition)
        {
            definition = null;
            if (string.IsNullOrEmpty(id) || IsBuiltinId(id))
                return false;

            foreach (var path in EnumerateMapFiles())
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var def = JsonUtility.FromJson<MapDefinition>(json);
                    if (def == null || def.id != id)
                        continue;
                    def.EnsureArrays();
                    definition = def;
                    return true;
                }
                catch
                {
                    // try next
                }
            }

            return false;
        }

        public static string Save(MapDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            definition.EnsureArrays();
            if (string.IsNullOrEmpty(definition.id))
                definition.id = "new_map";
            definition.id = SanitizeId(definition.id);

            string fileName = definition.id + ".map.json";
            string sharedDir = SharedMapsDirectory;
            Directory.CreateDirectory(sharedDir);
            string sharedPath = Path.Combine(sharedDir, fileName);
            string json = JsonUtility.ToJson(definition, true);
            File.WriteAllText(sharedPath, json);

            try
            {
                string streamDir = StreamingMapsDirectory;
                Directory.CreateDirectory(streamDir);
                File.WriteAllText(Path.Combine(streamDir, fileName), json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Asterra] Could not mirror map to StreamingAssets: {e.Message}");
            }

            return sharedPath;
        }

        public static IEnumerable<string> EnumerateMapFiles()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var dir in new[] { SharedMapsDirectory, StreamingMapsDirectory })
            {
                if (!Directory.Exists(dir))
                    continue;
                string[] files;
                try
                {
                    files = Directory.GetFiles(dir, "*.map.json");
                }
                catch
                {
                    continue;
                }

                for (int i = 0; i < files.Length; i++)
                {
                    string name = Path.GetFileName(files[i]);
                    if (!seen.Add(name))
                        continue;
                    yield return files[i];
                }
            }
        }

        public static string SanitizeId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "new_map";
            var chars = id.Trim().ToLowerInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '_')
                    chars[i] = '_';
            }

            return new string(chars);
        }

        public static int KeepCount(string mapId)
        {
            var def = ResolveDefinition(mapId);
            return def?.keeps != null ? def.keeps.Length : 2;
        }

        public static MapDefinition ResolveDefinition(string mapId)
        {
            if (TryParseBuiltin(mapId, out var builtin))
                return BuiltinMaps.Definition(builtin);
            if (TryLoad(mapId, out var custom))
                return custom;
            return BuiltinMaps.Definition(SkirmishMapId.LushForest);
        }
    }
}

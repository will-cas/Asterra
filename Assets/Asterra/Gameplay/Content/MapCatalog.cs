using System;
using System.Collections.Generic;
using System.IO;
using Asterra.Core.World;
using UnityEngine;

namespace Asterra.Gameplay.Content
{
    /// <summary>
    /// Discovers built-in + designer maps. Custom maps: Assets/Asterra/Shared/Maps/*.map.json
    /// (copied to StreamingAssets for builds).
    /// </summary>
    public static class MapCatalog
    {
        public const string TwinKeepsId = "twin_keeps";
        public const string RiverCrossingId = "river_crossing";
        public const string BlackridgePassId = "blackridge_pass";

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
            var list = new List<Choice>(8)
            {
                new Choice(TwinKeepsId, "Twin Keeps", true, SkirmishMapId.TwinKeeps),
                new Choice(RiverCrossingId, "River Crossing", true, SkirmishMapId.RiverCrossing),
                new Choice(BlackridgePassId, "Blackridge Pass", true, SkirmishMapId.BlackridgePass),
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
                    list.Add(new Choice(def.id, name + " ★", false, SkirmishMapId.TwinKeeps));
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
                return BuiltinChoice(SkirmishMapId.BlackridgePass);
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
            switch (id)
            {
                case SkirmishMapId.TwinKeeps:
                    return new Choice(TwinKeepsId, "Twin Keeps", true, id);
                case SkirmishMapId.RiverCrossing:
                    return new Choice(RiverCrossingId, "River Crossing", true, id);
                default:
                    return new Choice(BlackridgePassId, "Blackridge Pass", true, SkirmishMapId.BlackridgePass);
            }
        }

        public static Choice FromId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BuiltinChoice(SkirmishMapId.BlackridgePass);
            var all = ListChoices();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Id == id)
                    return all[i];
            }

            if (TryParseBuiltin(id, out var builtin))
                return BuiltinChoice(builtin);
            return BuiltinChoice(SkirmishMapId.BlackridgePass);
        }

        public static bool IsBuiltinId(string id)
        {
            return id == TwinKeepsId || id == RiverCrossingId || id == BlackridgePassId;
        }

        public static bool TryParseBuiltin(string id, out SkirmishMapId map)
        {
            switch (id)
            {
                case TwinKeepsId:
                    map = SkirmishMapId.TwinKeeps;
                    return true;
                case RiverCrossingId:
                    map = SkirmishMapId.RiverCrossing;
                    return true;
                case BlackridgePassId:
                    map = SkirmishMapId.BlackridgePass;
                    return true;
                default:
                    map = SkirmishMapId.TwinKeeps;
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
    }
}

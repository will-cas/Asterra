using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Loads exported Blender PBR maps and builds lit materials for units/buildings/props.</summary>
    public static class AsterraPbrLibrary
    {
        public readonly struct Maps
        {
            public readonly Texture2D Albedo;
            public readonly Texture2D Roughness;
            public readonly Texture2D Normal;

            public Maps(Texture2D albedo, Texture2D roughness, Texture2D normal)
            {
                Albedo = albedo;
                Roughness = roughness;
                Normal = normal;
            }

            public bool Valid => Albedo != null;
        }

        private static readonly Dictionary<string, Texture2D> TexCache = new();
        private static readonly Dictionary<string, Maps> SetCache = new();
        private static Shader _lit;

        public static Material CreateLit(Color tint, string setKey, float metallic = 0.04f)
        {
            var maps = GetMaps(setKey);
            return CreateLit(tint, maps, metallic);
        }

        public static Material CreateLit(Color tint, Maps maps, float metallic = 0.04f)
        {
            if (_lit == null)
                _lit = Shader.Find("Asterra/LitPBR");
            if (_lit == null)
                _lit = Shader.Find("Universal Render Pipeline/Lit");
            if (_lit == null)
                _lit = Shader.Find("Asterra/UnlitColor");

            var mat = new Material(_lit);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", tint);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", tint);
            if (maps.Albedo != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", maps.Albedo);
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", maps.Albedo);
            }

            if (maps.Normal != null && mat.HasProperty("_BumpMap"))
                mat.SetTexture("_BumpMap", maps.Normal);
            if (maps.Roughness != null && mat.HasProperty("_RoughnessMap"))
                mat.SetTexture("_RoughnessMap", maps.Roughness);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_TexBlend"))
                mat.SetFloat("_TexBlend", maps.Valid ? 1f : 0f);
            if (mat.HasProperty("_BumpScale"))
                mat.SetFloat("_BumpScale", 1f);
            if (mat.HasProperty("_UvScale") && maps.Valid)
                mat.SetFloat("_UvScale", 0.38f);
            if (mat.HasProperty("_TeamColor"))
                mat.SetColor("_TeamColor", Color.white);
            if (mat.HasProperty("_TeamCloth"))
                mat.SetFloat("_TeamCloth", 0f);
            return mat;
        }

        public static float TeamClothWeight(string setKey)
        {
            if (string.IsNullOrEmpty(setKey))
                return 0.35f;
            if (setKey.StartsWith("cloth"))
                return 1f;
            if (setKey == "leather")
                return 0.55f;
            if (setKey == "leaf")
                return 0.4f;
            if (setKey == "ice" || setKey == "glass")
                return 0.35f;
            if (setKey == "plaster" || setKey == "pale_wood")
                return 0.5f;
            if (setKey == "marble")
                return 0.35f;
            if (setKey == "stone_brick" || setKey == "red_brick" || setKey == "slate")
                return 0.22f;
            if (setKey == "steel" || setKey == "iron")
                return 0.18f;
            return 0.3f;
        }

        public static void ApplyTeamDye(Material mat, Color team, bool building, string setKey, Mesh mesh)
        {
            if (mat == null)
                return;
            if (mat.HasProperty("_TeamColor"))
                mat.SetColor("_TeamColor", team);
            if (mat.HasProperty("_TeamCloth"))
                mat.SetFloat("_TeamCloth", TeamClothWeight(setKey));
            if (mat.HasProperty("_TeamBuilding"))
                mat.SetFloat("_TeamBuilding", building ? 1f : 0f);
            if (mat.HasProperty("_TeamBounds") && mesh != null)
            {
                Bounds b = mesh.bounds;
                mat.SetVector("_TeamBounds", new Vector4(b.min.y, b.max.y, 0f, 0f));
            }
        }

        public static Maps GetMaps(string setKey)
        {
            if (string.IsNullOrEmpty(setKey))
                setKey = "plaster";
            if (SetCache.TryGetValue(setKey, out var cached) && cached.Albedo != null)
                return cached;

            var maps = new Maps(
                LoadPng(setKey + "_albedo", linear: false),
                LoadPng(setKey + "_rough", linear: true),
                LoadPng(setKey + "_normal", linear: true));
            SetCache[setKey] = maps;
            return maps;
        }

        public static string BodySetKey(bool isUnit, string definitionId)
        {
            if (!isUnit)
            {
                if (!string.IsNullOrEmpty(definitionId) && definitionId.Contains("university"))
                    return "marble";
                if (!string.IsNullOrEmpty(definitionId) && (definitionId.Contains("veiled")
                    || definitionId.Contains("arcane") || definitionId.Contains("portal")
                    || definitionId.Contains("shadowed") || definitionId.Contains("watchtower")))
                    return "steel";
                if (!string.IsNullOrEmpty(definitionId) && definitionId.Contains("outcast"))
                    return "marble";
                if (!string.IsNullOrEmpty(definitionId) && definitionId.Contains("freetown"))
                    return "stone_brick";
                if (!string.IsNullOrEmpty(definitionId) && definitionId.Contains("church"))
                    return "marble";
                if (AsterraMeshLibrary.IsWall(definitionId))
                    return "stone_brick";
                if (!string.IsNullOrEmpty(definitionId) && definitionId.Contains("turret"))
                    return "steel";
                if (AsterraMeshLibrary.IsKeep(definitionId))
                    return "marble";
                return "stone_brick";
            }

            if (string.IsNullOrEmpty(definitionId))
                return "cloth";
            if (definitionId.Contains("spider") || definitionId.Contains("airship")
                || definitionId.Contains("golem") || definitionId.Contains("colossus")
                || definitionId.Contains("earth_breaker") || definitionId.Contains("clock"))
                return "steel";
            if (definitionId.Contains("frost") || definitionId.Contains("ice")
                || definitionId.Contains("wold") || definitionId.Contains("cub"))
                return "ice";
            if (definitionId.Contains("elemental") || definitionId.Contains("shade")
                || definitionId.Contains("souling"))
                return "glass";
            if (definitionId.StartsWith("unit_veiled"))
                return "cloth_purple";
            if (definitionId.StartsWith("unit_outcast"))
                return "cloth_green";
            if (definitionId.StartsWith("unit_freetown"))
                return "cloth_blue";
            if (definitionId.StartsWith("unit_church"))
                return "cloth_sun";
            if (definitionId.StartsWith("unit_university"))
                return "cloth_deep";
            if (definitionId.StartsWith("unit_royal") || definitionId.Contains("legion")
                || definitionId.Contains("guard") || definitionId.Contains("knight"))
                return "steel";
            if (definitionId.Contains("dryad") || definitionId.Contains("sprite"))
                return "leaf";
            if (definitionId.Contains("ember") || definitionId.Contains("golem") || definitionId.Contains("siege")
                || definitionId.Contains("catapult") || definitionId.Contains("onager"))
                return "iron";
            if (definitionId.Contains("cavalry") || definitionId.Contains("rider") || definitionId.Contains("knight"))
                return "steel";
            if (definitionId.Contains("builder") || definitionId.Contains("archer") || definitionId.Contains("ranger"))
                return "leather";
            if (definitionId.Contains("mage") || definitionId.Contains("caster") || definitionId.Contains("priest")
                || definitionId.Contains("leader") || definitionId.Contains("king") || definitionId.Contains("heir"))
                return "cloth_deep";
            return "cloth";
        }

        public static string PropSetKey(string kind)
        {
            return kind switch
            {
                "leaf" or "canopy" or "bush" => "leaf",
                "bark" or "timber" => "bark",
                "gold" => "crystal",
                "bridge" => "wood",
                "rock" => "slate",
                _ => "plaster",
            };
        }

        public static float MetallicForSet(string setKey)
        {
            if (setKey == "gold" || setKey == "crystal")
                return 0.55f;
            if (setKey == "iron" || setKey == "steel")
                return 0.22f;
            if (setKey == "glass")
                return 0.08f;
            if (setKey == "ice")
                return 0.12f;
            return 0.04f;
        }

        private static Texture2D LoadPng(string fileStem, bool linear)
        {
            if (TexCache.TryGetValue(fileStem, out var hit) && hit != null)
                return hit;

            string path = Path.Combine(Application.dataPath, "Asterra", "Shared", "Art", "Textures", fileStem + ".png");
            if (!File.Exists(path))
                return null;

            byte[] bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true, linear);
            if (!tex.LoadImage(bytes, markNonReadable: true))
            {
                Object.Destroy(tex);
                return null;
            }

            tex.name = fileStem;
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 4;
            TexCache[fileStem] = tex;
            return tex;
        }
    }
}

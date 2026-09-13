using System.IO;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Loads authored menu stills from Assets/Asterra/Shared/Art/UI/Menu.</summary>
    public static class MenuArt
    {
        private static Texture2D _hubDiorama;
        private static Texture2D _blackridgePreview;
        private static bool _hubTried;
        private static bool _mapTried;

        public static Texture2D HubDiorama
        {
            get
            {
                if (!_hubTried)
                {
                    _hubTried = true;
                    _hubDiorama = LoadPng("hub_diorama_blackridge.png");
                }
                return _hubDiorama;
            }
        }

        public static Texture2D BlackridgeMapPreview
        {
            get
            {
                if (!_mapTried)
                {
                    _mapTried = true;
                    _blackridgePreview = LoadPng("map_preview_blackridge.png");
                }
                return _blackridgePreview;
            }
        }

        private static Texture2D LoadPng(string fileName)
        {
            string path = Path.Combine(
                Application.dataPath, "Asterra", "Shared", "Art", "UI", "Menu", fileName);
            if (!File.Exists(path))
                return null;
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = Path.GetFileNameWithoutExtension(fileName),
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                if (!tex.LoadImage(bytes))
                {
                    Object.Destroy(tex);
                    return null;
                }
                return tex;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Asterra] Menu art load failed for {fileName}: {ex.Message}");
                return null;
            }
        }
    }
}

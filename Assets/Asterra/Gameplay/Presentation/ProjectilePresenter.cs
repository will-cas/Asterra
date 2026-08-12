using System.Collections.Generic;
using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Client-only visual for in-flight sim projectiles.</summary>
    public sealed class ProjectilePresenter : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;

        private readonly List<GameObject> _pool = new();
        private Transform _root;
        private Material _mat;

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
            var go = new GameObject("Projectiles");
            go.transform.SetParent(transform, false);
            _root = go.transform;
            EnsureMaterial();
        }

        private void LateUpdate()
        {
            if (match == null || match.World == null)
                return;

            var projectiles = match.World.Projectiles;
            int needed = projectiles != null ? projectiles.Count : 0;
            EnsurePool(needed);

            for (int i = 0; i < _pool.Count; i++)
            {
                if (i >= needed)
                {
                    _pool[i].SetActive(false);
                    continue;
                }

                var p = projectiles[i];
                var go = _pool[i];
                go.SetActive(true);
                Vector3 from = new Vector3(p.X, 6f, p.Z);
                Vector3 to = new Vector3(p.TargetX, 6f, p.TargetZ);
                Vector3 mid = (from + to) * 0.5f;
                float len = Vector3.Distance(from, to);
                go.transform.position = mid;
                if (len > 0.01f)
                    go.transform.rotation = Quaternion.LookRotation(to - from, Vector3.up);
                go.transform.localScale = new Vector3(0.7f, 0.7f, Mathf.Max(1.2f, len));
            }
        }

        private void EnsurePool(int needed)
        {
            while (_pool.Count < needed)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.Destroy(go.GetComponent<Collider>());
                go.name = "ProjectileFx";
                go.transform.SetParent(_root, false);
                var rend = go.GetComponent<Renderer>();
                if (_mat != null)
                    rend.sharedMaterial = _mat;
                go.SetActive(false);
                _pool.Add(go);
            }
        }

        private void EnsureMaterial()
        {
            if (_mat != null)
                return;
            var shader = Shader.Find("Asterra/UnlitColor")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            if (shader == null)
                return;
            _mat = new Material(shader);
            var color = new Color(0.95f, 0.85f, 0.35f, 1f);
            if (_mat.HasProperty("_BaseColor"))
                _mat.SetColor("_BaseColor", color);
            if (_mat.HasProperty("_Color"))
                _mat.SetColor("_Color", color);
        }
    }
}

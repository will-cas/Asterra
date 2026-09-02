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
        private readonly List<bool> _wasActive = new();
        private readonly List<Vector3> _lastPos = new();
        private Transform _root;
        private Material _arrowMat;
        private Material _boltMat;
        private Material _rockMat;

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
            var go = new GameObject("Projectiles");
            go.transform.SetParent(transform, false);
            _root = go.transform;
            _arrowMat = MakeMat(new Color(0.95f, 0.85f, 0.35f, 1f));
            _boltMat = MakeMat(new Color(0.45f, 0.85f, 1f, 1f));
            _rockMat = MakeMat(new Color(0.55f, 0.38f, 0.22f, 1f));
        }

        private void LateUpdate()
        {
            if (match == null || match.World == null)
                return;

            var projectiles = match.World.Projectiles;
            int needed = projectiles != null ? projectiles.Count : 0;
            EnsurePool(needed);

            float groundY = 6f;
            for (int i = 0; i < _pool.Count; i++)
            {
                var go = _pool[i];
                if (i >= needed)
                {
                    if (_wasActive[i])
                        SpawnImpact(_lastPos[i]);
                    go.SetActive(false);
                    _wasActive[i] = false;
                    continue;
                }

                var p = projectiles[i];
                Vector3 from = new Vector3(p.X, groundY, p.Z);
                Vector3 to = new Vector3(p.TargetX, groundY, p.TargetZ);
                float remaining = Vector3.Distance(from, to);
                float arc = Mathf.Min(7f, remaining * 0.12f);
                from.y += arc;
                go.SetActive(true);
                go.transform.position = from;
                if (remaining > 0.01f)
                    go.transform.rotation = Quaternion.LookRotation(to - from, Vector3.up)
                                           * Quaternion.Euler(90f, 0f, Time.time * 720f);
                go.transform.localScale = RoleScale(p.Role);
                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                    rend.sharedMaterial = RoleMat(p.Role);
                _wasActive[i] = true;
                _lastPos[i] = from;
            }
        }

        private static Vector3 RoleScale(UnitRole role)
        {
            return role switch
            {
                UnitRole.Siege => new Vector3(1.1f, 1.1f, 1.6f),
                UnitRole.Ranged => new Vector3(0.35f, 0.35f, 1.35f),
                _ => new Vector3(0.45f, 0.45f, 0.9f),
            };
        }

        private Material RoleMat(UnitRole role)
        {
            return role switch
            {
                UnitRole.Siege => _rockMat,
                UnitRole.Ranged => _boltMat,
                _ => _arrowMat,
            };
        }

        private void SpawnImpact(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "ProjectileImpact";
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.9f;
            var rend = go.GetComponent<Renderer>();
            if (_arrowMat != null)
                rend.sharedMaterial = _arrowMat;
            Object.Destroy(go, 0.22f);
        }

        private void EnsurePool(int needed)
        {
            while (_pool.Count < needed)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.Destroy(go.GetComponent<Collider>());
                go.name = "ProjectileFx";
                go.transform.SetParent(_root, false);
                go.SetActive(false);
                _pool.Add(go);
                _wasActive.Add(false);
                _lastPos.Add(Vector3.zero);
            }
        }

        private static Material MakeMat(Color color)
        {
            var shader = Shader.Find("Asterra/UnlitColor")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            if (shader == null)
                return null;
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            return mat;
        }
    }
}

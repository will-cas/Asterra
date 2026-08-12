using System.Collections.Generic;
using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Client-only combat juice: hit flash and brief death burst.</summary>
    public sealed class CombatFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private float deathBurstSeconds = 0.35f;

        private readonly List<DeathBurst> _bursts = new();
        private int _handledTick = int.MinValue;

        private struct DeathBurst
        {
            public GameObject Go;
            public float ExpireAt;
        }

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
        }

        private void LateUpdate()
        {
            if (match == null || match.World == null)
                return;

            int tick = match.Clock != null ? (int)match.Clock.CurrentTick.Value : -1;
            if (tick != _handledTick)
            {
                _handledTick = tick;
                var events = match.World.CombatEvents;
                if (events != null && events.Count > 0)
                {
                    var views = FindObjectsByType<EntityView>(FindObjectsSortMode.None);
                    for (int i = 0; i < events.Count; i++)
                    {
                        var ev = events[i];
                        if (ev.Kind == CombatEventKind.Hit)
                        {
                            var view = FindView(views, ev.TargetId);
                            if (view != null)
                                view.SetHitFlash();
                        }
                        else if (ev.Kind == CombatEventKind.Death)
                        {
                            SpawnDeathBurst(ev.X, ev.Z);
                        }
                    }
                }
            }

            float now = Time.time;
            for (int i = _bursts.Count - 1; i >= 0; i--)
            {
                if (now < _bursts[i].ExpireAt)
                    continue;
                if (_bursts[i].Go != null)
                    Destroy(_bursts[i].Go);
                _bursts.RemoveAt(i);
            }
        }

        private void SpawnDeathBurst(float x, float z)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "DeathBurst";
            go.transform.position = new Vector3(x, 4f, z);
            go.transform.localScale = new Vector3(6f, 6f, 6f);
            var rend = go.GetComponent<Renderer>();
            var shader = Shader.Find("Asterra/UnlitColor")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var mat = new Material(shader);
                var color = new Color(0.95f, 0.2f, 0.15f, 0.9f);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", color);
                rend.sharedMaterial = mat;
            }

            _bursts.Add(new DeathBurst { Go = go, ExpireAt = Time.time + deathBurstSeconds });
        }

        private static EntityView FindView(EntityView[] views, SimEntityId id)
        {
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] != null && views[i].Id == id)
                    return views[i];
            }

            return null;
        }
    }
}

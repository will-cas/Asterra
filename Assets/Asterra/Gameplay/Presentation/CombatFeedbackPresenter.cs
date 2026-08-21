using System.Collections.Generic;
using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Client-only combat juice: hit flash, death burst, deposit/build pulses.</summary>
    public sealed class CombatFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private float deathBurstSeconds = 0.35f;
        [SerializeField] private float pulseSeconds = 0.45f;

        private readonly List<FxBurst> _bursts = new();
        private int _handledTick = int.MinValue;

        private struct FxBurst
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
                        switch (ev.Kind)
                        {
                            case CombatEventKind.Hit:
                            {
                                var view = FindView(views, ev.TargetId);
                                if (view != null)
                                    view.SetHitFlash();
                                break;
                            }
                            case CombatEventKind.Death:
                                SpawnBurst(ev.X, ev.Z, new Color(0.95f, 0.2f, 0.15f, 0.9f), 6f, deathBurstSeconds);
                                break;
                            case CombatEventKind.WorldDestroyed:
                                SpawnBurst(ev.X, ev.Z, new Color(0.55f, 0.35f, 0.15f, 0.95f), 7f, deathBurstSeconds);
                                break;
                            case CombatEventKind.Deposit:
                                SpawnBurst(ev.X, ev.Z, new Color(0.25f, 0.95f, 0.4f, 0.9f), 4.5f, pulseSeconds);
                                break;
                            case CombatEventKind.BuildComplete:
                                SpawnBurst(ev.X, ev.Z, new Color(0.2f, 0.95f, 0.95f, 0.95f), 8f, pulseSeconds);
                                break;
                            default:
                                throw new System.ArgumentOutOfRangeException(nameof(ev.Kind), ev.Kind, null);
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

        private void SpawnBurst(float x, float z, Color color, float size, float life)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "CombatPulse";
            go.transform.position = new Vector3(x, 4f, z);
            go.transform.localScale = new Vector3(size, size, size);
            var rend = go.GetComponent<Renderer>();
            var shader = Shader.Find("Asterra/UnlitColor")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var mat = new Material(shader);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", color);
                rend.sharedMaterial = mat;
            }

            _bursts.Add(new FxBurst { Go = go, ExpireAt = Time.time + life });
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

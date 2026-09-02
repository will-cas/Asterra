using System.Collections.Generic;
using Asterra.Core;
using Asterra.Gameplay.Audio;
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
        private bool _endPosesApplied;

        private struct FxBurst
        {
            public GameObject Go;
            public float ExpireAt;
            public float BornAt;
            public float Life;
            public float Size;
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
                                AsterraAudio.Play(AsterraSfx.Hit, 0.55f);
                                break;
                            }
                            case CombatEventKind.Death:
                            {
                                var deathView = FindView(views, ev.TargetId);
                                if (deathView != null)
                                {
                                    if (deathView.IsUnit)
                                        deathView.BeginDeath();
                                    else
                                        deathView.BeginCollapse();
                                }
                                SpawnBurst(ev.X, ev.Z, new Color(0.95f, 0.2f, 0.15f, 0.9f),
                                    ev.IsBuilding ? 9f : 6f, deathBurstSeconds);
                                AsterraAudio.Play(AsterraSfx.Death, 0.8f);
                                break;
                            }
                            case CombatEventKind.WorldDestroyed:
                                SpawnBurst(ev.X, ev.Z, new Color(0.55f, 0.35f, 0.15f, 0.95f), 7f, deathBurstSeconds);
                                AsterraAudio.Play(AsterraSfx.Death, 0.7f);
                                break;
                            case CombatEventKind.CaptureStarted:
                            case CombatEventKind.CaptureContested:
                                SpawnBurst(ev.X, ev.Z, new Color(0.95f, 0.85f, 0.2f, 0.9f), 5f, pulseSeconds);
                                AsterraAudio.Play(AsterraSfx.Capture, 0.5f);
                                break;
                            case CombatEventKind.CaptureCompleted:
                                SpawnBurst(ev.X, ev.Z, new Color(0.3f, 0.85f, 1f, 0.95f), 7f, pulseSeconds);
                                AsterraAudio.Play(AsterraSfx.Capture, 0.85f);
                                break;
                            case CombatEventKind.CaptureLost:
                                SpawnBurst(ev.X, ev.Z, new Color(0.9f, 0.35f, 0.2f, 0.9f), 6f, pulseSeconds);
                                AsterraAudio.Play(AsterraSfx.Invalid, 0.6f);
                                break;
                            case CombatEventKind.Deposit:
                                SpawnBurst(ev.X, ev.Z, new Color(0.25f, 0.95f, 0.4f, 0.9f), 4.5f, pulseSeconds);
                                AsterraAudio.Play(AsterraSfx.Deposit, 0.65f);
                                break;
                            case CombatEventKind.BuildComplete:
                            {
                                var built = FindView(views, ev.TargetId);
                                if (built != null && !built.IsUnit)
                                    built.PlayBuildCompleteFlash();
                                SpawnBurst(ev.X, ev.Z, new Color(0.2f, 0.95f, 0.95f, 0.95f), 8f, pulseSeconds);
                                AsterraAudio.Play(AsterraSfx.BuildComplete, 0.9f);
                                break;
                            }
                            case CombatEventKind.ResearchComplete:
                                SpawnBurst(ev.X, ev.Z, new Color(0.55f, 0.75f, 1f, 0.95f), 7f, pulseSeconds);
                                AsterraAudio.Play(AsterraSfx.OrderResearch, 0.9f);
                                MatchFeedback.Show("Research complete");
                                break;
                            case CombatEventKind.TrainComplete:
                            {
                                SpawnBurst(ev.X, ev.Z, new Color(0.4f, 0.9f, 0.55f, 0.9f), 5f, pulseSeconds);
                                AsterraAudio.Play(AsterraSfx.OrderTrain, 0.55f);
                                var trainer = FindView(views, ev.TargetId);
                                if (trainer != null && !trainer.IsUnit)
                                    trainer.PlayBuildCompleteFlash();
                                break;
                            }
                            case CombatEventKind.PowerActivated:
                            {
                                SpawnBurst(ev.X, ev.Z, new Color(0.85f, 0.55f, 1f, 0.95f), 12f, pulseSeconds * 1.2f);
                                AsterraAudio.Play(AsterraSfx.OrderResearch, 1f);
                                MatchFeedback.Show("Commander power active!");
                                for (int v = 0; v < views.Length; v++)
                                {
                                    var view = views[v];
                                    if (view == null || !view.IsUnit || !view.IsRevealed)
                                        continue;
                                    if (ev.IssuerPlayer != 255 && view.Owner.Value != ev.IssuerPlayer)
                                        continue;
                                    view.SetPowerAura(10f);
                                }

                                break;
                            }
                            case CombatEventKind.UpgradeApplied:
                            {
                                var upgraded = FindView(views, ev.TargetId);
                                if (upgraded != null)
                                {
                                    upgraded.SetHitFlash();
                                    upgraded.SetPowerAura(1.2f);
                                }

                                SpawnBurst(ev.X, ev.Z, new Color(1f, 0.55f, 0.15f, 0.95f), 6f, pulseSeconds);
                                MatchFeedback.Show("Equipment equipped");
                                break;
                            }
                            default:
                                throw new System.ArgumentOutOfRangeException(nameof(ev.Kind), ev.Kind, null);
                        }
                    }
                }
            }

            if (match.Result.IsOver && match.Session != null && !_endPosesApplied)
            {
                _endPosesApplied = true;
                var winner = match.Result.Winner;
                var views = FindObjectsByType<EntityView>(FindObjectsSortMode.None);
                for (int v = 0; v < views.Length; v++)
                {
                    var view = views[v];
                    if (view != null && view.IsUnit)
                        view.SetOutcomePose(view.Owner == winner);
                }
            }

            float now = Time.time;
            for (int i = _bursts.Count - 1; i >= 0; i--)
            {
                var burst = _bursts[i];
                if (burst.Go != null)
                {
                    float u = Mathf.Clamp01((now - burst.BornAt) / Mathf.Max(0.05f, burst.Life));
                    float pop = Mathf.Sin(Mathf.Clamp01(u * 1.15f) * Mathf.PI);
                    float s = burst.Size * (0.35f + pop * 0.85f);
                    burst.Go.transform.localScale = new Vector3(s, s, s);
                    burst.Go.transform.Rotate(0f, 220f * Time.deltaTime, 90f * Time.deltaTime, Space.World);
                }

                if (now < burst.ExpireAt)
                    continue;
                if (burst.Go != null)
                    Destroy(burst.Go);
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

            _bursts.Add(new FxBurst
            {
                Go = go,
                ExpireAt = Time.time + life,
                BornAt = Time.time,
                Life = life,
                Size = size,
            });
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

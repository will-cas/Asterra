using UnityEngine;
using Asterra.Core;
using Asterra.Core.World;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Development overlay for world systems. Toggle with F3 in the Editor / Development Player.
    /// </summary>
    public sealed class WorldDebugOverlay : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private bool visible;
        [SerializeField] private bool showTerrainCosts = true;
        [SerializeField] private bool showNoEntry = true;
        [SerializeField] private bool showCaptureRadii = true;
        [SerializeField] private bool showTraversal = true;

        private GUIStyle _box;

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
            enabled = false;
#endif
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
                visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible || match == null)
                return;
            var sim = match.World as SkirmishWorldSim;
            if (sim == null)
                return;

            if (_box == null)
                _box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 12 };

            var env = sim.Environment;
            var w = env.WeatherSim.Current;
            var tod = env.TimeOfDaySim;
            string text =
                $"Asterra World Debug (F3)\n" +
                $"Time {tod.Time01:0.00}  phase={tod.Phase}  day={tod.IsDay} night={tod.IsNight}\n" +
                $"Weather {w.Kind} inten={w.Intensity:0.00} vis={env.CombinedVisibility():0.00}\n" +
                $"Wind ({env.WeatherSim.WindDirX:0.00},{env.WeatherSim.WindDirZ:0.00}) i={env.WeatherSim.WindIntensity:0.00}\n" +
                $"Links={env.TraversalGraph.Links.Count} Destructibles={sim.Destructibles.Count} Dirty={env.PathDirty.Count}";
            GUI.Box(new Rect(12, 12, 420, 110), text, _box);
        }

        private void OnDrawGizmos()
        {
            if (!visible || match == null)
                return;
            var sim = match.World as SkirmishWorldSim;
            if (sim == null)
                return;
            var env = sim.Environment;

            if (showCaptureRadii)
            {
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
                for (int i = 0; i < sim.Territories.Count; i++)
                {
                    var t = sim.Territories[i];
                    Gizmos.DrawWireSphere(new Vector3(t.X, 1f, t.Z), t.Radius);
                }
            }

            if (showTraversal)
            {
                Gizmos.color = Color.yellow;
                var links = env.TraversalGraph.Links;
                for (int i = 0; i < links.Count; i++)
                {
                    if (!links[i].Enabled)
                        continue;
                    Gizmos.DrawLine(
                        new Vector3(links[i].StartX, 2f, links[i].StartZ),
                        new Vector3(links[i].EndX, 2f, links[i].EndZ));
                }
            }

            if (showNoEntry)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
                for (float z = -400f; z <= 400f; z += 40f)
                {
                    for (float x = -400f; x <= 400f; x += 40f)
                    {
                        if (env.Grid.IsBlocked(x, z))
                            Gizmos.DrawCube(new Vector3(x, 1f, z), new Vector3(8f, 1f, 8f));
                    }
                }
            }

            if (!showTerrainCosts)
                return;

            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.15f);
            for (float z = -200f; z <= 200f; z += 50f)
            {
                for (float x = -200f; x <= 200f; x += 50f)
                {
                    float cost = env.Grid.GetPathCost(x, z, TraversalCapability.Land);
                    if (cost > 2f && cost < TerrainDefData.PathCostBlocked * 0.5f)
                        Gizmos.DrawWireCube(new Vector3(x, 0.5f, z), Vector3.one * 12f);
                }
            }
        }
    }
}

using System.Collections.Generic;
using Asterra.Core;
using Asterra.Gameplay.Player;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Draws move/attack order lines for the local selection.</summary>
    public sealed class OrderLinePresenter : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private LocalOrderController orders;
        [SerializeField] private float lineHeight = 1.2f;

        private readonly List<LineRenderer> _pool = new();
        private Material _moveMat;
        private Material _attackMat;

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
            if (orders == null)
                orders = FindFirstObjectByType<LocalOrderController>();
        }

        private void LateUpdate()
        {
            if (match?.World == null || orders?.Selection == null || !match.IsMatchRunning)
            {
                HideAll();
                return;
            }

            EnsureMaterials();
            var selected = orders.Selection.Selected;
            int used = 0;
            for (int i = 0; i < selected.Count; i++)
            {
                var id = selected[i];
                UnitSnapshot? snap = null;
                for (int u = 0; u < match.World.Units.Count; u++)
                {
                    if (match.World.Units[u].Id != id)
                        continue;
                    snap = match.World.Units[u];
                    break;
                }

                if (!snap.HasValue || !snap.Value.IsAlive || snap.Value.IsGarrisoned)
                    continue;

                var unit = snap.Value;
                float tx = unit.X;
                float tz = unit.Z;
                bool attack = false;
                if (unit.HasAttackTarget)
                {
                    if (!TryResolveTarget(unit.AttackTargetId, out tx, out tz))
                        continue;
                    attack = true;
                }
                else if (unit.HasMoveTarget || unit.AttackMoving || unit.Patrolling)
                {
                    tx = unit.MoveTargetX;
                    tz = unit.MoveTargetZ;
                }
                else
                    continue;

                var line = GetLine(used++);
                line.enabled = true;
                line.sharedMaterial = attack ? _attackMat : _moveMat;
                line.startColor = attack ? new Color(1f, 0.35f, 0.25f, 0.85f) : new Color(0.35f, 0.85f, 1f, 0.75f);
                line.endColor = line.startColor;
                line.SetPosition(0, new Vector3(unit.X, lineHeight, unit.Z));
                line.SetPosition(1, new Vector3(tx, lineHeight, tz));
            }

            for (int i = used; i < _pool.Count; i++)
                _pool[i].enabled = false;
        }

        private bool TryResolveTarget(uint id, out float x, out float z)
        {
            x = 0f;
            z = 0f;
            var world = match.World;
            for (int i = 0; i < world.Units.Count; i++)
            {
                if (world.Units[i].Id.Value != id)
                    continue;
                x = world.Units[i].X;
                z = world.Units[i].Z;
                return true;
            }

            for (int i = 0; i < world.Buildings.Count; i++)
            {
                if (world.Buildings[i].Id.Value != id)
                    continue;
                x = world.Buildings[i].X;
                z = world.Buildings[i].Z;
                return true;
            }

            if (world.Destructibles != null)
            {
                for (int i = 0; i < world.Destructibles.Count; i++)
                {
                    if (world.Destructibles[i].Id.Value != id)
                        continue;
                    x = world.Destructibles[i].X;
                    z = world.Destructibles[i].Z;
                    return true;
                }
            }

            return false;
        }

        private LineRenderer GetLine(int index)
        {
            while (_pool.Count <= index)
            {
                var go = new GameObject("OrderLine");
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.widthMultiplier = 0.45f;
                lr.numCapVertices = 2;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.enabled = false;
                _pool.Add(lr);
            }

            return _pool[index];
        }

        private void HideAll()
        {
            for (int i = 0; i < _pool.Count; i++)
                _pool[i].enabled = false;
        }

        private void EnsureMaterials()
        {
            if (_moveMat != null)
                return;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            _moveMat = new Material(shader) { color = new Color(0.35f, 0.85f, 1f, 0.75f) };
            _attackMat = new Material(shader) { color = new Color(1f, 0.35f, 0.25f, 0.85f) };
        }
    }
}

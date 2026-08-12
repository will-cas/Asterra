using UnityEngine;
using Asterra.Core;

namespace Asterra.Gameplay
{
    public sealed class TerritoryNodeRuntime : MonoBehaviour, ITerritoryNode
    {
        public SimEntityId Id { get; private set; }
        public TerritoryState State { get; private set; } = TerritoryState.Neutral;
        public PlayerId? Controller { get; private set; }
        public float CaptureProgress { get; private set; }

        public void Initialize(SimEntityId id)
        {
            Id = id;
            State = TerritoryState.Neutral;
            Controller = null;
            CaptureProgress = 0f;
        }

        public void SetController(PlayerId player)
        {
            Controller = player;
            State = TerritoryState.Controlled;
            CaptureProgress = 1f;
        }

        public void SetContested(float progress)
        {
            State = TerritoryState.Contested;
            CaptureProgress = Mathf.Clamp01(progress);
        }
    }
}

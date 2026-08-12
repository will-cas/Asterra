using Asterra.Core;
using Asterra.Gameplay;
using UnityEngine;

namespace Asterra.UI
{
    /// <summary>Local-only HUD shell for the skirmish slice.</summary>
    public sealed class SkirmishHud : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private string lastResourceLine;
        [SerializeField] private string lastStatusLine;

        public string LastResourceLine => lastResourceLine;
        public string LastStatusLine => lastStatusLine;

        private void Update()
        {
            if (match == null || match.Wallet == null || match.Session == null)
                return;

            var player = match.Session.LocalPlayer;
            int gold = match.Wallet.Get(player, ResourceType.Gold);
            int timber = match.Wallet.Get(player, ResourceType.Timber);
            int mana = match.Wallet.Get(player, ResourceType.Mana);
            lastResourceLine = $"Gold {gold}  Timber {timber}  Mana {mana}";

            int units = match.World != null ? match.World.Units.Count : 0;
            string territory = "n/a";
            if (match.World != null && match.World.Territories.Count > 0)
            {
                var t = match.World.Territories[0];
                territory = t.HasController ? t.Controller.ToString() : t.State.ToString();
            }

            lastStatusLine = $"Units {units}  Territory {territory}  Tick {match.Clock?.CurrentTick.Value}";
        }

        public void SetResources(int gold, int timber, int mana)
        {
            lastResourceLine = $"Gold {gold}  Timber {timber}  Mana {mana}";
        }
    }
}

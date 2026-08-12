using Asterra.Gameplay.Content;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Pre-match OnGUI setup: faction picks, map, start offline skirmish.</summary>
    public sealed class OfflineMatchMenu : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap bootstrap;

        private int _playerFaction;
        private int _enemyFaction = 1;
        private SkirmishMapId _map = SkirmishMapId.BlackridgePass;

        private void Awake()
        {
            if (bootstrap == null)
                bootstrap = GetComponent<MatchBootstrap>();
            if (bootstrap == null)
                bootstrap = FindFirstObjectByType<MatchBootstrap>();

            if (bootstrap != null)
            {
                _playerFaction = bootstrap.PlayerFactionIndex;
                _enemyFaction = bootstrap.EnemyFactionIndex;
                _map = bootstrap.MapId;
            }
        }

        private void OnGUI()
        {
            if (bootstrap == null)
                bootstrap = FindFirstObjectByType<MatchBootstrap>();
            if (bootstrap == null || bootstrap.IsMatchRunning)
                return;

            float w = 460f;
            float h = 320f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.38f;
            GUI.Box(new Rect(x, y, w, h), "Asterra — Offline Skirmish");

            float rowX = x + 28f;
            float rowY = y + 48f;
            const float rowH = 36f;
            const float labelW = 140f;
            const float btnW = 240f;

            GUI.Label(new Rect(rowX, rowY, labelW, rowH), "Your Faction");
            var playerRoster = FactionDefaultContent.All[_playerFaction % FactionDefaultContent.All.Length];
            if (GUI.Button(new Rect(rowX + labelW, rowY, btnW, 28f), playerRoster.DisplayName))
                _playerFaction = (_playerFaction + 1) % 3;

            rowY += rowH;
            GUI.Label(new Rect(rowX, rowY, labelW, rowH), "Enemy Faction");
            var enemyRoster = FactionDefaultContent.All[_enemyFaction % FactionDefaultContent.All.Length];
            if (GUI.Button(new Rect(rowX + labelW, rowY, btnW, 28f), enemyRoster.DisplayName))
                _enemyFaction = (_enemyFaction + 1) % 3;

            rowY += rowH;
            GUI.Label(new Rect(rowX, rowY, labelW, rowH), "Map");
            if (GUI.Button(new Rect(rowX + labelW, rowY, btnW, 28f), SkirmishDefaultContent.GetMapDisplayName(_map)))
                _map = SkirmishDefaultContent.NextMap(_map);

            rowY += rowH + 8f;
            GUI.Label(new Rect(rowX, rowY, w - 56f, 40f), playerRoster.LoreBlurb);

            rowY += 44f;
            if (GUI.Button(new Rect(rowX + 40f, rowY, w - 100f, 40f), "Start Skirmish"))
            {
                bootstrap.ConfigureAndStartOffline(_playerFaction, _enemyFaction, _map);
                enabled = false;
            }
        }
    }
}

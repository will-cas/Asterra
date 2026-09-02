using Asterra.Core;
using Asterra.Gameplay;
using Asterra.Gameplay.Audio;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Presentation;
using Asterra.Net;
using UnityEngine;

namespace Asterra.UI
{
    /// <summary>Lobby actions + OnGUI lobby screen when a lobby session is active.</summary>
    public sealed class LobbyHud : MonoBehaviour
    {
        [SerializeField] private MatchLobbyController lobby;
        [SerializeField] private MultiplayerMenu multiplayerMenu;
        [SerializeField] private int factionIndex;
        [SerializeField] private int teamColorIndex;
        [SerializeField] private bool drawGui = true;

        public string StatusLine { get; private set; } = "Lobby";

        private void Awake()
        {
            if (lobby == null)
                lobby = FindFirstObjectByType<MatchLobbyController>();
            if (multiplayerMenu == null)
                multiplayerMenu = FindFirstObjectByType<MultiplayerMenu>();
            _ = AsterraAudio.Instance;
        }

        private void Update()
        {
            if (lobby != null)
                StatusLine = lobby.Status;
        }

        private void OnGUI()
        {
            if (!drawGui)
                return;
            if (lobby == null)
                lobby = FindFirstObjectByType<MatchLobbyController>();
            if (lobby == null)
                return;

            // Hide while an offline skirmish is running.
            var match = FindFirstObjectByType<MatchBootstrap>();
            if (match != null && match.IsMatchRunning)
                return;

            HudStyle.Ensure();
            float w = 460f;
            float h = 360f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.38f;
            var panel = new Rect(x, y, w, h);
            HudClickBlocker.Block(panel);
            HudStyle.DrawPanel(panel, new Color(0.05f, 0.07f, 0.09f, 0.94f));
            GUI.Label(new Rect(x, y + 12f, w, 28f), "Multiplayer Lobby", HudStyle.Title);
            GUI.Label(new Rect(x + 24f, y + 52f, w - 48f, 22f), StatusLine ?? "Lobby", HudStyle.Label);

            float bx = x + 24f;
            float by = y + 90f;
            if (IconBtn(new Rect(bx, by, 130f, 32f), "Claim Slot"))
                OnClaim();
            if (IconBtn(new Rect(bx + 140f, by, 100f, 32f), "Ready"))
                OnReady();
            if (IconBtn(new Rect(bx + 250f, by, 110f, 32f), "Unready"))
                OnUnready();

            by += 44f;
            GUI.Label(new Rect(bx, by, 120f, 24f), "Faction", HudStyle.Label);
            int n = FactionDefaultContent.All.Length;
            string fname = FactionDefaultContent.All[factionIndex % n].DisplayName;
            if (IconBtn(new Rect(bx + 120f, by, 240f, 28f), fname))
                OnCycleFaction();

            by += 36f;
            GUI.Label(new Rect(bx, by, 120f, 24f), "Team colour", HudStyle.Label);
            DrawLobbySwatches(new Rect(bx + 120f, by, 280f, 26f));

            by += 44f;
            if (IconBtn(new Rect(bx, by, 130f, 32f), "Host"))
                OnHostSession();
            if (IconBtn(new Rect(bx + 140f, by, 130f, 32f), "Join"))
                OnJoinSession();
            if (IconBtn(new Rect(bx + 280f, by, 120f, 32f), "Start"))
                OnStart();
        }

        private void DrawLobbySwatches(Rect row)
        {
            int n = AsterraMeshLibrary.TeamSwatchCount;
            float s = Mathf.Min(24f, (row.width - (n - 1) * 4f) / n);
            for (int i = 0; i < n; i++)
            {
                var r = new Rect(row.x + i * (s + 4f), row.y + (row.height - s) * 0.5f, s, s);
                HudClickBlocker.Block(r);
                bool picked = teamColorIndex == i;
                HudStyle.DrawFrame(
                    r,
                    AsterraMeshLibrary.TeamSwatch(i),
                    picked ? Color.white : new Color(0.05f, 0.05f, 0.05f, 0.7f),
                    picked ? 2f : 1f);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                    OnPickTeamColor(i);
            }
        }

        private static bool IconBtn(Rect r, string label)
        {
            HudClickBlocker.Block(r);
            HudStyle.DrawPanel(r, new Color(0.1f, 0.12f, 0.14f, 0.95f));
            bool clicked = GUI.Button(r, label, HudStyle.Button);
            if (clicked)
                AsterraAudio.PlayUiClick();
            return clicked;
        }

        public void OnClaim() => lobby?.ClaimLocalSlot();
        public void OnReady() => lobby?.SetLocalReady(true);
        public void OnUnready() => lobby?.SetLocalReady(false);
        public void OnStart() => lobby?.HostStart();

        public void OnCycleFaction()
        {
            int n = FactionDefaultContent.All.Length;
            factionIndex = (factionIndex + 1) % n;
            lobby?.SetLocalFaction(factionIndex);
            AsterraAudio.PlayUiClick();
        }

        public void OnPickTeamColor(int index)
        {
            teamColorIndex = ((index % AsterraMeshLibrary.TeamSwatchCount) + AsterraMeshLibrary.TeamSwatchCount)
                % AsterraMeshLibrary.TeamSwatchCount;
            lobby?.SetLocalTeamColor(teamColorIndex);
            AsterraAudio.PlayUiClick();
        }

        public void OnHostSession() => multiplayerMenu?.Host();
        public void OnJoinSession() => multiplayerMenu?.Join();
    }
}

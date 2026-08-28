using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Biofall.Core;
using Biofall.Data;
using Biofall.Net;

namespace Biofall.UI
{
    // The menu, rebuilt on the session layer. Nothing here knows about NetworkManager,
    // LanDiscovery or CoopSession: hosting and joining go through ISessionService, the roster
    // and the ready flags through ILobbyService.
    public sealed class MainMenuScreen : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private GameObject coopPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Root buttons")]
        [SerializeField] private Button singlePlayerButton;
        [SerializeField] private Button coopButton;
        [SerializeField] private Button waveModeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button exitButton;

        [Header("Co-op panel")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private TMP_InputField joinCodeField;
        [SerializeField] private Button coopBackButton;
        [SerializeField] private TMP_Text statusLabel;

        [Header("Lobby panel")]
        [SerializeField] private TMP_Text joinCodeLabel;
        [SerializeField] private TMP_Text[] slotLabels;
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text readyButtonLabel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button leaveButton;

        [Header("Solo")]
        [Tooltip("Mission scene loaded for a single-player run.")]
        [SerializeField] private string missionScene = GameScenes.Gameplay;
        [SerializeField] private string waveScene = GameScenes.WaveMode;

        private ISessionService _session;
        private ILobbyService _lobby;
        private bool _busy;

        private void Awake()
        {
            _session = ServiceLocator.Get<ISessionService>();
            _lobby = ServiceLocator.Get<ILobbyService>();

            Wire(singlePlayerButton, () => StartSolo(missionScene));
            Wire(waveModeButton, () => StartSolo(waveScene));
            Wire(coopButton, () => Show(coopPanel));
            Wire(settingsButton, () => Show(settingsPanel));
            Wire(creditsButton, () => Show(creditsPanel));
            Wire(exitButton, Quit);

            Wire(hostButton, Host);
            Wire(joinButton, Join);
            Wire(coopBackButton, ShowRoot);

            Wire(readyButton, ToggleReady);
            Wire(startButton, () => _lobby.RequestStartRun());
            Wire(leaveButton, Leave);
        }

        private void OnEnable()
        {
            if (_lobby != null) _lobby.Changed += Refresh;
            if (_session != null) _session.PhaseChanged += OnSessionPhase;
            ShowRoot();
        }

        private void OnDisable()
        {
            if (_lobby != null) _lobby.Changed -= Refresh;
            if (_session != null) _session.PhaseChanged -= OnSessionPhase;
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        // ---- solo -------------------------------------------------------------------------

        // Solo is a session of one: the same host path, the same spawner, the same authority.
        // There is no second code path to keep in step.
        private async void StartSolo(string scene)
        {
            if (_busy) return;
            _busy = true;
            SetStatus("STARTING...");

            bool ok = await _session.HostAsync(1, "BIOFALL Solo");
            _busy = false;

            if (!ok) { SetStatus(_session.LastError); return; }

            _lobby.SetReady(true);
            _lobby.RequestStartRun();
        }

        // ---- co-op ------------------------------------------------------------------------

        private async void Host()
        {
            if (_busy) return;
            _busy = true;
            SetStatus("CREATING SESSION...");

            bool ok = await _session.HostAsync(4, "BIOFALL Squad");
            _busy = false;

            SetStatus(ok ? string.Empty : _session.LastError);
            if (ok) Show(lobbyPanel);
        }

        private async void Join()
        {
            if (_busy) return;
            _busy = true;
            SetStatus("JOINING...");

            string code = joinCodeField != null ? joinCodeField.text : string.Empty;
            bool ok = await _session.JoinAsync(code);
            _busy = false;

            SetStatus(ok ? string.Empty : _session.LastError);
            if (ok) Show(lobbyPanel);
        }

        private async void Leave()
        {
            if (_busy) return;
            _busy = true;

            await _session.LeaveAsync();

            _busy = false;
            ShowRoot();
        }

        private void ToggleReady() => _lobby.SetReady(!_lobby.LocalIsReady);

        private void OnSessionPhase(SessionPhase phase)
        {
            if (phase == SessionPhase.Offline) ShowRoot();
            if (phase == SessionPhase.Failed) SetStatus(_session.LastError);
        }

        // ---- view -------------------------------------------------------------------------

        private void Refresh()
        {
            if (lobbyPanel == null || !lobbyPanel.activeSelf) return;

            if (joinCodeLabel != null)
                joinCodeLabel.text = string.IsNullOrEmpty(_session.JoinCode)
                    ? "CODE —" : "CODE  " + _session.JoinCode;

            if (slotLabels != null)
                for (int i = 0; i < slotLabels.Length; i++)
                {
                    if (slotLabels[i] == null) continue;

                    if (_lobby.TryGetSlot(i, out PlayerSlot slot))
                        slotLabels[i].text = $"{slot.DisplayName}   {(slot.IsReady ? "READY" : "...")}";
                    else
                        slotLabels[i].text = "— EMPTY —";
                }

            if (readyButtonLabel != null)
                readyButtonLabel.text = _lobby.LocalIsReady ? "UNREADY" : "READY";

            if (startButton != null)
            {
                startButton.gameObject.SetActive(_lobby.IsHost);
                startButton.interactable = _lobby.AllReady;
            }
        }

        private void Show(GameObject panel)
        {
            SetActive(rootPanel, panel == rootPanel);
            SetActive(coopPanel, panel == coopPanel);
            SetActive(lobbyPanel, panel == lobbyPanel);
            SetActive(settingsPanel, panel == settingsPanel);
            SetActive(creditsPanel, panel == creditsPanel);

            Refresh();
        }

        private void ShowRoot()
        {
            SetStatus(string.Empty);
            Show(rootPanel);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }

        private void SetStatus(string text)
        {
            if (statusLabel != null) statusLabel.text = text ?? string.Empty;
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

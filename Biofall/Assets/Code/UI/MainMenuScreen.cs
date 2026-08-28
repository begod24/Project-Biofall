using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Biofall.Core;
using Biofall.Data;
using Biofall.Net;

namespace Biofall.UI
{
    // The menu, sitting on the session layer. Nothing here knows about NetworkManager,
    // LanDiscovery or CoopSession: hosting and joining go through ISessionService, the roster
    // and the ready flags through ILobbyService.
    //
    // Solo is a session of one -- the same host path, the same spawner, the same authority.
    public sealed class MainMenuScreen : MonoBehaviour
    {
        // What the player picked before the operative screen interrupted them.
        private enum PendingAction { None, SoloNew, SoloContinue, Wave, CoopHost, CoopContinue, CoopJoin }

        [Header("Panels")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private GameObject singlePanel;
        [SerializeField] private GameObject coopPanel;
        [SerializeField] private GameObject connectPanel;
        [SerializeField] private GameObject operativePanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Root")]
        [SerializeField] private Button singlePlayerButton;
        [SerializeField] private Button coopButton;
        [SerializeField] private Button waveModeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button exitButton;

        [Header("Single player")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button singleBackButton;

        [Header("Co-op")]
        [SerializeField] private Button hostGameButton;
        [SerializeField] private Button coopContinueButton;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button coopBackButton;

        [Header("Connect")]
        [SerializeField] private TMP_InputField joinCodeField;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button connectBackButton;

        [Header("Operative select")]
        [SerializeField] private Button[] operativeButtons;
        [SerializeField] private TMP_Text[] operativeNames;
        [SerializeField] private TMP_Text[] operativeDescriptions;
        [SerializeField] private Graphic[] operativeFrames;
        [SerializeField] private Button deployButton;
        [SerializeField] private Button operativeBackButton;

        [Header("Lobby")]
        [SerializeField] private TMP_Text joinCodeLabel;
        [SerializeField] private TMP_Text[] slotLabels;
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text readyButtonLabel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button leaveButton;

        [Header("Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider shakeSlider;
        [SerializeField] private Button settingsBackButton;

        [Header("Credits")]
        [SerializeField] private Button creditsBackButton;

        [Header("Status")]
        [SerializeField] private TMP_Text statusLabel;

        [Header("Scenes")]
        [Tooltip("Networked mission. Solo and co-op run the same scene: solo is a session of one.")]
        [SerializeField] private string missionScene = GameScenes.MissionCoop;
        [SerializeField] private string waveScene = GameScenes.WaveMode;

        private ISessionService _session;
        private ILobbyService _lobby;
        private ISettingsService _settings;
        private IOperativeService _operatives;
        private ICampaignState _campaign;

        private PendingAction _pending = PendingAction.None;
        private bool _busy;

        private void Awake()
        {
            _session = ServiceLocator.Get<ISessionService>();
            _lobby = ServiceLocator.Get<ILobbyService>();
            _settings = ServiceLocator.Get<ISettingsService>();
            _operatives = ServiceLocator.Get<IOperativeService>();
            _campaign = ServiceLocator.Get<ICampaignState>();

            Wire(singlePlayerButton, () => Show(singlePanel));
            Wire(coopButton, () => Show(coopPanel));
            Wire(waveModeButton, () => Begin(PendingAction.Wave));
            Wire(settingsButton, () => Show(settingsPanel));
            Wire(creditsButton, () => Show(creditsPanel));
            Wire(exitButton, Quit);

            Wire(newGameButton, () => Begin(PendingAction.SoloNew));
            Wire(continueButton, () => Begin(PendingAction.SoloContinue));
            Wire(singleBackButton, ShowRoot);

            Wire(hostGameButton, () => Begin(PendingAction.CoopHost));
            Wire(coopContinueButton, () => Begin(PendingAction.CoopContinue));
            Wire(connectButton, () => Show(connectPanel));
            Wire(coopBackButton, ShowRoot);

            Wire(joinButton, () => Begin(PendingAction.CoopJoin));
            Wire(connectBackButton, () => Show(coopPanel));

            Wire(deployButton, Deploy);
            Wire(operativeBackButton, ShowRoot);

            Wire(readyButton, () => _lobby.SetReady(!_lobby.LocalIsReady));
            Wire(startButton, () => _lobby.RequestStartRun(missionScene));
            Wire(leaveButton, Leave);

            Wire(settingsBackButton, ShowRoot);
            Wire(creditsBackButton, ShowRoot);

            WireOperativeCards();
            WireSettings();
        }

        private void OnEnable()
        {
            if (_lobby != null) _lobby.Changed += Refresh;
            if (_session != null) _session.PhaseChanged += OnSessionPhase;
            if (_operatives != null) _operatives.Changed += RefreshOperatives;
            ShowRoot();
        }

        private void OnDisable()
        {
            if (_lobby != null) _lobby.Changed -= Refresh;
            if (_session != null) _session.PhaseChanged -= OnSessionPhase;
            if (_operatives != null) _operatives.Changed -= RefreshOperatives;
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        // ---- flow ---------------------------------------------------------------------------

        // Every launch route passes through the operative screen first, so the pick is made
        // once and the action that was waiting resumes on Deploy.
        private void Begin(PendingAction action)
        {
            _pending = action;
            Show(operativePanel);
        }

        private void Deploy()
        {
            PendingAction action = _pending;
            _pending = PendingAction.None;

            switch (action)
            {
                case PendingAction.SoloNew:      StartSolo(missionScene, 0, fresh: true); break;
                case PendingAction.SoloContinue: StartSolo(missionScene, _campaign.LastMissionIndex, fresh: false); break;
                case PendingAction.Wave:         StartSolo(waveScene, -1, fresh: true); break;
                case PendingAction.CoopHost:     HostCoop(); break;
                case PendingAction.CoopContinue: HostCoop(); break;
                case PendingAction.CoopJoin:     JoinCoop(); break;
                default:                         ShowRoot(); break;
            }
        }

        // Solo is a session of one: same host path, same spawner, same authority. There is no
        // second code path to keep in step.
        private async void StartSolo(string scene, int missionIndex, bool fresh)
        {
            if (_busy) return;
            _busy = true;
            SetStatus("STARTING...");

            await DropStaleSessionAsync();
            bool ok = await _session.HostAsync(1, "BIOFALL Solo");
            _busy = false;

            if (!ok) { SetStatus(_session.LastError); Show(rootPanel); return; }

            if (missionIndex >= 0) _campaign.RecordRunStarted(fresh ? 0 : missionIndex);

            SetStatus(string.Empty);
            _lobby.SetReady(true);
            _lobby.RequestStartRun(scene);
        }

        private async void HostCoop()
        {
            if (_busy) return;
            _busy = true;
            SetStatus("CREATING SESSION...");

            await DropStaleSessionAsync();
            bool ok = await _session.HostAsync(4, "BIOFALL Squad");
            _busy = false;

            SetStatus(ok ? string.Empty : _session.LastError);
            Show(ok ? lobbyPanel : coopPanel);
        }

        private async void JoinCoop()
        {
            if (_busy) return;
            _busy = true;
            SetStatus("JOINING...");

            await DropStaleSessionAsync();

            string code = joinCodeField != null ? joinCodeField.text : string.Empty;
            bool ok = await _session.JoinAsync(code);
            _busy = false;

            SetStatus(ok ? string.Empty : _session.LastError);
            Show(ok ? lobbyPanel : connectPanel);
        }

        // Reaching a launch button means the player is on the root menu, not in a lobby, so a
        // session still open here is left over from a run that exited badly. Closing it is what
        // keeps a single stuck session from locking the player out of the game -- without this,
        // "Already in a session." is unrecoverable short of restarting the build.
        private async System.Threading.Tasks.Task DropStaleSessionAsync()
        {
            if (_session == null || _session.Phase == SessionPhase.Offline) return;

            Debug.LogWarning("[Menu] A session was still open on the main menu. Closing it " +
                             "before starting a new one.");
            await _session.LeaveAsync();
        }

        private async void Leave()
        {
            if (_busy) return;
            _busy = true;

            await _session.LeaveAsync();

            _busy = false;
            ShowRoot();
        }

        private void OnSessionPhase(SessionPhase phase)
        {
            if (phase == SessionPhase.Offline && lobbyPanel != null && lobbyPanel.activeSelf) ShowRoot();
            if (phase == SessionPhase.Failed) SetStatus(_session.LastError);
        }

        // ---- operatives ---------------------------------------------------------------------

        private void WireOperativeCards()
        {
            if (operativeButtons == null || _operatives == null) return;

            OperativeData[] all = _operatives.All;

            for (int i = 0; i < operativeButtons.Length; i++)
            {
                int index = i;
                Wire(operativeButtons[i], () =>
                {
                    OperativeData[] list = _operatives.All;
                    if (index < list.Length && list[index] != null) _operatives.Select(list[index].id);
                });

                bool has = i < all.Length && all[i] != null;

                if (operativeNames != null && i < operativeNames.Length && operativeNames[i] != null)
                    operativeNames[i].text = has ? all[i].displayName : "—";

                if (operativeDescriptions != null && i < operativeDescriptions.Length && operativeDescriptions[i] != null)
                    operativeDescriptions[i].text = has ? all[i].description : string.Empty;

                if (operativeButtons[i] != null) operativeButtons[i].interactable = has;
            }

            RefreshOperatives();
        }

        private void RefreshOperatives()
        {
            if (operativeFrames == null || _operatives == null) return;

            OperativeData[] all = _operatives.All;
            string selected = _operatives.Selected != null ? _operatives.Selected.id : null;

            for (int i = 0; i < operativeFrames.Length; i++)
            {
                if (operativeFrames[i] == null) continue;
                operativeFrames[i].enabled = i < all.Length && all[i] != null && all[i].id == selected;
            }
        }

        // ---- settings -------------------------------------------------------------------------

        private void WireSettings()
        {
            if (_settings == null) return;

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(_settings.MasterVolume);
                masterVolumeSlider.onValueChanged.AddListener(_settings.SetMasterVolume);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(_settings.MusicVolume);
                musicVolumeSlider.onValueChanged.AddListener(_settings.SetMusicVolume);
            }

            if (shakeSlider != null)
            {
                shakeSlider.SetValueWithoutNotify(_settings.CameraShakeIntensity);
                shakeSlider.onValueChanged.AddListener(_settings.SetCameraShakeIntensity);
            }
        }

        // ---- view -----------------------------------------------------------------------------

        private void Refresh()
        {
            if (lobbyPanel == null || !lobbyPanel.activeSelf) return;

            if (joinCodeLabel != null)
                joinCodeLabel.text = string.IsNullOrEmpty(_session.JoinCode)
                    ? "CODE  —" : "CODE  " + _session.JoinCode;

            if (slotLabels != null)
                for (int i = 0; i < slotLabels.Length; i++)
                {
                    if (slotLabels[i] == null) continue;

                    slotLabels[i].text = _lobby.TryGetSlot(i, out PlayerSlot slot)
                        ? $"{slot.DisplayName}   {(slot.IsReady ? "READY" : "STANDBY")}"
                        : "— EMPTY —";
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
            SetActive(singlePanel, panel == singlePanel);
            SetActive(coopPanel, panel == coopPanel);
            SetActive(connectPanel, panel == connectPanel);
            SetActive(operativePanel, panel == operativePanel);
            SetActive(lobbyPanel, panel == lobbyPanel);
            SetActive(settingsPanel, panel == settingsPanel);
            SetActive(creditsPanel, panel == creditsPanel);

            // Nothing to continue until a run has been entered once.
            if (continueButton != null) continueButton.interactable = _campaign.HasStartedARun;
            if (coopContinueButton != null) coopContinueButton.interactable = _campaign.HasStartedARun;

            Refresh();
        }

        private void ShowRoot()
        {
            _pending = PendingAction.None;
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

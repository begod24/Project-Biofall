using System.Collections;
using Biofall.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Biofall.Core;
using Biofall.Gameplay.Mission1;
using Biofall.Net;

namespace Biofall.UI
{
    public sealed class MissionCompleteUI : MonoBehaviour
    {

        private IEventBus _bus;
        private IEventBus Bus => _bus ??= ServiceLocator.Get<IEventBus>();
        [SerializeField] private GameObject panel;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Audio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip completeSfx;
        [Range(0f, 1f)] [SerializeField] private float completeVolume = 0.6f;

        [Header("Timing")]
        [Tooltip("Delay (real seconds) before the panel appears.")]
        [SerializeField] private float showDelay = 1.2f;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (replayButton != null) replayButton.onClick.AddListener(Replay);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ToMainMenu);
        }

        private void OnEnable() => Bus.Subscribe<MissionCompleted>(OnCompleted);
        private void OnDisable() => Bus.Unsubscribe<MissionCompleted>(OnCompleted);

        private void OnCompleted(MissionCompleted _)
        {
            if (showDelay > 0f) StartCoroutine(ShowAfterDelay());
            else Show();
        }

        private IEnumerator ShowAfterDelay()
        {
            yield return new WaitForSecondsRealtime(showDelay);
            Show();
        }

        private void Show()
        {
            if (panel != null) panel.SetActive(true);
            UiOverlay.Active = true;
            Cursor.visible = true;
            if (replayButton != null) replayButton.interactable = !NetSession.InCoop || NetSession.IsServer;
            if (sfxSource != null && completeSfx != null)
                sfxSource.PlayOneShot(completeSfx, completeVolume);
        }

        private void Replay()
        {
            Time.timeScale = 1f;
            UiOverlay.Active = false;

            if (NetSession.InCoop)
            {
                if (NetSession.IsServer && CoopSession.Instance != null) CoopSession.Instance.StartGame();
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Leaving the session, shutting NGO down and loading the menu all belong together, so
        // all three screens hand off to the one place that does them in order.
        private void ToMainMenu() => RunExit.ToMainMenu();
    }
}

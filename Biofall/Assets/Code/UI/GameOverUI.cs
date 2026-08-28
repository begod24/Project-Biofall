using System.Collections;
using Biofall.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.UI
{
    public sealed class GameOverUI : MonoBehaviour
    {

        private IEventBus _bus;
        private IEventBus Bus => _bus ??= ServiceLocator.Get<IEventBus>();
        [SerializeField] private GameObject panel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Audio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip gameOverSfx;
        [Range(0f, 1f)] [SerializeField] private float gameOverVolume = 0.5f;

        [Header("Timing")]
        [Tooltip("Delay (seconds, real time) before the Game Over panel appears.")]
        [SerializeField] private float showDelay = 3f;

        private bool _shown;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ToMainMenu);
        }

        private void OnEnable()
        {
            Bus.Subscribe<PlayerDied>(OnPlayerDied);
            Bus.Subscribe<TeamWiped>(OnTeamWiped);
        }

        private void OnDisable()
        {
            Bus.Unsubscribe<PlayerDied>(OnPlayerDied);
            Bus.Unsubscribe<TeamWiped>(OnTeamWiped);
        }

        private void OnPlayerDied(PlayerDied _)
        {
            if (showDelay > 0f) StartCoroutine(ShowAfterDelay());
            else Show();
        }

        private void OnTeamWiped(TeamWiped _)
        {
            if (_shown) return;
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
            _shown = true;
            if (panel != null) panel.SetActive(true);
            UiOverlay.Active = true;
            Cursor.visible = true;
            if (restartButton != null) restartButton.interactable = !NetSession.InCoop;
            if (sfxSource != null && gameOverSfx != null)
                sfxSource.PlayOneShot(gameOverSfx, gameOverVolume);
        }

        private void Update()
        {
            if (!_shown || NetSession.InCoop) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                Restart();
        }

        private void Restart()
        {
            if (NetSession.InCoop) return;
            Time.timeScale = 1f;
            UiOverlay.Active = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Leaving the session, shutting NGO down and loading the menu all belong together, so
        // all three screens hand off to the one place that does them in order.
        private void ToMainMenu() => RunExit.ToMainMenu();
    }
}

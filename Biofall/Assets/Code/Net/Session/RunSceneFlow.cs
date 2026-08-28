using Biofall.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biofall.Net
{
    // Drives scene changes off the replicated phase, then reports back so the server can hold
    // InRun until the slowest machine has the scene up. Without that handshake a fast machine
    // spawns players into a scene a slow machine has not finished loading.
    //
    // Deviation from Office, deliberate: Office keeps NetworkConfig.EnableSceneManagement OFF
    // and loads additively itself. Biofall keeps it ON, because Mission_1_Coop holds an
    // in-scene NetworkObject (CoopMission) and NGO cannot resolve those on a remote client with
    // scene management off. So the server drives NGO's scene manager and clients follow it; the
    // composition root survives the Single load because it lives in DontDestroyOnLoad.
    public sealed class RunSceneFlow : MonoBehaviour
    {
        [SerializeField] private SessionDirector director;
        [Tooltip("Scene the squad waits in between runs.")]
        [SerializeField] private string lobbyScene = GameScenes.MainMenu;
        [Tooltip("Fallback only. The real run scene is named by the host and replicated by " +
                 "SessionDirector, so campaign and wave mode can share one session layer.")]
        [SerializeField] private string fallbackRunScene = GameScenes.MissionCoop;

        private NetworkManager _manager;
        private string _awaitingScene;

        private void Awake()
        {
            if (director != null) director.PhaseChanged += OnPhaseChanged;
        }

        private void OnDestroy()
        {
            if (director != null) director.PhaseChanged -= OnPhaseChanged;
            Unsubscribe();
        }

        private void OnPhaseChanged(GameState phase)
        {
            _manager = director != null ? director.NetworkManager : null;
            if (_manager == null || _manager.SceneManager == null) return;

            Subscribe();

            switch (phase)
            {
                case GameState.Loading:
                    string target = ResolveRunScene();
                    _awaitingScene = target;
                    ServerLoad(target);
                    break;

                case GameState.Lobby:
                    _awaitingScene = null;
                    ServerLoad(lobbyScene);
                    break;
            }
        }

        private string ResolveRunScene()
        {
            string named = director != null ? director.RunScene : null;
            return string.IsNullOrEmpty(named) ? fallbackRunScene : named;
        }

        // Only the server asks; NGO pushes the load to everyone, host included.
        private void ServerLoad(string sceneName)
        {
            if (director == null || !director.IsHostClient) return;
            if (string.IsNullOrEmpty(sceneName)) return;
            if (SceneManager.GetActiveScene().name == sceneName) return;

            _manager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        private void Subscribe()
        {
            Unsubscribe();
            _manager.SceneManager.OnSceneEvent += OnSceneEvent;
        }

        private void Unsubscribe()
        {
            if (_manager != null && _manager.SceneManager != null)
                _manager.SceneManager.OnSceneEvent -= OnSceneEvent;
        }

        // Each machine reports for itself once its own load is finished.
        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            if (_awaitingScene == null || sceneEvent.SceneName != _awaitingScene) return;
            if (sceneEvent.SceneEventType != SceneEventType.LoadComplete) return;
            if (director == null || !director.IsSpawned) return;

            _awaitingScene = null;
            director.ReportRunSceneReadyRpc();
        }
    }
}

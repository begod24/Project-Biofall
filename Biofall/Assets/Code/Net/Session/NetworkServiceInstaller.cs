using Biofall.Core;
using Biofall.Data;
using Unity.Netcode;
using UnityEngine;

namespace Biofall.Net
{
    // Biofall.Core cannot construct a network service -- it may not reference Biofall.Net. So
    // the composition root knows only the abstract ServiceInstaller, and this subclass, which
    // lives in the network assembly, registers the network services. Dependencies keep
    // pointing down.
    public sealed class NetworkServiceInstaller : ServiceInstaller
    {
        [Header("Wiring")]
        [Tooltip("Same-scene NetworkManager. Do NOT replace with .Singleton: the bootstrap runs " +
                 "at -10000 and the singleton is still null inside its Awake.")]
        [SerializeField] private NetworkManager networkManager;

        [Tooltip("Session prefab (SessionRoot + LobbyRoster + SessionDirector + PlayerSpawner " +
                 "+ RunSceneFlow). Spawned by the server when it starts.")]
        [SerializeField] private GameObject sessionPrefab;

        public override int Order => 100;

        private MultiplayerSessionService _sessionService;
        private LobbyService _lobbyService;
        private bool _subscribed;

        public override void Install()
        {
            _sessionService = new MultiplayerSessionService();
            ServiceLocator.Register<ISessionService>(_sessionService);

            _lobbyService = new LobbyService();
            ServiceLocator.Register<ILobbyService>(_lobbyService);

            if (networkManager == null)
            {
                Debug.LogError("[Net] NetworkServiceInstaller has no NetworkManager assigned. " +
                               "Co-op cannot start.");
                return;
            }

            networkManager.OnServerStarted += OnServerStarted;
            networkManager.OnClientStopped += OnClientStopped;
            _subscribed = true;
        }

        public override void Uninstall()
        {
            if (_subscribed && networkManager != null)
            {
                networkManager.OnServerStarted -= OnServerStarted;
                networkManager.OnClientStopped -= OnClientStopped;
                _subscribed = false;
            }

            _lobbyService?.Unbind();

            ServiceLocator.Unregister<ILobbyService>();
            ServiceLocator.Unregister<ISessionService>();
        }

        private void OnServerStarted()
        {
            if (sessionPrefab == null)
            {
                Debug.LogError("[Net] No session prefab assigned; the lobby cannot come up.");
                return;
            }

            GameObject instance = Instantiate(sessionPrefab);
            var networkObject = instance.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogError("[Net] Session prefab has no NetworkObject.");
                Destroy(instance);
                return;
            }

            networkObject.Spawn();
        }

        // Losing the host returns a non-host client to the menu. This lives in the boot scene
        // deliberately: by now the session object is despawned, so nothing riding on it could
        // react. Unloading everything but boot avoids naming a scene -- which run scene was up
        // depends on the mission, and the network layer has no business knowing that list.
        private async void OnClientStopped(bool wasHost)
        {
            if (wasHost) return;

            if (ServiceLocator.TryGet<ISceneLoader>(out var loader))
                await loader.ReturnToAsync(GameScenes.Boot);

            if (ServiceLocator.TryGet<IGameStateService>(out var state))
                state.SetFromAuthority(GameState.MainMenu);

            if (ServiceLocator.TryGet<ISceneLoader>(out var menuLoader))
                await menuLoader.LoadAdditiveAsync(GameScenes.MainMenu, setActive: true);
        }
    }
}

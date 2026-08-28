using System;
using System.Collections.Generic;
using Biofall.Core;
using Biofall.Data;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Biofall.Net
{
    // Owns the run loop. The server is the only thing that decides a transition; it validates
    // against the same GameStateMachine table the local machine uses and writes the variable.
    // Every client, host included, applies what arrives.
    //
    //   Lobby --(host starts, everyone ready)--> Loading
    //                                              |
    //            each client loads the run scene, then reports ready
    //                                              |
    //                        all reported --------> InRun
    //                                              |
    //              mission ends --> RunComplete / RunFailed --> Lobby
    //
    // The scene-ready handshake is not optional: without it a fast machine spawns players into
    // a scene a slow machine has not finished loading.
    public sealed class SessionDirector : NetworkBehaviour
    {
        [SerializeField] private LobbyRoster roster;

        private readonly NetworkVariable<GameState> _phase = new(GameState.Lobby);

        // Which scene this run takes place in. Office has one run scene and a constant; Biofall
        // has several (campaign mission, wave mode), so the host names it when it starts the run
        // and every client reads it off the wire instead of guessing.
        private readonly NetworkVariable<FixedString64Bytes> _runScene = new();

        private readonly HashSet<ulong> _sceneReady = new();

        private const float TerminalDwellSeconds = 3.5f;

        private IGameStateService _gameState;
        private LobbyService _lobbyService;
        private bool _ending;

        public GameState Phase => _phase.Value;

        public string RunScene => _runScene.Value.ToString();

        public bool IsHostClient => IsServer;

        public event Action<GameState> PhaseChanged;

        public event Action<ulong> ClientReadyDuringRun;

        public override void OnNetworkSpawn()
        {
            ServiceLocator.TryGet(out _gameState);

            _phase.OnValueChanged += OnPhaseReplicated;

            if (IsServer)
            {
                _phase.Value = GameState.Lobby;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            }

            ApplyPhaseLocally(_phase.Value);

            if (ServiceLocator.TryGet<ILobbyService>(out var service) && service is LobbyService concrete)
            {
                _lobbyService = concrete;
                _lobbyService.Bind(this, roster);
            }
        }

        public override void OnNetworkDespawn()
        {
            _phase.OnValueChanged -= OnPhaseReplicated;

            if (IsServer)
            {
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
                _sceneReady.Clear();
            }

            _lobbyService?.Unbind();
            _lobbyService = null;

            _gameState?.SetFromAuthority(GameState.Lobby);
        }

        [Rpc(SendTo.Server)]
        public void RequestStartRunRpc(FixedString64Bytes runScene, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
            {
                Debug.LogWarning("[Session] A non-host client asked to start the run. Ignored.");
                return;
            }

            if (_phase.Value != GameState.Lobby) return;

            if (roster == null || !roster.AllReady)
            {
                Debug.Log("[Session] Start refused: not everyone is ready.");
                return;
            }

            if (runScene.Length == 0)
            {
                Debug.LogError("[Session] Start refused: no run scene named.");
                return;
            }

            _sceneReady.Clear();
            _runScene.Value = runScene;
            TrySetPhase(GameState.Loading);
        }

        [Rpc(SendTo.Server)]
        public void ReportRunSceneReadyRpc(RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            _sceneReady.Add(clientId);

            if (_phase.Value == GameState.InRun)
            {
                ClientReadyDuringRun?.Invoke(clientId);
                return;
            }

            if (_phase.Value != GameState.Loading) return;
            if (_sceneReady.Count < NetworkManager.ConnectedClientsIds.Count) return;

            TrySetPhase(GameState.InRun);
        }

        [Rpc(SendTo.Server)]
        public void RequestEndRunRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId) return;
            ServerEndRun(GameState.RunFailed);
        }

        public bool ServerEndRun(GameState terminal)
        {
            if (!IsServer || !IsSpawned) return false;

            if (terminal is not (GameState.RunComplete or GameState.RunFailed))
            {
                Debug.LogError($"[Session] '{terminal}' is not a terminal state. Ignored.");
                return false;
            }

            if (_phase.Value is not (GameState.InRun or GameState.Loading)) return false;
            if (_ending) return false;

            _ = EndRunAsync(terminal);
            return true;
        }

        private async Awaitable EndRunAsync(GameState terminal)
        {
            _ending = true;

            try
            {
                if (_phase.Value == GameState.InRun)
                {
                    TrySetPhase(terminal);

                    await Awaitable.NextFrameAsync();
                    if (this == null || !IsSpawned || !IsServer) return;

                    // Let the outcome screen sit before the lobby takes over.
                    await Awaitable.WaitForSecondsAsync(TerminalDwellSeconds);
                    if (this == null || !IsSpawned || !IsServer) return;
                }

                _sceneReady.Clear();
                roster?.ClearReadyFlags();
                TrySetPhase(GameState.Lobby);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _ending = false;
            }
        }

        private bool TrySetPhase(GameState next)
        {
            if (!IsServer) return false;
            if (next == _phase.Value) return true;

            if (!GameStateMachine.IsLegal(_phase.Value, next))
            {
                Debug.LogError($"[Session] Illegal transition {_phase.Value} -> {next}. Ignored.");
                return false;
            }

            _phase.Value = next;
            return true;
        }

        private void OnClientDisconnected(ulong clientId)
        {
            _sceneReady.Remove(clientId);

            if (_phase.Value == GameState.Loading &&
                _sceneReady.Count >= NetworkManager.ConnectedClientsIds.Count &&
                _sceneReady.Count > 0)
                TrySetPhase(GameState.InRun);
        }

        private void OnPhaseReplicated(GameState previous, GameState current) =>
            ApplyPhaseLocally(current);

        private void ApplyPhaseLocally(GameState current)
        {
            _gameState?.SetFromAuthority(current);
            PhaseChanged?.Invoke(current);
        }
    }
}

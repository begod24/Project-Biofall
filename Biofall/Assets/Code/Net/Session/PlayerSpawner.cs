using System.Collections.Generic;
using Biofall.Data;
using Unity.Netcode;
using UnityEngine;

namespace Biofall.Net
{
    // Bodies are created by the server when the run starts, not by NGO on connection, because
    // the lobby has no bodies in it. NetworkConfig.PlayerPrefab stays null for the same reason.
    public sealed class PlayerSpawner : NetworkBehaviour
    {
        [SerializeField] private SessionDirector director;

        [Tooltip("Networked player prefab. Must be registered in the NetworkPrefabs list.")]
        [SerializeField] private GameObject playerPrefab;

        private readonly List<NetworkObject> _spawned = new(4);

        private GameState _lastPhase = GameState.Lobby;

        private void Awake()
        {
            if (director == null) return;

            director.PhaseChanged += OnPhaseChanged;
            director.ClientReadyDuringRun += OnClientReadyDuringRun;
        }

        public override void OnDestroy()
        {
            if (director != null)
            {
                director.PhaseChanged -= OnPhaseChanged;
                director.ClientReadyDuringRun -= OnClientReadyDuringRun;
            }

            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            _spawned.Clear();
        }

        private void OnPhaseChanged(GameState phase)
        {
            if (!IsServer)
            {
                _lastPhase = phase;
                return;
            }

            bool wasInRun = _lastPhase == GameState.InRun;
            _lastPhase = phase;

            if (phase == GameState.InRun) SpawnMissingPlayers();
            else if (wasInRun) DespawnAll();
        }

        private void SpawnMissingPlayers()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[Spawn] PlayerSpawner has no player prefab assigned.");
                return;
            }

            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
                if (NeedsBody(clientId))
                    SpawnFor(clientId);
        }

        // A client that finished loading after the run already began still gets a body.
        private void OnClientReadyDuringRun(ulong clientId)
        {
            if (!IsServer || !IsSpawned || playerPrefab == null) return;
            if (!NeedsBody(clientId)) return;

            SpawnFor(clientId);
        }

        private bool NeedsBody(ulong clientId) =>
            NetworkManager.ConnectedClients.TryGetValue(clientId, out var client) &&
            client.PlayerObject == null;

        private void SpawnFor(ulong clientId)
        {
            Vector3 position = PlayerSpawnPoints.ResolveMission(clientId);

            GameObject instance = Instantiate(playerPrefab, position, Quaternion.identity);
            var networkObject = instance.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogError("[Spawn] Player prefab has no NetworkObject.");
                Destroy(instance);
                return;
            }

            networkObject.SpawnAsPlayerObject(clientId);
            _spawned.Add(networkObject);
        }

        private void DespawnAll()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                NetworkObject networkObject = _spawned[i];
                if (networkObject != null && networkObject.IsSpawned) networkObject.Despawn();
            }

            _spawned.Clear();
        }

        private void OnClientDisconnected(ulong clientId)
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                NetworkObject networkObject = _spawned[i];

                if (networkObject == null || networkObject.OwnerClientId == clientId)
                    _spawned.RemoveAt(i);
            }
        }
    }
}

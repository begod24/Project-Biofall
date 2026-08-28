using System;
using Unity.Collections;
using Unity.Netcode;

namespace Biofall.Net
{
    // Who is in the squad and who has pressed ready. Server owns the list; everyone reads it.
    public sealed class LobbyRoster : NetworkBehaviour
    {
        private readonly NetworkList<PlayerSlot> _slots = new();

        public event Action Changed;

        public int Count => _slots.Count;

        public PlayerSlot this[int index] => _slots[index];

        public bool AllReady
        {
            get
            {
                if (_slots.Count == 0) return false;

                for (int i = 0; i < _slots.Count; i++)
                    if (!_slots[i].IsReady)
                        return false;

                return true;
            }
        }

        public bool IsReady(ulong clientId) => TryFind(clientId, out int i) && _slots[i].IsReady;

        public override void OnNetworkSpawn()
        {
            _slots.OnListChanged += OnListChanged;

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

                foreach (ulong clientId in NetworkManager.ConnectedClientsIds) AddSlot(clientId);
            }

            Changed?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            _slots.OnListChanged -= OnListChanged;

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }

            Changed?.Invoke();
        }

        [Rpc(SendTo.Server)]
        public void SetReadyRpc(bool ready, RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!TryFind(clientId, out int index)) return;

            PlayerSlot slot = _slots[index];
            if (slot.IsReady == ready) return;

            slot.IsReady = ready;
            _slots[index] = slot;
        }

        public void ClearReadyFlags()
        {
            if (!IsServer) return;

            for (int i = 0; i < _slots.Count; i++)
            {
                PlayerSlot slot = _slots[i];
                if (!slot.IsReady) continue;

                slot.IsReady = false;
                _slots[i] = slot;
            }
        }

        private void OnClientConnected(ulong clientId) => AddSlot(clientId);

        private void OnClientDisconnected(ulong clientId)
        {
            if (!IsServer || !TryFind(clientId, out int index)) return;
            _slots.RemoveAt(index);
        }

        private void AddSlot(ulong clientId)
        {
            if (!IsServer || TryFind(clientId, out _)) return;
            _slots.Add(new PlayerSlot(clientId, BuildDisplayName(_slots.Count + 1)));
        }

        private static FixedString32Bytes BuildDisplayName(int ordinal)
        {
            FixedString32Bytes name = default;
            name.Append("OPERATIVE ");
            if (ordinal < 10) name.Append('0');
            name.Append(ordinal);
            return name;
        }

        private bool TryFind(ulong clientId, out int index)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].ClientId != clientId) continue;
                index = i;
                return true;
            }

            index = -1;
            return false;
        }

        private void OnListChanged(NetworkListEvent<PlayerSlot> _) => Changed?.Invoke();

        public override void OnDestroy()
        {
            _slots?.Dispose();
            base.OnDestroy();
        }
    }
}

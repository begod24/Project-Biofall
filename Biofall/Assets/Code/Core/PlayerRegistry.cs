using System.Collections.Generic;
using UnityEngine;

namespace Biofall.Core
{
    public static class PlayerRegistry
    {
        private static readonly List<Transform> _players = new(4);

        private static readonly HashSet<Transform> _downed = new();

        public static Transform LocalPlayer { get; private set; }

        // Offline there is one body and it is yours, so the first to register wins the local
        // slot. In a session the owner's body claims it explicitly through SetLocal, and
        // nothing else may -- the network layer turns this off when a session starts.
        public static bool AutoPromoteFirstToLocal { get; set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _players.Clear();
            _downed.Clear();
            LocalPlayer = null;
            AutoPromoteFirstToLocal = true;
        }

        public static Transform Player =>
            LocalPlayer != null ? LocalPlayer : (_players.Count > 0 ? _players[0] : null);

        public static bool HasPlayer => Player != null;

        public static IReadOnlyList<Transform> All => _players;

        public static void Register(Transform player)
        {
            if (player == null) return;
            if (!_players.Contains(player)) _players.Add(player);
            if (LocalPlayer == null && AutoPromoteFirstToLocal) LocalPlayer = player;
        }

        public static void SetLocal(Transform player)
        {
            if (player == null) return;
            if (!_players.Contains(player)) _players.Add(player);
            LocalPlayer = player;
        }

        public static void Unregister(Transform player)
        {
            _players.Remove(player);
            _downed.Remove(player);
            if (LocalPlayer == player)
                LocalPlayer = _players.Count > 0 ? _players[0] : null;
        }

        public static void SetDowned(Transform player, bool downed)
        {
            if (player == null) return;
            if (downed) _downed.Add(player);
            else _downed.Remove(player);
        }

        public static bool IsDowned(Transform player) => player != null && _downed.Contains(player);

        public static int AliveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _players.Count; i++)
                {
                    Transform p = _players[i];
                    if (p != null && !_downed.Contains(p)) n++;
                }
                return n;
            }
        }

        public static Transform NearestAlive(Vector3 position)
        {
            Transform best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < _players.Count; i++)
            {
                Transform p = _players[i];
                if (p == null || _downed.Contains(p)) continue;
                float sqr = (p.position - position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = p; }
            }
            return best;
        }

        public static Vector3 PositionOr(Vector3 fallback)
        {
            Transform p = Player;
            return p != null ? p.position : fallback;
        }

        public static Transform Nearest(Vector3 position)
        {
            Transform best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < _players.Count; i++)
            {
                Transform p = _players[i];
                if (p == null) continue;
                float sqr = (p.position - position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = p; }
            }
            return best;
        }
    }
}

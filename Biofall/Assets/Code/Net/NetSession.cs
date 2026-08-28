using Unity.Netcode;

namespace Biofall.Net
{
    public static class NetSession
    {
        private static bool _inCoop;

        public static bool InCoop
        {
            get => _inCoop;
            internal set
            {
                _inCoop = value;
                // The owner's body claims the local slot explicitly once a session is up.
                Core.PlayerRegistry.AutoPromoteFirstToLocal = !value;
            }
        }

        private static NetworkManager NM => NetworkManager.Singleton;

        public static bool IsServer => InCoop && NM != null && NM.IsServer;
        public static bool IsClient => InCoop && NM != null && NM.IsClient;
        public static bool IsHost   => InCoop && NM != null && NM.IsHost;

        public static bool HasAuthority => !InCoop || IsServer;
    }
}

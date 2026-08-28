using Unity.Netcode;

namespace Biofall.Net
{
    public static class NetSession
    {
        private static bool _inCoop;

        // Set only by the legacy LAN stack (NetworkBootstrap.StartHost/StartClient), which the
        // session layer replaced and nothing calls any more -- so this reads false for every
        // run, solo and squad alike. Stage 3 of the migration deletes the flag and the ~45
        // branches on it; until then, treat a false here as "unknown", not as "offline".
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

        // These ask the network manager rather than the flag above. They used to read
        // `InCoop && NM.IsServer`, which made every one of them permanently false: the host
        // that solo and co-op both run on could never see itself as the server, so
        // CoopEnemySpawner never started a wave and CoopLootService never dropped anything.
        //
        // That leaves IsServer true while InCoop is false. The pair looks contradictory and is:
        // it is the honest description of where the project stands mid-migration -- every run
        // is a session of one or more, and nothing is "co-op" in the old LAN sense any more.
        public static bool IsServer => NM != null && NM.IsServer;
        public static bool IsClient => NM != null && NM.IsClient;
        public static bool IsHost   => NM != null && NM.IsHost;

        public static bool HasAuthority => !InCoop || IsServer;
    }
}

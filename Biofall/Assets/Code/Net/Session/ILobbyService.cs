using System;
using Biofall.Data;

namespace Biofall.Net
{
    // How the UI talks to the lobby. Screens above Biofall.Net use this and never reach into
    // SessionDirector or LobbyRoster directly.
    public interface ILobbyService
    {
        bool IsAvailable { get; }
        bool IsHost { get; }
        ulong LocalClientId { get; }
        GameState Phase { get; }
        int PlayerCount { get; }
        bool LocalIsReady { get; }
        bool AllReady { get; }

        event Action Changed;

        bool TryGetSlot(int index, out PlayerSlot slot);
        void SetReady(bool ready);
        void RequestStartRun();
        void RequestEndRun();
    }
}

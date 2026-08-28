using System;
using System.Threading.Tasks;
using Biofall.Data;

namespace Biofall.Net
{
    // The connection itself: Relay, the join code, sign-in. Never throws at its caller --
    // failures land in Phase and LastError so the menu can show them.
    public interface ISessionService
    {
        SessionPhase Phase { get; }
        string JoinCode { get; }
        string LastError { get; }
        bool IsHost { get; }
        int PlayerCount { get; }
        int MaxPlayers { get; }

        event Action<SessionPhase> PhaseChanged;

        Task<bool> HostAsync(int maxPlayers, string sessionName);
        Task<bool> JoinAsync(string joinCode);
        Task LeaveAsync();
    }
}

using System;
using Biofall.Data;

namespace Biofall.Core
{
    public interface IGameStateService
    {
        GameState Current { get; }

        event Action<GameState> Changed;

        // Applied from the replicated phase. The local machine is a mirror, never a second
        // source of truth -- which is why there is no public TryChange for gameplay code.
        void SetFromAuthority(GameState state);
    }
}

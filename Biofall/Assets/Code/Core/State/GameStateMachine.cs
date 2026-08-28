using System;
using Biofall.Data;
using UnityEngine;

namespace Biofall.Core
{
    public sealed class GameStateMachine : IGameStateService
    {
        private readonly IEventBus _bus;

        public GameState Current { get; private set; } = GameState.Boot;

        public event Action<GameState> Changed;

        public GameStateMachine(IEventBus bus) => _bus = bus;

        // The same table the server validates against, so a transition cannot be legal on one
        // machine and illegal on another. Note there is deliberately no InRun -> Lobby edge:
        // aborting a run passes through RunFailed, so a run can never end without reaching a
        // terminal state.
        public static bool IsLegal(GameState from, GameState to)
        {
            if (from == to) return true;

            return (from, to) switch
            {
                (GameState.Boot, GameState.MainMenu) => true,
                (GameState.MainMenu, GameState.Lobby) => true,
                (GameState.Lobby, GameState.Loading) => true,
                (GameState.Lobby, GameState.MainMenu) => true,
                (GameState.Loading, GameState.InRun) => true,
                (GameState.Loading, GameState.RunFailed) => true,
                (GameState.InRun, GameState.RunComplete) => true,
                (GameState.InRun, GameState.RunFailed) => true,
                (GameState.RunComplete, GameState.Lobby) => true,
                (GameState.RunFailed, GameState.Lobby) => true,
                (GameState.RunComplete, GameState.MainMenu) => true,
                (GameState.RunFailed, GameState.MainMenu) => true,
                _ => false,
            };
        }

        public void SetFromAuthority(GameState state)
        {
            if (Current == state) return;

            if (!IsLegal(Current, state))
                Debug.LogWarning($"[GameState] Authority sent {Current} -> {state}, which the " +
                                 "local table calls illegal. Applying anyway: the server decides.");

            Current = state;
            Changed?.Invoke(state);
            _bus?.Publish(new GameStateChanged(state));
        }
    }

    public readonly struct GameStateChanged
    {
        public readonly GameState State;
        public GameStateChanged(GameState state) { State = state; }
    }
}

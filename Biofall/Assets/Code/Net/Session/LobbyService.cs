using System;
using Biofall.Data;

namespace Biofall.Net
{
    // A stable handle the UI can hold across the session object coming and going.
    public sealed class LobbyService : ILobbyService
    {
        private SessionDirector _director;
        private LobbyRoster _roster;

        public bool IsAvailable => _director != null && _director.IsSpawned && _roster != null;

        public bool IsHost => IsAvailable && _director.IsHostClient;

        public ulong LocalClientId => IsAvailable ? _director.NetworkManager.LocalClientId : 0;

        public GameState Phase => IsAvailable ? _director.Phase : GameState.MainMenu;

        public int PlayerCount => IsAvailable ? _roster.Count : 0;

        public bool AllReady => IsAvailable && _roster.AllReady;

        public bool LocalIsReady =>
            IsAvailable && _roster.IsReady(_director.NetworkManager.LocalClientId);

        public event Action Changed;

        public bool TryGetSlot(int index, out PlayerSlot slot)
        {
            if (!IsAvailable || index < 0 || index >= _roster.Count)
            {
                slot = default;
                return false;
            }

            slot = _roster[index];
            return true;
        }

        public void SetReady(bool ready)
        {
            if (IsAvailable) _roster.SetReadyRpc(ready);
        }

        public string RunScene => IsAvailable ? _director.RunScene : string.Empty;

        public void RequestStartRun(string runScene)
        {
            if (IsAvailable) _director.RequestStartRunRpc(runScene);
        }

        public void RequestEndRun()
        {
            if (IsAvailable) _director.RequestEndRunRpc();
        }

        internal void Bind(SessionDirector director, LobbyRoster roster)
        {
            Unbind();

            _director = director;
            _roster = roster;

            if (_roster != null) _roster.Changed += RaiseChanged;
            if (_director != null) _director.PhaseChanged += OnPhaseChanged;

            RaiseChanged();
        }

        internal void Unbind()
        {
            if (_roster != null) _roster.Changed -= RaiseChanged;
            if (_director != null) _director.PhaseChanged -= OnPhaseChanged;

            _director = null;
            _roster = null;

            RaiseChanged();
        }

        private void OnPhaseChanged(GameState _) => RaiseChanged();

        private void RaiseChanged() => Changed?.Invoke();
    }
}

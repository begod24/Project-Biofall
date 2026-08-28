using System;
using System.Threading.Tasks;
using Biofall.Data;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Biofall.Net
{
    // The Sessions API wraps Relay, Lobby and the NGO handshake behind one
    // CreateSessionAsync(...).WithRelayNetwork() call. It replaces LanDiscovery's 194 lines of
    // UDP broadcast, and it plays over the internet rather than one subnet.
    //
    // This never throws at its caller: failures land in Phase and LastError.
    public sealed class MultiplayerSessionService : ISessionService
    {
        private ISession _session;
        private SessionPhase _phase = SessionPhase.Offline;

        public SessionPhase Phase
        {
            get => _phase;
            private set
            {
                if (_phase == value) return;
                _phase = value;
                PhaseChanged?.Invoke(value);
            }
        }

        public string JoinCode => _session?.Code ?? string.Empty;
        public string LastError { get; private set; } = string.Empty;
        public bool IsHost => _session?.IsHost ?? false;
        public int PlayerCount => _session?.PlayerCount ?? 0;
        public int MaxPlayers => _session?.MaxPlayers ?? 0;

        public event Action<SessionPhase> PhaseChanged;

        public async Task<bool> HostAsync(int maxPlayers, string sessionName)
        {
            if (_session != null)
            {
                LastError = "Already in a session.";
                return false;
            }

            if (!await EnsureSignedInAsync()) return false;

            Phase = SessionPhase.Creating;

            try
            {
                var options = new SessionOptions
                {
                    Name = string.IsNullOrWhiteSpace(sessionName) ? "BIOFALL Squad" : sessionName,
                    MaxPlayers = Mathf.Clamp(maxPlayers, 1, 4),
                    IsPrivate = true
                }.WithRelayNetwork();

                _session = await MultiplayerService.Instance.CreateSessionAsync(options);
                Bind(_session);

                Phase = SessionPhase.InSession;
                Debug.Log($"[Session] Hosting. Join code: {_session.Code}");
                return true;
            }
            catch (Exception e)
            {
                Fail("Could not create the session.", e);
                return false;
            }
        }

        public async Task<bool> JoinAsync(string joinCode)
        {
            if (_session != null)
            {
                LastError = "Already in a session.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                LastError = "Enter a join code.";
                Phase = SessionPhase.Failed;
                return false;
            }

            if (!await EnsureSignedInAsync()) return false;

            Phase = SessionPhase.Joining;

            try
            {
                _session = await MultiplayerService.Instance
                    .JoinSessionByCodeAsync(joinCode.Trim().ToUpperInvariant());
                Bind(_session);

                Phase = SessionPhase.InSession;
                Debug.Log($"[Session] Joined {_session.Id}.");
                return true;
            }
            catch (Exception e)
            {
                Fail("Could not join that session. Check the code.", e);
                return false;
            }
        }

        public async Task LeaveAsync()
        {
            if (_session == null)
            {
                Phase = SessionPhase.Offline;
                return;
            }

            Phase = SessionPhase.Leaving;

            try
            {
                Unbind(_session);
                await _session.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Session] Leave reported an error, continuing: {e.Message}");
            }
            finally
            {
                _session = null;
                Phase = SessionPhase.Offline;
            }
        }

        private async Task<bool> EnsureSignedInAsync()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    var options = new InitializationOptions();
                    options.SetProfile(ResolveProfileName());
                    await UnityServices.InitializeAsync(options);
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    Phase = SessionPhase.Initialising;
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                return true;
            }
            catch (Exception e)
            {
                Fail("Could not reach Unity Gaming Services. Check the project link and network.", e);
                return false;
            }
        }

        // Multiplayer Play Mode virtual players are separate processes sharing one project
        // folder. Without distinct profiles they all authenticate as the same anonymous player
        // and the second one evicts the first.
        private static string ResolveProfileName()
        {
#if UNITY_EDITOR
            int id = System.Diagnostics.Process.GetCurrentProcess().Id;
            return $"editor{id}";
#else
            return "player";
#endif
        }

        private void Bind(ISession target)
        {
            target.RemovedFromSession += OnRemovedFromSession;
            target.Deleted += OnSessionDeleted;
        }

        private void Unbind(ISession target)
        {
            target.RemovedFromSession -= OnRemovedFromSession;
            target.Deleted -= OnSessionDeleted;
        }

        private void OnRemovedFromSession()
        {
            LastError = "You were removed from the session.";
            ClearSession();
        }

        private void OnSessionDeleted()
        {
            LastError = "The host closed the session.";
            ClearSession();
        }

        private void ClearSession()
        {
            if (_session != null) Unbind(_session);
            _session = null;
            Phase = SessionPhase.Offline;
        }

        private void Fail(string userMessage, Exception e)
        {
            LastError = userMessage;
            Phase = SessionPhase.Failed;
            Debug.LogError($"[Session] {userMessage}\n{e}");
        }
    }
}

using Biofall.Core;
using Biofall.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biofall.Net
{
    // The single way out of a run.
    //
    // Every quit-to-menu button used to load the menu scene directly, through a branch that
    // tested the dead NetSession.InCoop flag. Nothing told the session layer, so the UGS
    // session stayed open and the next host attempt answered "Already in a session.", while
    // NGO kept listening and the director sat in InRun forever.
    //
    // Solo is a session of one, so there is no offline shortcut to take here: leaving a run
    // means leaving the session, whether one player was in it or four.
    public static class RunExit
    {
        public static async void ToMainMenu()
        {
            Time.timeScale = 1f;
            UiOverlay.Active = false;
            Cursor.visible = true;

            if (ServiceLocator.TryGet<ISessionService>(out var session))
                await session.LeaveAsync();

            // The Sessions API tears the network handler down with the session. This is the
            // backstop for the paths it does not own -- a host that never finished creating,
            // or a NetworkManager someone started by hand.
            var manager = NetworkManager.Singleton;
            if (manager != null && manager.IsListening) manager.Shutdown();

            // Safe as a plain load now: with NGO down there is no network scene manager left
            // to disagree about which scene everyone is in.
            SceneManager.LoadScene(GameScenes.MainMenu);
        }
    }
}

namespace Biofall.Data
{
    // The run loop. The server is the only thing that decides a transition; every client,
    // host included, applies what arrives. One decision point, one code path.
    public enum GameState : byte
    {
        Boot = 0,
        MainMenu = 1,
        Lobby = 2,
        Loading = 3,
        InRun = 4,
        RunComplete = 5,
        RunFailed = 6
    }
}

namespace Biofall.Data
{
    // Where the connection itself is. Distinct from GameState, which is where the game is.
    public enum SessionPhase : byte
    {
        Offline = 0,
        Initialising = 1,
        Creating = 2,
        Joining = 3,
        InSession = 4,
        Leaving = 5,
        Failed = 6
    }
}

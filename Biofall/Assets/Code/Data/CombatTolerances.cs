namespace Biofall.Data
{
    // The slack the server allows an honest client. Movement and aim are owner-authoritative,
    // so by the time a hit request lands the server's copy of both bodies has moved on; without
    // slack, ordinary latency would cost real hits. Office keeps the same numbers in a
    // CombatConfig ScriptableObject -- constants here until they need to be tuned per weapon.
    public static class CombatTolerances
    {
        // Multiplies the weapon's authored range when the server re-checks reach.
        public const float Reach = 1.35f;

        // Fraction of the weapon's cooldown the server will forgive on rate-of-fire.
        public const float Cooldown = 0.25f;
    }
}

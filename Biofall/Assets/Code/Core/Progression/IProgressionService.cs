using Biofall.Data;
namespace Biofall.Core
{
    // Meta-progression that outlives a run: the bank and the purchased upgrade levels.
    public interface IProgressionService
    {
        int BankedSamples { get; }
        UpgradeCatalog Catalog { get; }

        event System.Action Changed;

        void DepositRunSamples(int amount);
        int GetLevel(string id);
        int GetLevel(UpgradeData data);
        bool CanPurchase(UpgradeData data);
        bool TryPurchase(UpgradeData data);
        void ResetProgress();

        float MaxHealthBonus { get; }
        float MoveSpeedMultiplier { get; }
        float HealthRegenPerSecond { get; }
        float ReviveHoldMultiplier { get; }
        int GrenadeCapacityBonus { get; }
        float PickupRadiusBonus { get; }
    }
}

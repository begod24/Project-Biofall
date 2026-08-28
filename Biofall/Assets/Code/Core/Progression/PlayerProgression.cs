using System;
using Biofall.Data;
using UnityEngine;

namespace Biofall.Core
{
    // Same PlayerPrefs keys as the old static class, so a player's bank and upgrade levels
    // survive the refactor. The store and the catalog are injected, which is what makes the
    // stat maths testable without a live editor.
    public sealed class PlayerProgression : IProgressionService
    {
        private const string BankKey = "bf_bank_samples";
        private const string LevelKeyPrefix = "bf_upg_";

        private readonly IProgressionStore _store;
        private readonly UpgradeCatalog _catalog;

        public event Action Changed;

        public int BankedSamples { get; private set; }

        public UpgradeCatalog Catalog => _catalog;

        public PlayerProgression(IProgressionStore store, UpgradeCatalog catalog)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _catalog = catalog;

            if (_catalog == null)
                Debug.LogWarning("[Progression] No UpgradeCatalog supplied — upgrades are inert.");

            BankedSamples = Mathf.Max(0, _store.GetInt(BankKey, 0));
        }

        public void DepositRunSamples(int amount)
        {
            if (amount <= 0) return;

            BankedSamples += amount;
            _store.SetInt(BankKey, BankedSamples);
            _store.Save();
            Changed?.Invoke();
        }

        public int GetLevel(string id) =>
            string.IsNullOrEmpty(id) ? 0 : _store.GetInt(LevelKeyPrefix + id, 0);

        public int GetLevel(UpgradeData data) => data != null ? GetLevel(data.id) : 0;

        public bool CanPurchase(UpgradeData data)
        {
            if (data == null) return false;

            int cost = data.CostForNext(GetLevel(data.id));
            return cost >= 0 && BankedSamples >= cost;
        }

        public bool TryPurchase(UpgradeData data)
        {
            if (!CanPurchase(data)) return false;

            int level = GetLevel(data.id);
            int cost = data.CostForNext(level);

            BankedSamples -= cost;
            _store.SetInt(BankKey, BankedSamples);
            _store.SetInt(LevelKeyPrefix + data.id, level + 1);
            _store.Save();
            Changed?.Invoke();
            return true;
        }

        public void ResetProgress()
        {
            if (_catalog != null && _catalog.Upgrades != null)
                foreach (var u in _catalog.Upgrades)
                    if (u != null && !string.IsNullOrEmpty(u.id))
                        _store.DeleteKey(LevelKeyPrefix + u.id);

            BankedSamples = 0;
            _store.SetInt(BankKey, 0);
            _store.Save();
            Changed?.Invoke();
        }

        private float Sum(UpgradeStat stat, UpgradeApply apply)
        {
            if (_catalog == null || _catalog.Upgrades == null) return 0f;

            float total = 0f;
            foreach (var u in _catalog.Upgrades)
            {
                if (u == null || u.stat != stat || u.apply != apply) continue;
                total += u.ValueAtLevel(GetLevel(u.id));
            }
            return total;
        }

        public float MaxHealthBonus => Sum(UpgradeStat.MaxHealth, UpgradeApply.Flat);

        public float MoveSpeedMultiplier => 1f + Sum(UpgradeStat.MoveSpeed, UpgradeApply.Percent);

        public float HealthRegenPerSecond => Sum(UpgradeStat.HealthRegen, UpgradeApply.Flat);

        public float ReviveHoldMultiplier =>
            Mathf.Clamp(1f - Sum(UpgradeStat.ReviveSpeed, UpgradeApply.Percent), 0.25f, 1f);

        public int GrenadeCapacityBonus =>
            Mathf.RoundToInt(Sum(UpgradeStat.GrenadeCapacity, UpgradeApply.Flat));

        public float PickupRadiusBonus => Sum(UpgradeStat.PickupRadius, UpgradeApply.Flat);
    }
}

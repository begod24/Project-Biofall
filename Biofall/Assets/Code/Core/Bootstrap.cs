using UnityEngine;

namespace Biofall.Core
{
    [DefaultExecutionOrder(-1000)]
    public sealed class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            EventBus.Clear();
            CurrencyWallet.Reset();

            EnsurePoolService();
        }

        private void EnsurePoolService()
        {
            if (PoolService.Instance != null) return;
            var go = new GameObject("PoolService");
            go.AddComponent<PoolService>();
        }
    }
}

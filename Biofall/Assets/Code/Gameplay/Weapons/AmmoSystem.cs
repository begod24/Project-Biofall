using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    public sealed class AmmoSystem : MonoBehaviour
    {
        [SerializeField] private int magazineSize = 12;
        [SerializeField] private int rounds = 12;
        [SerializeField] private int reserve = 48;

        public int Rounds => rounds;
        public int Reserve => reserve;
        public int MagazineSize => magazineSize;

        private bool _infinite;

        public void SetInfinite(bool infinite)
        {
            _infinite = infinite;
            Broadcast();
        }

        private void Start()
        {
            Broadcast();
        }

        private void OnEnable()
        {
            Broadcast();
        }

        public bool TryConsume(int amount = 1)
        {
            if (amount <= 0 || rounds < amount) return false;

            rounds -= amount;
            Broadcast();
            return true;
        }

        public void Reload()
        {
            int need = magazineSize - rounds;
            if (need <= 0 || reserve <= 0) return;

            int moved = Mathf.Min(need, reserve);
            rounds += moved;
            reserve -= moved;
            Broadcast();
        }

        // Re-publishes the current counts. Used to reclaim the shared HUD after a remote
        // player's body broadcast its own ammo on spawn.
        public void RefreshHud() => Broadcast();

        public void AddRounds(int amount)
        {
            if (amount <= 0) return;

            reserve += amount;
            Broadcast();
        }

        private void Broadcast()
        {
            EventBus.Publish(new AmmoChanged(rounds, reserve, _infinite));
        }
    }
}

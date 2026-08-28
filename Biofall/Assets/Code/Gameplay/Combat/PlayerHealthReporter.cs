using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay
{
    [RequireComponent(typeof(Health))]
    public sealed class PlayerHealthReporter : MonoBehaviour
    {

        private IEventBus _bus;
        private IEventBus Bus => _bus ??= ServiceLocator.Get<IEventBus>();
        private Health _health;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Healed += OnHealed;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Healed -= OnHealed;
            _health.Died -= OnDied;
        }

        private void Start() => RefreshHud();

        public void RefreshHud()
        {
            if (_health == null) return;
            Bus.Publish(new PlayerDamaged(_health.Current, _health.Max, 0f));
        }

        private void OnDamaged(DamageInfo info, float current)
            => Bus.Publish(new PlayerDamaged(current, _health.Max, info.Amount));

        private void OnHealed(float current, float max)
            => Bus.Publish(new PlayerDamaged(current, max, 0f));

        private void OnDied()
        {
            if (NetSession.InCoop) return;
            Bus.Publish(new PlayerDied());
        }
    }
}

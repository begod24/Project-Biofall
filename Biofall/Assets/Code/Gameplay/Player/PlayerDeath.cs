using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    public sealed class PlayerDeath : MonoBehaviour
    {

        private IEventBus _bus;
        private IEventBus Bus => _bus ??= ServiceLocator.Get<IEventBus>();
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Weapon weapon;

        private static readonly int DieId = Animator.StringToHash("Die");

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (controller == null) controller = GetComponent<PlayerController>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (weapon == null) weapon = GetComponentInChildren<Weapon>();
        }

        private void OnEnable() => Bus.Subscribe<PlayerDied>(OnPlayerDied);
        private void OnDisable() => Bus.Unsubscribe<PlayerDied>(OnPlayerDied);

        private void OnPlayerDied(PlayerDied _)
        {
            if (animator != null) animator.SetTrigger(DieId);
            if (controller != null) controller.enabled = false;
            if (motor != null) motor.enabled = false;
            if (weapon != null) weapon.enabled = false;
        }
    }
}

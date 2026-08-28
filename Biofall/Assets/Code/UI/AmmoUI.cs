using UnityEngine;
using TMPro;
using Biofall.Core;

namespace Biofall.UI
{
    public sealed class AmmoUI : MonoBehaviour
    {

        private IEventBus _bus;
        private IEventBus Bus => _bus ??= ServiceLocator.Get<IEventBus>();
        [SerializeField] private TMP_Text text;

        private void Awake()
        {
            if (text == null) text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            Bus.Subscribe<AmmoChanged>(OnAmmoChanged);
        }

        private void OnDisable()
        {
            Bus.Unsubscribe<AmmoChanged>(OnAmmoChanged);
        }

        private void OnAmmoChanged(AmmoChanged e)
        {
            if (text == null) return;
            text.text = e.Infinite ? "∞" : $"{e.InMagazine} / {e.InReserve}";
        }
    }
}

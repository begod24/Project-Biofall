using UnityEngine;
using TMPro;
using Biofall.Core;

namespace Biofall.UI
{
    public sealed class CurrencyUI : MonoBehaviour
    {

        private IEventBus _bus;
        private IEventBus Bus => _bus ??= ServiceLocator.Get<IEventBus>();
        private RunState _run;
        private RunState Run => _run ??= ServiceLocator.Get<RunState>();
        [SerializeField] private TMP_Text text;

        private void Awake()
        {
            if (text == null) text = GetComponent<TMP_Text>();
            Refresh(Run.BioSamples);
        }

        private void OnEnable()
        {
            Bus.Subscribe<BioSamplesChanged>(OnChanged);
            Refresh(Run.BioSamples);
        }

        private void OnDisable() => Bus.Unsubscribe<BioSamplesChanged>(OnChanged);

        private void OnChanged(BioSamplesChanged e) => Refresh(e.Total);

        private void Refresh(int total)
        {
            if (text != null) text.text = $"BIO  {total}";
        }
    }
}

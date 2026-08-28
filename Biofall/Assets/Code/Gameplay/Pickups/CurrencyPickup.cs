using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    public sealed class CurrencyPickup : Pickup
    {

        private RunState _run;
        private RunState Run => _run ??= ServiceLocator.Get<RunState>();
        [SerializeField] private int amount = 1;

        protected override void OnCollected() => Run.AddBioSamples(amount);
    }
}

using System;

namespace Biofall.Core
{
    // What this run has accumulated so far. Replaces the static CurrencyWallet: a run is a
    // lifetime, not a global, and the bootstrap makes a fresh one when the session starts.
    public sealed class RunState
    {
        private readonly IEventBus _bus;

        public int BioSamples { get; private set; }

        public RunState(IEventBus bus) => _bus = bus ?? throw new ArgumentNullException(nameof(bus));

        public void AddBioSamples(int amount)
        {
            if (amount == 0) return;

            BioSamples += amount;
            _bus.Publish(new BioSamplesChanged(BioSamples, amount));
        }

        public void Reset()
        {
            BioSamples = 0;
            _bus.Publish(new BioSamplesChanged(0, 0));
        }
    }
}

using UnityEngine;
using Biofall.Core;
using Biofall.Gameplay.Mission1;

namespace Biofall.Gameplay
{
    public sealed class RunSampleBanker : MonoBehaviour
    {

        private IEventBus _bus;
        private IEventBus Bus => _bus ??= ServiceLocator.Get<IEventBus>();
        private IProgressionService _progression;
        private IProgressionService Progression =>
            _progression ??= ServiceLocator.Get<IProgressionService>();
        private RunState _run;
        private RunState Run => _run ??= ServiceLocator.Get<RunState>();
        private bool _banked;

        private void OnEnable() => Bus.Subscribe<MissionCompleted>(OnCompleted);
        private void OnDisable() => Bus.Unsubscribe<MissionCompleted>(OnCompleted);

        private void OnCompleted(MissionCompleted _)
        {
            if (_banked) return;
            _banked = true;
            Progression.DepositRunSamples(Run.BioSamples);
        }
    }
}

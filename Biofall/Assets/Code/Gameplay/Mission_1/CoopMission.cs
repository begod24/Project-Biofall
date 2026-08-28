using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay.Mission1
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class CoopMission : NetworkBehaviour
    {

        private IEventBus _bus;
        private IEventBus Bus => _bus ??= ServiceLocator.Get<IEventBus>();
        public static CoopMission Instance { get; private set; }

        [SerializeField] private GeneratorStation generator;
        [SerializeField] private BeaconStation beacon;

        private readonly NetworkVariable<MissionPhase> _phase = new(
            MissionPhase.FindGenerator,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private string _progLabel;
        private float _progValue;
        private bool _progActive;
        private bool _progDirty;
        private float _progSendTimer;

        public override void OnNetworkSpawn()
        {
            Instance = this;

            if (IsServer)
            {
                Bus.Subscribe<MissionPhaseChanged>(OnPhaseServer);
                Bus.Subscribe<MissionProgress>(OnProgressServer);
                Bus.Subscribe<GeneratorActivated>(OnGeneratorServer);
                Bus.Subscribe<BeaconActivated>(OnBeaconServer);
                Bus.Subscribe<BeaconCharged>(OnChargedServer);
                Bus.Subscribe<MissionCompleted>(OnCompletedServer);
            }
            else
            {
                _phase.OnValueChanged += OnPhaseClient;
                Bus.Publish(new MissionPhaseChanged(_phase.Value));
            }
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;

            if (IsServer)
            {
                Bus.Unsubscribe<MissionPhaseChanged>(OnPhaseServer);
                Bus.Unsubscribe<MissionProgress>(OnProgressServer);
                Bus.Unsubscribe<GeneratorActivated>(OnGeneratorServer);
                Bus.Unsubscribe<BeaconActivated>(OnBeaconServer);
                Bus.Unsubscribe<BeaconCharged>(OnChargedServer);
                Bus.Unsubscribe<MissionCompleted>(OnCompletedServer);
            }
            else
            {
                _phase.OnValueChanged -= OnPhaseClient;
            }
        }

        private void OnPhaseServer(MissionPhaseChanged e) => _phase.Value = e.Phase;

        private void OnProgressServer(MissionProgress e)
        {
            _progLabel = e.Label;
            _progValue = e.Value01;
            _progActive = e.Active;
            _progDirty = true;
        }

        private void OnGeneratorServer(GeneratorActivated _) => FactClientRpc(0);
        private void OnBeaconServer(BeaconActivated _) => FactClientRpc(1);
        private void OnChargedServer(BeaconCharged _) => FactClientRpc(2);
        private void OnCompletedServer(MissionCompleted _) => FactClientRpc(3);

        private void Update()
        {
            if (!IsServer || !_progDirty) return;

            _progSendTimer -= Time.deltaTime;
            if (_progActive && _progSendTimer > 0f) return;

            _progSendTimer = 0.1f;
            _progDirty = false;
            ProgressClientRpc(new FixedString64Bytes(_progLabel ?? string.Empty), _progValue, _progActive);
        }

        private void OnPhaseClient(MissionPhase _, MissionPhase current) =>
            Bus.Publish(new MissionPhaseChanged(current));

        [Rpc(SendTo.NotServer)]
        private void ProgressClientRpc(FixedString64Bytes label, float value, bool active)
        {
            string l = label.Length > 0 ? label.ToString() : null;
            Bus.Publish(new MissionProgress(l, value, active));
        }

        [Rpc(SendTo.NotServer)]
        private void FactClientRpc(int id)
        {
            switch (id)
            {
                case 0: Bus.Publish(new GeneratorActivated()); break;
                case 1: Bus.Publish(new BeaconActivated()); break;
                case 2: Bus.Publish(new BeaconCharged()); break;
                case 3: Bus.Publish(new MissionCompleted()); break;
            }
        }

        [Rpc(SendTo.Server)]
        public void RequestGeneratorRpc()
        {
            if (generator != null) generator.ServerInteract();
        }

        [Rpc(SendTo.Server)]
        public void RequestBeaconRpc()
        {
            if (beacon != null) beacon.ServerInteract();
        }
    }
}

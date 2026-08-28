using Unity.Netcode;
using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Enemy))]
    [RequireComponent(typeof(Health))]
    public sealed class CoopEnemy : NetworkBehaviour
    {
        private Enemy _enemy;
        private Health _health;
        private EnemyMovement _movement;
        private Animator _animator;
        private Transform _tf;
        private Vector3 _lastPos;

        private readonly NetworkVariable<float> _hp01 = new(
            1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private static readonly int SpeedId = Animator.StringToHash("Speed");

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _health = GetComponent<Health>();
            _movement = GetComponent<EnemyMovement>();
            _animator = GetComponentInChildren<Animator>();
            _tf = transform;
        }

        public override void OnNetworkSpawn()
        {
            _lastPos = _tf.position;

            if (IsServer)
            {
                _enemy.OnSpawned();
                _enemy.Aggro();
                _enemy.DespawnRequested += ServerDespawn;
                _enemy.AttackTriggered += OnServerAttack;
                _health.Damaged += OnServerDamaged;
                _health.Died += OnServerDied;
            }
            else
            {
                _enemy.SuppressDespawn = true;
                if (_movement != null) _movement.enabled = false;
                if (_animator != null) { _animator.Rebind(); _animator.Update(0f); }

                _hp01.OnValueChanged += OnHpReplicated;
                if (_hp01.Value < 1f) _enemy.SetHealthBar(_hp01.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                _enemy.DespawnRequested -= ServerDespawn;
                _enemy.AttackTriggered -= OnServerAttack;
                _health.Damaged -= OnServerDamaged;
                _health.Died -= OnServerDied;
            }
            else
            {
                _hp01.OnValueChanged -= OnHpReplicated;
            }
        }

        private void OnHpReplicated(float previous, float current) => _enemy.SetHealthBar(current);

        private void OnServerDamaged(DamageInfo info, float current)
        {
            _hp01.Value = _health.Max > 0f ? current / _health.Max : 0f;
            ReactClientRpc(info.HitPoint, info.HitDirection, false);
        }

        private void OnServerDied()
        {
            _hp01.Value = 0f;
            ReactClientRpc(_tf.position, Vector3.up, true);
        }

        private void OnServerAttack() => AttackClientRpc();

        private void ServerDespawn()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned) NetworkObject.Despawn(true);
        }

        // Server-side entry point. The amount is decided by the server (see
        // CoopPlayer.RequestHitsRpc), never sent by the client.
        public void ServerApplyDamage(float amount, Vector3 point, Vector3 dir)
        {
            if (!IsServer) return;
            _health.TakeDamage(new DamageInfo(amount, point, dir, null));
        }

        // TODO (Stage 8): grenades are not networked yet, so the thrower still names its own
        // damage. This is the last unvalidated damage path left; it dies when ThrownGrenade
        // becomes a server-spawned NetworkObject.
        [Rpc(SendTo.Server)]
        public void UnvalidatedDamageRpc(float amount, Vector3 point, Vector3 dir)
        {
            _health.TakeDamage(new DamageInfo(amount, point, dir, null));
        }

        [Rpc(SendTo.NotServer)]
        private void ReactClientRpc(Vector3 point, Vector3 dir, bool died)
        {
            if (died) { _enemy.PlayDeathFx(); return; }
            _enemy.PlayHitFx(new DamageInfo(0f, point, dir, null));
        }

        [Rpc(SendTo.NotServer)]
        private void AttackClientRpc() => _enemy.PlayAttackFx();

        // The server picks where the acid lands and tells everyone. Clients used to compute it
        // themselves from interpolated player positions, so the pool drew in a different spot on
        // every screen while only the server's copy dealt damage.
        public void ServerBroadcastSpit(Vector3 landing)
        {
            if (IsServer) SpitClientRpc(landing);
        }

        [Rpc(SendTo.NotServer)]
        private void SpitClientRpc(Vector3 landing) =>
            GetComponent<SpitAcidAttack>()?.RenderSpit(landing);

        // Same for the scream: the server decides when a pulse goes out, clients only draw it.
        public void ServerBroadcastScream(Vector3 origin)
        {
            if (IsServer) ScreamClientRpc(origin);
        }

        [Rpc(SendTo.NotServer)]
        private void ScreamClientRpc(Vector3 origin) =>
            GetComponent<ScreamWaveAttack>()?.RenderWave(origin);

        private void Update()
        {
            if (IsServer) return;

            float dt = Time.deltaTime;
            Vector3 p = _tf.position;
            float speed = (p - _lastPos).magnitude / Mathf.Max(1e-4f, dt);
            _lastPos = p;

            if (_animator != null) _animator.SetFloat(SpeedId, speed > 0.15f ? 1f : 0f);
            _enemy.ClientFxTick(dt);
        }
    }
}

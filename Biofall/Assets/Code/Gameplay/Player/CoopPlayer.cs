using System.Collections;
using Biofall.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Biofall.Core;
using Biofall.Net;
using Biofall.Gameplay.Mission1;

namespace Biofall.Gameplay
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class CoopPlayer : NetworkBehaviour
    {
        private Coroutine _ownerSceneRefresh;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                SetLocalSimulation(false);
                PlayerRegistry.SetLocal(transform);
                GetComponent<PlayerLoadout>()?.Apply();
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneLoaded += OnSceneLoaded;
                SubscribeNetworkSceneEvents();
                RefreshOwnerForActiveScene();
            }
            else
            {
                SetLocalSimulation(false);
                PlayerRegistry.Register(transform);
                RefreshLocalHud();
            }
        }

        // A body broadcasts its ammo/grenades/health from Awake, which lands before
        // OnNetworkSpawn can silence a remote one. Re-assert the local player's values so the
        // HUD does not keep a teammate's numbers.
        private static void RefreshLocalHud()
        {
            Transform local = PlayerRegistry.LocalPlayer;
            if (local == null) return;

            local.GetComponent<PlayerHealthReporter>()?.RefreshHud();
            local.GetComponent<GrenadeInventory>()?.RefreshHud();
            local.GetComponent<WeaponController>()?.ActiveAmmo?.RefreshHud();
        }

        public override void OnNetworkDespawn()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnsubscribeNetworkSceneEvents();
            if (_ownerSceneRefresh != null)
            {
                StopCoroutine(_ownerSceneRefresh);
                _ownerSceneRefresh = null;
            }
            PlayerRegistry.Unregister(transform);
        }

        [Rpc(SendTo.Owner)]
        public void TakeDamageRpc(float amount, Vector3 from)
        {
            var health = GetComponent<Health>();
            if (health == null) return;
            Vector3 dir = transform.position - from;
            dir.y = 0f;
            health.TakeDamage(new DamageInfo(amount, transform.position, dir.normalized, null));
        }

        // ---- Hitscan: the client asks, the server decides -------------------------------
        //
        // Movement and aim are owner-authoritative, so the client's raycast is the only aim
        // that exists and the probe must run there. That makes the request untrusted, so the
        // server re-derives everything that matters -- exactly the split Office uses in
        // PlayerAttacker:
        //
        //   what was hit  -> server re-resolves the reference and re-checks reach from its copy
        //   which weapon  -> server reads the slot out of its own WeaponController
        //   how often     -> server keeps the cooldown clock, with tolerance for honest jitter
        //   how much      -> server reads damage from its own WeaponData
        //
        // Pellets travel as a count, not as one request each: a shotgun blast into a crowd used
        // to be one reliable RPC per pellet per enemy.
        private float _serverNextShotAllowed;

        public void RequestHits(int weaponSlot, CoopEnemy enemy, int pellets, Vector3 hitPoint, Vector3 dir)
        {
            if (!IsOwner || enemy == null) return;
            RequestHitsRpc(new NetworkObjectReference(enemy.NetworkObject), weaponSlot, pellets, hitPoint, dir);
        }

        [Rpc(SendTo.Server)]
        private void RequestHitsRpc(NetworkObjectReference target, int weaponSlot, int pellets,
            Vector3 hitPoint, Vector3 dir, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

            if (!target.TryGet(out NetworkObject targetObject)) return;
            var enemy = targetObject.GetComponent<CoopEnemy>();
            if (enemy == null) return;

            var controller = GetComponent<WeaponController>();
            Weapon weapon = controller != null ? controller.WeaponAt(weaponSlot) : null;
            if (weapon == null || weapon.Data == null) return;

            WeaponData weaponData = weapon.Data;

            // How often. One clock per player, shared by every target hit in the same shot.
            float cooldown = 1f / Mathf.Max(0.01f, weaponData.fireRate);
            float earliest = _serverNextShotAllowed - cooldown * CombatTolerances.Cooldown;
            if (Time.time < earliest) return;
            _serverNextShotAllowed = Time.time + cooldown;

            // How many. A shot cannot land more pellets than the weapon fires.
            int allowed = Mathf.Clamp(pellets, 1, Mathf.Max(1, weaponData.pelletsPerShot));

            // What was hit, from the server's own copy of both bodies.
            float reach = weaponData.range * CombatTolerances.Reach;
            if ((enemy.transform.position - transform.position).sqrMagnitude > reach * reach) return;

            enemy.ServerApplyDamage(weaponData.damage * allowed, hitPoint, dir);
        }

        public void BroadcastFireFx(int weaponSlot, Vector3 origin, Vector3 direction)
        {
            if (!IsOwner) return;
            FireFxRpc(weaponSlot, origin, direction);
        }

        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Unreliable)]
        private void FireFxRpc(int weaponSlot, Vector3 origin, Vector3 direction)
        {
            var wc = GetComponentInChildren<WeaponController>(true);
            if (wc == null) return;
            var weapon = wc.WeaponAt(weaponSlot);
            if (weapon != null) weapon.PlayRemoteFireFx(origin, direction);
        }

        public void SetControllable(bool enabled) => SetLocalSimulation(enabled);

        private void SetLocalSimulation(bool enabled)
        {
            if (!enabled) SetEnabled<PlayerController>(false);

            SetEnabled<PlayerInput>(enabled);
            SetEnabled<PlayerMotor>(enabled);
            SetEnabled<PlayerAim>(enabled);
            SetEnabled<WeaponController>(enabled);
            SetEnabled<GrenadeThrower>(enabled);
            SetEnabled<PlayerInteractor>(enabled);

            SetEnabled<PlayerAnimator>(enabled);
            SetEnabled<PlayerDeath>(enabled);

            // These three publish player-scoped state onto the shared EventBus, which the HUD
            // reads. A remote body must stay quiet or it overwrites the local readouts.
            SetEnabled<PlayerHealthReporter>(enabled);
            SetEnabled<GrenadeInventory>(enabled);
            foreach (var ammo in GetComponentsInChildren<AmmoSystem>(true))
                ammo.enabled = enabled;

            foreach (var weapon in GetComponentsInChildren<Weapon>(true))
                weapon.enabled = enabled;

            if (enabled) SetEnabled<PlayerController>(true);
        }

        private void SetEnabled<T>(bool enabled) where T : Behaviour
        {
            var c = GetComponent<T>();
            if (c != null) c.enabled = enabled;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsOwner) return;
            RefreshOwnerForActiveScene();
        }

        private void OnNetworkSceneEvent(SceneEvent sceneEvent)
        {
            if (!IsOwner) return;
            if (sceneEvent.SceneEventType == SceneEventType.LoadComplete ||
                sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted ||
                sceneEvent.SceneEventType == SceneEventType.SynchronizeComplete)
            {
                RefreshOwnerForActiveScene();
            }
        }

        private void RefreshOwnerForActiveScene()
        {
            if (_ownerSceneRefresh != null) StopCoroutine(_ownerSceneRefresh);
            _ownerSceneRefresh = StartCoroutine(RefreshOwnerForActiveSceneRoutine());
        }

        private IEnumerator RefreshOwnerForActiveSceneRoutine()
        {
            SetLocalSimulation(false);
            ResetMotion();
            PlayerRegistry.SetLocal(transform);

            yield return null;
            yield return null;

            for (int i = 0; i < 3; i++)
            {
                MoveToSpawn(PlayerSpawnPoints.ResolveMission(OwnerClientId));
                BindLocalCamera();
                yield return null;
            }

            SetLocalSimulation(true);
            PlayerRegistry.SetLocal(transform);
            _ownerSceneRefresh = null;
        }

        private void MoveToSpawn(Vector3 position)
        {
            var controller = GetComponent<CharacterController>();
            bool controllerWasEnabled = controller != null && controller.enabled;

            if (controller != null) controller.enabled = false;
            ResetMotion();
            transform.SetPositionAndRotation(position, Quaternion.identity);
            if (controller != null) controller.enabled = controllerWasEnabled;
        }

        private void ResetMotion()
        {
            var motor = GetComponent<PlayerMotor>();
            if (motor != null) motor.ResetVerticalVelocity();
        }

        private void BindLocalCamera()
        {
            var cam = FindFirstObjectByType<TopDownCamera>();
            if (cam != null) cam.SetTarget(transform);
        }

        private void SubscribeNetworkSceneEvents()
        {
            var nm = NetworkManager != null ? NetworkManager : Unity.Netcode.NetworkManager.Singleton;
            if (nm == null || nm.SceneManager == null) return;
            nm.SceneManager.OnSceneEvent -= OnNetworkSceneEvent;
            nm.SceneManager.OnSceneEvent += OnNetworkSceneEvent;
        }

        private void UnsubscribeNetworkSceneEvents()
        {
            var nm = NetworkManager != null ? NetworkManager : Unity.Netcode.NetworkManager.Singleton;
            if (nm == null || nm.SceneManager == null) return;
            nm.SceneManager.OnSceneEvent -= OnNetworkSceneEvent;
        }
    }
}

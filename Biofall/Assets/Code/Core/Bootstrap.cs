using System;
using Biofall.Data;
using System.Linq;
using UnityEngine;

namespace Biofall.Core
{
    // The composition root, ported from The Office's GameBootstrap. It registers the core
    // services, then runs every ServiceInstaller in ascending Order; teardown runs them back.
    //
    // Office keeps exactly one of these, in a boot scene that never unloads. Biofall still
    // loads scenes in single mode and is played by pressing Play inside a gameplay scene, so
    // this one survives across loads and the hasBooted guard makes a second copy a no-op --
    // whichever scene starts first becomes the root. Once Stage 6 brings the real scene flow,
    // dropping the extra copies is a scene-only change with no code impact.
    [DefaultExecutionOrder(-10000)]
    public sealed class Bootstrap : MonoBehaviour
    {
        [Header("Installers")]
        [Tooltip("One per assembly that owns services. Executed in ascending Order, " +
                 "torn down in reverse.")]
        [SerializeField] private ServiceInstaller[] installers = Array.Empty<ServiceInstaller>();

        [Tooltip("Prefab carrying the services that live above Core -- the network stack. The " +
                 "root instantiates it and runs every ServiceInstaller on it, so a scene entered " +
                 "directly gets the same set as the boot scene, and only the winning root ever " +
                 "creates a NetworkManager.")]
        [SerializeField] private GameObject servicesPrefab;

        [Header("Content")]
        [Tooltip("Upgrade catalogue driving meta-progression. Falls back to " +
                 "Resources/UpgradeCatalog when left empty.")]
        [SerializeField] private UpgradeCatalog upgradeCatalog;

        [Tooltip("The four operatives. Falls back to Resources/OperativeCatalog when empty.")]
        [SerializeField] private OperativeCatalog operativeCatalog;

        private static bool s_hasBooted;

        // Only the instance that actually booted may tear the services down. Without this a
        // duplicate in a later scene destroys itself, and its OnDestroy wipes the locator the
        // real root populated. Office never hits this because it keeps exactly one root.
        private bool _isRoot;

        private EventBus _eventBus;
        private ServiceInstaller[] _ordered = Array.Empty<ServiceInstaller>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_hasBooted = false;

        private void Awake()
        {
            if (s_hasBooted)
            {
                Destroy(gameObject);
                return;
            }

            s_hasBooted = true;
            _isRoot = true;
            DontDestroyOnLoad(gameObject);

            _eventBus = new EventBus();

            ServiceLocator.Register<IEventBus>(_eventBus);
            ServiceLocator.Register<ISceneLoader>(new SceneLoader(_eventBus));
            ServiceLocator.Register<IGameStateService>(new GameStateMachine(_eventBus));

            // Applies the stored resolution in its constructor, so it is built before anything
            // that reads a volume in its own Awake.
            ServiceLocator.Register<ISettingsService>(
                new GameSettingsService(new PlayerPrefsSettingsStore(), new ScreenDisplayDevice()));

            ServiceLocator.Register<IProgressionService>(
                new PlayerProgression(new PlayerPrefsProgressionStore(), ResolveCatalog()));

            ServiceLocator.Register(new RunState(_eventBus));

            var settingsStore = new PlayerPrefsSettingsStore();
            ServiceLocator.Register<IOperativeService>(
                new OperativeService(ResolveOperatives(), settingsStore));
            ServiceLocator.Register<ICampaignState>(new CampaignState(settingsStore));

            EnsurePoolService();

            var found = new System.Collections.Generic.List<ServiceInstaller>();
            foreach (var installer in installers)
                if (installer != null) found.Add(installer);

            if (servicesPrefab != null)
            {
                var services = Instantiate(servicesPrefab);
                services.name = servicesPrefab.name;
                DontDestroyOnLoad(services);
                found.AddRange(services.GetComponentsInChildren<ServiceInstaller>(true));
            }

            _ordered = found.OrderBy(i => i.Order).ToArray();
            foreach (var installer in _ordered) installer.Install();
        }

        private void OnDestroy()
        {
            if (!_isRoot) return;

            for (int i = _ordered.Length - 1; i >= 0; i--) _ordered[i].Uninstall();

            _eventBus?.Clear();
            ServiceLocator.Clear();
            s_hasBooted = false;
        }

        private OperativeCatalog ResolveOperatives()
        {
            if (operativeCatalog != null) return operativeCatalog;

            var loaded = Resources.Load<OperativeCatalog>("OperativeCatalog");
            if (loaded == null)
                Debug.LogWarning("[Bootstrap] No OperativeCatalog assigned and none at " +
                                 "Resources/OperativeCatalog — operative selection is inert.");
            return loaded;
        }

        private UpgradeCatalog ResolveCatalog()
        {
            if (upgradeCatalog != null) return upgradeCatalog;

            var loaded = Resources.Load<UpgradeCatalog>("UpgradeCatalog");
            if (loaded == null)
                Debug.LogWarning("[Bootstrap] No UpgradeCatalog assigned and none at " +
                                 "Resources/UpgradeCatalog — upgrades will be inert.");
            return loaded;
        }

        private static void EnsurePoolService()
        {
            if (PoolService.Instance != null) return;

            var go = new GameObject("PoolService");
            DontDestroyOnLoad(go);
            go.AddComponent<PoolService>();
        }
    }
}

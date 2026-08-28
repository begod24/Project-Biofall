using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biofall.Core
{
    // Reuses gameplay objects instead of allocating them: enemies, bullets, blood, acid,
    // grenades, muzzle flashes.
    //
    // The service itself is DontDestroyOnLoad -- it belongs to the composition root and must
    // outlive scene changes. Its *contents* must not. Instances are parented to this
    // transform, which puts them in the DontDestroyOnLoad scene too, so a run's enemies used to
    // walk straight out of the run and into the main menu, still ticking their AI and still
    // playing their groan clips over the menu music. Everything is dropped when a scene goes.
    public sealed class PoolService : MonoBehaviour
    {
        public static PoolService Instance { get; private set; }

        private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            Instance = null;
        }

        private void OnSceneUnloaded(Scene scene) => Clear();

        // Drops every instance, pooled or live. The prefabs a scene pooled against are that
        // scene's content; keeping their instances alive past it leaks objects, audio and AI.
        public void Clear()
        {
            _pools.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            var queue = GetQueue(prefab);
            for (int i = 0; i < count; i++)
            {
                var obj = CreateInstance(prefab);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            var queue = GetQueue(prefab);

            // Skip entries destroyed out from under the pool -- Clear() marks them for
            // destruction, and Unity only finishes the job at the end of the frame.
            GameObject obj = null;
            while (obj == null && queue.Count > 0) obj = queue.Dequeue();
            if (obj == null) obj = CreateInstance(prefab);

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);

            if (obj.TryGetComponent<IPoolable>(out var poolable)) poolable.OnSpawned();
            return obj;
        }

        public void Despawn(GameObject obj)
        {
            if (obj == null) return;

            if (obj.TryGetComponent<IPoolable>(out var poolable)) poolable.OnDespawned();
            obj.SetActive(false);

            if (obj.TryGetComponent<PooledObject>(out var tag) && tag.SourcePrefab != null)
            {
                GetQueue(tag.SourcePrefab).Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        private Queue<GameObject> GetQueue(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<GameObject>();
                _pools[prefab] = queue;
            }
            return queue;
        }

        private GameObject CreateInstance(GameObject prefab)
        {
            var obj = Instantiate(prefab, transform);
            var tag = obj.GetComponent<PooledObject>();
            if (tag == null) tag = obj.AddComponent<PooledObject>();
            tag.SourcePrefab = prefab;
            return obj;
        }
    }
}

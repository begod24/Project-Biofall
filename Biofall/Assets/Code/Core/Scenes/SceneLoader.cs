using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biofall.Core
{
    // Additive loading, so the boot scene holding the composition root never unloads. The
    // loader brings the next scene up before dropping the previous one.
    public sealed class SceneLoader : ISceneLoader
    {
        private readonly IEventBus _bus;

        public bool IsBusy { get; private set; }

        public SceneLoader(IEventBus bus) => _bus = bus;

        public async Awaitable LoadAdditiveAsync(string sceneName, bool setActive)
        {
            if (string.IsNullOrEmpty(sceneName)) return;

            Scene existing = SceneManager.GetSceneByName(sceneName);
            if (existing.IsValid() && existing.isLoaded)
            {
                if (setActive) SceneManager.SetActiveScene(existing);
                return;
            }

            IsBusy = true;
            _bus?.Publish(new SceneLoadStarted(sceneName));

            try
            {
                AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (op == null) return;

                while (!op.isDone) await Awaitable.NextFrameAsync();

                if (setActive)
                {
                    Scene loaded = SceneManager.GetSceneByName(sceneName);
                    if (loaded.IsValid() && loaded.isLoaded) SceneManager.SetActiveScene(loaded);
                }
            }
            finally
            {
                IsBusy = false;
                _bus?.Publish(new SceneLoadFinished(sceneName));
            }
        }

        public async Awaitable UnloadAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded) return;

            AsyncOperation op = SceneManager.UnloadSceneAsync(scene);
            if (op == null) return;

            while (!op.isDone) await Awaitable.NextFrameAsync();
        }

        public async Awaitable ReturnToAsync(string keepSceneName)
        {
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || scene.name == keepSceneName) continue;

                AsyncOperation op = SceneManager.UnloadSceneAsync(scene);
                if (op == null) continue;

                while (!op.isDone) await Awaitable.NextFrameAsync();
            }
        }
    }

    public readonly struct SceneLoadStarted
    {
        public readonly string SceneName;
        public SceneLoadStarted(string sceneName) { SceneName = sceneName; }
    }

    public readonly struct SceneLoadFinished
    {
        public readonly string SceneName;
        public SceneLoadFinished(string sceneName) { SceneName = sceneName; }
    }
}

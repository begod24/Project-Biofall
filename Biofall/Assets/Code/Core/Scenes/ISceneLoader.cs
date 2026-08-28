using UnityEngine;

namespace Biofall.Core
{
    public interface ISceneLoader
    {
        bool IsBusy { get; }

        Awaitable LoadAdditiveAsync(string sceneName, bool setActive);

        Awaitable UnloadAsync(string sceneName);

        // Unloads everything except the scene named, without naming what to load. Which run
        // scene was up depends on the mission, and the network layer has no business knowing
        // the mission list.
        Awaitable ReturnToAsync(string keepSceneName);
    }
}

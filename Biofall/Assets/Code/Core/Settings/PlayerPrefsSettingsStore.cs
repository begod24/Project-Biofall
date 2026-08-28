using UnityEngine;

namespace Biofall.Core
{
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        public float GetFloat(string key, float fallback) => PlayerPrefs.GetFloat(key, fallback);
        public int GetInt(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public void Save() => PlayerPrefs.Save();
    }
}

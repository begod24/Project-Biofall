using UnityEngine;

namespace Biofall.Core
{
    public sealed class PlayerPrefsProgressionStore : IProgressionStore
    {
        public int GetInt(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        public void Save() => PlayerPrefs.Save();
    }
}

namespace Biofall.Core
{
    // Where settings persist. Injected so the rules can be tested without PlayerPrefs.
    public interface ISettingsStore
    {
        float GetFloat(string key, float fallback);
        int GetInt(string key, int fallback);
        bool HasKey(string key);
        void SetFloat(string key, float value);
        void SetInt(string key, int value);
        void Save();
    }
}

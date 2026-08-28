namespace Biofall.Core
{
    public interface IProgressionStore
    {
        int GetInt(string key, int fallback);
        void SetInt(string key, int value);
        void DeleteKey(string key);
        void Save();
    }
}

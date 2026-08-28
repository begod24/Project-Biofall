namespace Biofall.Core
{
    // What CONTINUE resumes. Biofall has no run-save: this remembers which mission was last
    // entered, not what happened inside it. A real save/resume is its own feature.
    public interface ICampaignState
    {
        bool HasStartedARun { get; }
        int LastMissionIndex { get; }

        void RecordRunStarted(int missionIndex);
        void Clear();
    }
}

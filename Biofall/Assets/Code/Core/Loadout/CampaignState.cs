namespace Biofall.Core
{
    public sealed class CampaignState : ICampaignState
    {
        private const string StartedKey = "bf_campaign_started";
        private const string MissionKey = "bf_campaign_mission";

        private readonly ISettingsStore _store;

        public bool HasStartedARun { get; private set; }
        public int LastMissionIndex { get; private set; }

        public CampaignState(ISettingsStore store)
        {
            _store = store;
            HasStartedARun = _store != null && _store.GetInt(StartedKey, 0) == 1;
            LastMissionIndex = _store != null ? _store.GetInt(MissionKey, 0) : 0;
        }

        public void RecordRunStarted(int missionIndex)
        {
            HasStartedARun = true;
            LastMissionIndex = missionIndex;

            _store?.SetInt(StartedKey, 1);
            _store?.SetInt(MissionKey, missionIndex);
            _store?.Save();
        }

        public void Clear()
        {
            HasStartedARun = false;
            LastMissionIndex = 0;

            _store?.SetInt(StartedKey, 0);
            _store?.SetInt(MissionKey, 0);
            _store?.Save();
        }
    }
}

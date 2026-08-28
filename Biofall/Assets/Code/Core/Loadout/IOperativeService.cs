using System;
using Biofall.Data;

namespace Biofall.Core
{
    // Which operative the player fields. Persisted, so the pick survives a restart.
    public interface IOperativeService
    {
        OperativeData[] All { get; }
        OperativeData Selected { get; }
        string SelectedId { get; }

        event Action Changed;

        void Select(string id);
    }
}

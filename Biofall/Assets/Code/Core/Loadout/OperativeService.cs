using System;
using Biofall.Data;
using UnityEngine;

namespace Biofall.Core
{
    public sealed class OperativeService : IOperativeService
    {
        private const string SelectedKey = "bf_operative";

        private readonly OperativeCatalog _catalog;
        private readonly ISettingsStore _store;

        private string _selectedId;

        public event Action Changed;

        public OperativeData[] All => _catalog != null && _catalog.Operatives != null
            ? _catalog.Operatives
            : Array.Empty<OperativeData>();

        public string SelectedId => _selectedId;

        public OperativeData Selected
        {
            get
            {
                OperativeData found = _catalog != null ? _catalog.Find(_selectedId) : null;
                if (found != null) return found;

                OperativeData[] all = All;
                return all.Length > 0 ? all[0] : null;
            }
        }

        public OperativeService(OperativeCatalog catalog, ISettingsStore store)
        {
            _catalog = catalog;
            _store = store;

            if (_catalog == null)
                Debug.LogWarning("[Operatives] No OperativeCatalog supplied — selection is inert.");

            // ISettingsStore has no string API; the pick rides as an index into the catalog.
            int index = _store != null ? _store.GetInt(SelectedKey, 0) : 0;
            OperativeData[] all = All;
            if (index >= 0 && index < all.Length && all[index] != null) _selectedId = all[index].id;
        }

        public void Select(string id)
        {
            if (string.IsNullOrEmpty(id) || id == _selectedId) return;

            OperativeData[] all = All;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i].id != id) continue;

                _selectedId = id;
                _store?.SetInt(SelectedKey, i);
                _store?.Save();
                Changed?.Invoke();
                return;
            }
        }
    }
}

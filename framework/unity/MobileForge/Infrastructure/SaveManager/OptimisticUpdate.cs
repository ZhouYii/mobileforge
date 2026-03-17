using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Snapshot before mutation, sync to server, restore on failure.
    /// Wraps state changes in an optimistic transaction.
    /// </summary>
    public class OptimisticUpdate
    {
        private readonly Dictionary<int, Dictionary<string, Dictionary<string, object>>> _snapshots = new();
        private int _nextId;

        /// <summary>Injected: get section data. (sectionName) -> dict or null</summary>
        public System.Func<string, Dictionary<string, object>> GetSectionData { get; set; }

        /// <summary>Injected: restore section data. (sectionName, data)</summary>
        public System.Action<string, Dictionary<string, object>> SetSectionData { get; set; }

        /// <summary>Begin an optimistic transaction. Returns transaction id.</summary>
        public int Begin(List<string> sectionNames)
        {
            int txId = _nextId++;
            var sections = new Dictionary<string, Dictionary<string, object>>();
            foreach (var name in sectionNames)
            {
                var data = GetSectionData?.Invoke(name);
                if (data != null) sections[name] = new Dictionary<string, object>(data);
            }
            _snapshots[txId] = sections;
            return txId;
        }

        /// <summary>Commit the transaction (discard snapshot).</summary>
        public void Commit(int txId) => _snapshots.Remove(txId);

        /// <summary>Rollback the transaction (restore snapshot).</summary>
        public bool Rollback(int txId)
        {
            if (!_snapshots.TryGetValue(txId, out var sections)) return false;
            foreach (var kvp in sections)
                SetSectionData?.Invoke(kvp.Key, kvp.Value);
            _snapshots.Remove(txId);
            return true;
        }

        public bool IsPending(int txId) => _snapshots.ContainsKey(txId);
    }
}

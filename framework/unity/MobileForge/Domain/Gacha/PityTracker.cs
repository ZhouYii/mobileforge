using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Tracks pity counter per pool. Pure state container.
    /// </summary>
    public class PityTracker
    {
        private Dictionary<int, int> _counters = new();

        public int GetPity(int poolId)
        {
            return _counters.TryGetValue(poolId, out var count) ? count : 0;
        }

        public void Increment(int poolId)
        {
            _counters[poolId] = GetPity(poolId) + 1;
        }

        public void Reset(int poolId)
        {
            _counters[poolId] = 0;
        }

        public Dictionary<int, int> ToDict()
        {
            return new Dictionary<int, int>(_counters);
        }

        public void FromDict(Dictionary<int, int> data)
        {
            _counters = new Dictionary<int, int>(data);
        }
    }
}

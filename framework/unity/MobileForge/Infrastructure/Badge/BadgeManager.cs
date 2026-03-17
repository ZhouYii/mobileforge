using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Notification badge manager. Tracks badge sources and their counts.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class BadgeManager
    {
        private readonly Dictionary<string, Func<int>> _sources = new();
        private readonly Dictionary<string, int> _counts = new();

        /// <summary>Fired when a badge count changes. Args: sourceId, count.</summary>
        public event Action<string, int> BadgeChanged;

        /// <summary>Register a badge source. checkFunc returns badge count (0 = no badge).</summary>
        public void RegisterSource(string sourceId, Func<int> checkFunc)
        {
            _sources[sourceId] = checkFunc ?? throw new ArgumentNullException(nameof(checkFunc));
            _counts[sourceId] = 0;
            Refresh(sourceId);
        }

        /// <summary>Unregister a badge source.</summary>
        public void UnregisterSource(string sourceId)
        {
            _sources.Remove(sourceId);
            _counts.Remove(sourceId);
        }

        /// <summary>Refresh a single source's badge count.</summary>
        public void Refresh(string sourceId)
        {
            if (!_sources.TryGetValue(sourceId, out var checkFunc)) return;
            int oldCount = _counts.GetValueOrDefault(sourceId, 0);
            int newCount = Math.Max(checkFunc(), 0);
            _counts[sourceId] = newCount;
            if (oldCount != newCount)
                BadgeChanged?.Invoke(sourceId, newCount);
        }

        /// <summary>Refresh all sources.</summary>
        public void RefreshAll()
        {
            foreach (var sourceId in new List<string>(_sources.Keys))
                Refresh(sourceId);
        }

        /// <summary>Get badge count for a source (0 = no badge).</summary>
        public int GetBadgeCount(string sourceId) => _counts.GetValueOrDefault(sourceId, 0);

        /// <summary>Whether a source has any badges.</summary>
        public bool HasBadge(string sourceId) => GetBadgeCount(sourceId) > 0;

        /// <summary>Whether any of the given sources have badges.</summary>
        public bool HasAnyBadge(IEnumerable<string> sourceIds)
        {
            foreach (var id in sourceIds)
                if (HasBadge(id)) return true;
            return false;
        }

        /// <summary>Get total badge count across given sources.</summary>
        public int GetTotalCount(IEnumerable<string> sourceIds)
        {
            int total = 0;
            foreach (var id in sourceIds)
                total += GetBadgeCount(id);
            return total;
        }
    }
}

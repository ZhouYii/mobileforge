using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Multi-lock input guard. Locked when ANY reason string is active.
    /// Pure C# — timer-based unlock delegated to caller via Update().
    /// </summary>
    public class InputGuard
    {
        private readonly HashSet<string> _locks = new();
        private readonly Dictionary<string, float> _timedLocks = new();

        /// <summary>Fired when locked state changes. Parameter: is_locked.</summary>
        public event Action<bool> OnLockedChanged;

        /// <summary>Add a lock reason. Fires OnLockedChanged(true) on first lock.</summary>
        public void Lock(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                throw new ArgumentException("Reason cannot be null or empty.", nameof(reason));

            bool wasEmpty = _locks.Count == 0;
            _locks.Add(reason);
            if (wasEmpty)
                OnLockedChanged?.Invoke(true);
        }

        /// <summary>Remove a lock reason. Fires OnLockedChanged(false) when last lock removed.</summary>
        public void Unlock(string reason)
        {
            if (!_locks.Remove(reason))
                return;
            _timedLocks.Remove(reason);
            if (_locks.Count == 0)
                OnLockedChanged?.Invoke(false);
        }

        /// <summary>Whether any lock is active.</summary>
        public bool IsLocked => _locks.Count > 0;

        /// <summary>Get a list of active lock reason strings.</summary>
        public List<string> ActiveLocks() => new(_locks);

        /// <summary>
        /// Lock with automatic unlock after duration seconds.
        /// Caller must call Update(deltaTime) each frame for timed locks to expire.
        /// </summary>
        public void LockTimed(string reason, float duration)
        {
            Lock(reason);
            _timedLocks[reason] = duration;
        }

        /// <summary>
        /// Tick timed locks. Call once per frame with delta time.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (_timedLocks.Count == 0) return;

            var expired = new List<string>();
            var keys = _timedLocks.Keys.ToList();

            foreach (var key in keys)
            {
                float remaining = _timedLocks[key] - deltaTime;
                if (remaining <= 0f)
                    expired.Add(key);
                else
                    _timedLocks[key] = remaining;
            }

            foreach (var reason in expired)
                Unlock(reason);
        }
    }
}

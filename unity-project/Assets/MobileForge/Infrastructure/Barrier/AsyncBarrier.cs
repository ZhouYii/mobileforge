using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Async synchronization primitive. Tracks N pending tokens; fires AllResolved when all complete.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class AsyncBarrier
    {
        private readonly HashSet<string> _pending = new();

        /// <summary>Fired when the last pending token is resolved.</summary>
        public event Action AllResolved;

        /// <summary>Add a pending token. Does nothing if already pending.</summary>
        public void Add(string tokenId)
        {
            if (string.IsNullOrEmpty(tokenId))
                throw new ArgumentException("Token ID cannot be null or empty.", nameof(tokenId));
            _pending.Add(tokenId);
        }

        /// <summary>Resolve a pending token. Fires AllResolved when last token resolved.</summary>
        public void Resolve(string tokenId)
        {
            if (!_pending.Remove(tokenId))
                return;
            if (_pending.Count == 0)
                AllResolved?.Invoke();
        }

        /// <summary>Whether all tokens have been resolved (or none were added).</summary>
        public bool IsResolved => _pending.Count == 0;

        /// <summary>Number of tokens still pending.</summary>
        public int PendingCount => _pending.Count;

        /// <summary>Get a copy of pending token IDs.</summary>
        public List<string> GetPending() => new(_pending);

        /// <summary>Reset: clear all pending tokens without firing AllResolved.</summary>
        public void Reset() => _pending.Clear();
    }
}

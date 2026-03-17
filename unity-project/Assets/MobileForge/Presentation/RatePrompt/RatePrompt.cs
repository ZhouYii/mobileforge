using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Progressive rate-app prompt with Fibonacci spacing and dismissal tracking.
    /// Pure C# — delegates UI display to engine layer.
    /// </summary>
    public class RatePrompt
    {
        private int _sessionCount;
        private int _dismissCount;
        private bool _rated;
        private bool _neverAsk;

        private static readonly int[] PromptSessions = { 3, 5, 8, 13, 21, 34, 55 };

        /// <summary>Call at app launch.</summary>
        public void OnSessionStart() => _sessionCount++;

        /// <summary>Whether the rate prompt should be shown this session.</summary>
        public bool ShouldShow()
        {
            if (_rated || _neverAsk) return false;
            if (_dismissCount < PromptSessions.Length)
                return _sessionCount >= PromptSessions[_dismissCount];
            return false;
        }

        public void OnRated() => _rated = true;
        public void OnDismissed() => _dismissCount++;
        public void OnNeverAsk() => _neverAsk = true;

        /// <summary>Load state from saved data.</summary>
        public void LoadState(int sessionCount, int dismissCount, bool rated, bool neverAsk)
        {
            _sessionCount = sessionCount;
            _dismissCount = dismissCount;
            _rated = rated;
            _neverAsk = neverAsk;
        }

        /// <summary>Get state for saving.</summary>
        public Dictionary<string, object> SaveState()
        {
            return new Dictionary<string, object>
            {
                ["session_count"] = _sessionCount,
                ["dismiss_count"] = _dismissCount,
                ["rated"] = _rated,
                ["never_ask"] = _neverAsk,
            };
        }
    }
}

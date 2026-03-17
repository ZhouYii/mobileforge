using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Show hints when player is idle, hide on any input.
    /// Pure C# — delegates display to engine layer.
    /// </summary>
    public class InactivityHints
    {
        private readonly List<string> _hints = new();
        private float _idleTimer;
        private float _hintTimer;
        private int _currentIndex;
        private bool _isShowing;

        public float IdleThreshold { get; set; } = 10f;
        public float HintInterval { get; set; } = 8f;
        public bool IsShowing => _isShowing;

        public event Action<string> HintShown;
        public event Action HintHidden;

        public void SetHints(List<string> hints)
        {
            _hints.Clear();
            _hints.AddRange(hints);
        }

        /// <summary>Call when any input is detected.</summary>
        public void OnInput()
        {
            if (_isShowing) HideHint();
            _idleTimer = 0f;
        }

        /// <summary>Call each frame with delta.</summary>
        public void Update(float delta)
        {
            _idleTimer += delta;
            if (!_isShowing && _idleTimer >= IdleThreshold && _hints.Count > 0)
            {
                ShowHint();
            }
            else if (_isShowing)
            {
                _hintTimer += delta;
                if (_hintTimer >= HintInterval)
                {
                    _hintTimer = 0f;
                    _currentIndex = (_currentIndex + 1) % _hints.Count;
                    HintShown?.Invoke(_hints[_currentIndex]);
                }
            }
        }

        private void ShowHint()
        {
            _isShowing = true;
            _hintTimer = 0f;
            _currentIndex = 0;
            HintShown?.Invoke(_hints[0]);
        }

        private void HideHint()
        {
            _isShowing = false;
            _idleTimer = 0f;
            HintHidden?.Invoke();
        }
    }
}

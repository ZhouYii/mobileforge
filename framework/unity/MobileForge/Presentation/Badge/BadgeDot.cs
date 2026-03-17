using System;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Red dot notification indicator model. Binds to a BadgeManager source.
    /// Pure C# — UI rendering delegated to engine layer.
    /// </summary>
    public class BadgeDot
    {
        private Infrastructure.BadgeManager _badgeManager;
        private string _sourceId;
        private bool _showCount;

        /// <summary>Whether the dot should be visible.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>Display text (count or empty). Only relevant when ShowCount is true.</summary>
        public string DisplayText { get; private set; } = "";

        /// <summary>Current badge count.</summary>
        public int Count { get; private set; }

        /// <summary>Fired when display state changes.</summary>
        public event Action OnDisplayChanged;

        /// <summary>Bind to a badge manager source.</summary>
        public void Bind(Infrastructure.BadgeManager badgeManager, string sourceId)
        {
            Unbind();
            _badgeManager = badgeManager;
            _sourceId = sourceId;
            if (_badgeManager != null)
            {
                _badgeManager.BadgeChanged += OnBadgeChanged;
                UpdateDisplay(_badgeManager.GetBadgeCount(sourceId));
            }
        }

        /// <summary>Unbind from current badge manager.</summary>
        public void Unbind()
        {
            if (_badgeManager != null)
            {
                _badgeManager.BadgeChanged -= OnBadgeChanged;
                _badgeManager = null;
            }
        }

        /// <summary>Set whether to show count or just dot.</summary>
        public void SetShowCount(bool show)
        {
            _showCount = show;
            if (_badgeManager != null)
                UpdateDisplay(_badgeManager.GetBadgeCount(_sourceId));
        }

        private void OnBadgeChanged(string sourceId, int count)
        {
            if (sourceId == _sourceId)
                UpdateDisplay(count);
        }

        private void UpdateDisplay(int count)
        {
            Count = count;
            IsVisible = count > 0;
            DisplayText = _showCount && count > 0
                ? (count < 100 ? count.ToString() : "99+")
                : "";
            OnDisplayChanged?.Invoke();
        }
    }
}

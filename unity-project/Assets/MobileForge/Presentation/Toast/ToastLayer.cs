using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Toast notification manager. Maintains a list of active toasts
    /// and removes them when their duration expires.
    /// Pure C# — rendering is delegated to engine-specific code.
    /// </summary>
    public class ToastLayer
    {
        private readonly List<ToastEntry> _active = new();

        /// <summary>
        /// Fired when a new toast is shown. Listeners can use this to create visual elements.
        /// Parameters: (text, duration, toastId).
        /// </summary>
        public Action<string, float, int> OnToastShown;

        /// <summary>
        /// Fired when a toast expires and is removed.
        /// Parameter: toastId.
        /// </summary>
        public Action<int> OnToastRemoved;

        public int ActiveCount => _active.Count;

        private int _nextId;

        /// <summary>
        /// Show a toast with the given text and duration in seconds.
        /// Returns a toast ID that can be used to identify the toast.
        /// </summary>
        public int ShowToast(string text, float duration = 2.0f)
        {
            if (string.IsNullOrEmpty(text))
                return -1;
            if (duration <= 0f)
                duration = 2.0f;

            var id = _nextId++;
            _active.Add(new ToastEntry
            {
                Id = id,
                Text = text,
                Remaining = duration,
                Duration = duration,
            });

            OnToastShown?.Invoke(text, duration, id);
            return id;
        }

        /// <summary>
        /// Tick the toast layer. Call this each frame with the elapsed time.
        /// Expired toasts are removed and OnToastRemoved is fired for each.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (_active.Count == 0) return;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _active[i].Remaining -= deltaTime;
                if (_active[i].Remaining <= 0f)
                {
                    var id = _active[i].Id;
                    _active.RemoveAt(i);
                    OnToastRemoved?.Invoke(id);
                }
            }
        }

        /// <summary>
        /// Get the text of all currently active toasts.
        /// </summary>
        public List<string> GetActiveTexts()
        {
            var texts = new List<string>(_active.Count);
            foreach (var entry in _active)
                texts.Add(entry.Text);
            return texts;
        }

        /// <summary>
        /// Immediately remove all active toasts.
        /// </summary>
        public void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                OnToastRemoved?.Invoke(_active[i].Id);

            _active.Clear();
        }

        private class ToastEntry
        {
            public int Id;
            public string Text;
            public float Remaining;
            public float Duration;
        }
    }
}

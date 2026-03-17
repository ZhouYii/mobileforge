using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Priority-queue popup manager. Popups are sorted by priority (higher = shown first).
    /// Only one popup is visible at a time; the next in queue is shown when the current is dismissed.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class PopupStack
    {
        private readonly List<PopupEntry> _queue = new();
        private PopupEntry _current;

        private BackHandler _backHandler;

        /// <summary>
        /// Optional back handler. When set, auto-registers a handler that dismisses the current popup.
        /// Register PopupStack's back handler AFTER UIRouter's so popup dismissal takes priority.
        /// </summary>
        public BackHandler BackHandler
        {
            get => _backHandler;
            set
            {
                if (_backHandler != null)
                    _backHandler.Remove("popup_stack");
                _backHandler = value;
                if (_backHandler != null)
                    _backHandler.Push("popup_stack", () =>
                    {
                        if (_current == null) return false;
                        Dismiss();
                        return true;
                    });
            }
        }

        public int PopupCount => _queue.Count + (_current != null ? 1 : 0);
        public bool IsShowing => _current != null;
        public string CurrentPopupId => _current?.Popup?.PopupId;

        /// <summary>
        /// Show a popup. If a popup is already visible, the new one is queued by priority.
        /// </summary>
        /// <param name="popupId">Unique identifier for the popup.</param>
        /// <param name="factory">Factory that creates the IPopup instance.</param>
        /// <param name="parameters">Parameters passed to OnShow.</param>
        /// <param name="priority">Higher priority popups are shown first. Default is 0.</param>
        /// <param name="onDismiss">Optional callback invoked with the dismissal result.</param>
        public void Show(string popupId, Func<IPopup> factory, Dictionary<string, object> parameters = null,
            int priority = 0, Action<object> onDismiss = null)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            parameters ??= new Dictionary<string, object>();

            var entry = new PopupEntry
            {
                PopupId = popupId,
                Factory = factory,
                Parameters = parameters,
                Priority = priority,
                OnDismiss = onDismiss,
            };

            if (_current == null)
            {
                ShowEntry(entry);
            }
            else
            {
                InsertSorted(entry);
            }
        }

        /// <summary>
        /// Dismiss the currently visible popup with an optional result.
        /// </summary>
        public void Dismiss(object result = null)
        {
            if (_current == null) return;

            var dismissed = _current;
            _current = null;

            dismissed.Popup?.Dismiss(result);
            dismissed.OnDismiss?.Invoke(result);

            ShowNext();
        }

        /// <summary>
        /// Dismiss all popups (current + queued). Current popup receives null result.
        /// Queued popups are discarded without callbacks.
        /// </summary>
        public void DismissAll()
        {
            if (_current != null)
            {
                _current.Popup?.Dismiss(null);
                _current.OnDismiss?.Invoke(null);
                _current = null;
            }

            _queue.Clear();
        }

        private void ShowEntry(PopupEntry entry)
        {
            _current = entry;

            var popup = entry.Factory.Invoke();
            popup.PopupId = entry.PopupId;
            entry.Popup = popup;

            // Listen for self-dismiss (popup calls Dismiss on itself)
            popup.Dismissed += OnPopupSelfDismissed;
            popup.OnShow(entry.Parameters);
        }

        private void OnPopupSelfDismissed(object result)
        {
            if (_current?.Popup != null)
                _current.Popup.Dismissed -= OnPopupSelfDismissed;

            var dismissed = _current;
            _current = null;

            dismissed?.OnDismiss?.Invoke(result);

            ShowNext();
        }

        private void ShowNext()
        {
            if (_queue.Count == 0) return;

            var next = _queue[0];
            _queue.RemoveAt(0);
            ShowEntry(next);
        }

        /// <summary>
        /// Show a popup and asynchronously await its dismissal result.
        /// </summary>
        public Task<object> ShowAwait(string popupId, Func<IPopup> factory,
            Dictionary<string, object> parameters = null, int priority = 0)
        {
            var tcs = new TaskCompletionSource<object>();
            Show(popupId, factory, parameters, priority, result => tcs.TrySetResult(result));
            return tcs.Task;
        }

        private void InsertSorted(PopupEntry entry)
        {
            // Insert so that higher priority is at the front
            int idx = 0;
            while (idx < _queue.Count && _queue[idx].Priority >= entry.Priority)
                idx++;

            _queue.Insert(idx, entry);
        }

        private class PopupEntry
        {
            public string PopupId;
            public Func<IPopup> Factory;
            public Dictionary<string, object> Parameters;
            public int Priority;
            public Action<object> OnDismiss;
            public IPopup Popup;
        }
    }
}

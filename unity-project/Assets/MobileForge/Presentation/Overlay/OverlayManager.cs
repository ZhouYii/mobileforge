using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Named overlay manager. Overlays are persistent UI layers identified by string IDs.
    /// Multiple overlays can be visible simultaneously.
    /// Pure C# — rendering is delegated to engine-specific code via callbacks.
    /// </summary>
    public class OverlayManager
    {
        private readonly Dictionary<string, object> _overlays = new();

        /// <summary>
        /// Fired when an overlay is shown. Parameters: (overlayId, overlayObject).
        /// </summary>
        public Action<string, object> OnOverlayShown;

        /// <summary>
        /// Fired when an overlay is hidden. Parameter: overlayId.
        /// </summary>
        public Action<string> OnOverlayHidden;

        /// <summary>
        /// Show an overlay. If the overlay is already showing, this is a no-op.
        /// </summary>
        /// <param name="id">Unique identifier for the overlay.</param>
        /// <param name="factory">Factory function that creates the overlay object.</param>
        public void ShowOverlay(string id, Func<object> factory)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Overlay ID cannot be null or empty.", nameof(id));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (_overlays.ContainsKey(id))
                return;

            var overlay = factory.Invoke();
            _overlays[id] = overlay;

            OnOverlayShown?.Invoke(id, overlay);
        }

        /// <summary>
        /// Hide and remove an overlay by ID.
        /// </summary>
        public void HideOverlay(string id)
        {
            if (!_overlays.Remove(id))
                return;

            OnOverlayHidden?.Invoke(id);
        }

        /// <summary>
        /// Hide all overlays.
        /// </summary>
        public void HideAll()
        {
            var ids = _overlays.Keys.ToList();
            _overlays.Clear();

            foreach (var id in ids)
                OnOverlayHidden?.Invoke(id);
        }

        /// <summary>
        /// Check if an overlay is currently showing.
        /// </summary>
        public bool IsShowing(string id)
        {
            return _overlays.ContainsKey(id);
        }

        /// <summary>
        /// Get the overlay object by ID, or null if not showing.
        /// </summary>
        public object GetOverlay(string id)
        {
            return _overlays.TryGetValue(id, out var overlay) ? overlay : null;
        }

        /// <summary>
        /// Get the number of active overlays.
        /// </summary>
        public int ActiveCount => _overlays.Count;
    }
}

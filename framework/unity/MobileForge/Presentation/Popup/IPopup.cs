using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Lifecycle interface for managed popups.
    /// Popups are shown above screens and can return a result on dismissal.
    /// </summary>
    public interface IPopup
    {
        string PopupId { get; set; }
        void OnShow(Dictionary<string, object> parameters);
        void Dismiss(object result = null);
        event Action<object> Dismissed;
    }
}

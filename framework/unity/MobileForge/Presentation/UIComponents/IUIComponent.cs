using UnityEngine;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Non-generic lifecycle interface for UI components (pages).
    /// </summary>
    public interface IUIComponent
    {
        void Mount(Transform parent);
        void Unmount();
        void Refresh();
        void BindUntyped(object screen);
    }

    /// <summary>
    /// Typed binding — each page knows its screen type.
    /// </summary>
    public interface IUIComponent<TScreen> : IUIComponent where TScreen : class
    {
        void Bind(TScreen screen);
        TScreen Screen { get; }
    }
}

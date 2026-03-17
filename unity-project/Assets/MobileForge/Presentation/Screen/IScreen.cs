using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Lifecycle interface for managed screens.
    /// Implemented by plain C# classes; MonoBehaviour wrappers delegate to these.
    /// </summary>
    public interface IScreen
    {
        void OnEnter(Dictionary<string, object> parameters);
        void OnPause();
        void OnResume();
        void OnExit();
    }
}

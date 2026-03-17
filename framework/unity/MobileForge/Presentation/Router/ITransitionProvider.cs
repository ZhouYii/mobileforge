using System;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Interface for screen transition animations (e.g., fade overlay).
    /// Implement this to provide custom transitions between screens.
    /// </summary>
    public interface ITransitionProvider
    {
        /// <summary>
        /// Execute a transition. Call onMidpoint at peak (e.g., when fully faded to black)
        /// so the router can swap screens while obscured. Call onComplete when transition finishes.
        /// </summary>
        /// <param name="onMidpoint">Callback to invoke at the transition midpoint (swap screens here).</param>
        /// <param name="onComplete">Callback to invoke when the full transition is finished.</param>
        /// <param name="duration">Total transition duration in seconds.</param>
        void Transition(Action onMidpoint, Action onComplete, float duration = 0.5f);
    }
}

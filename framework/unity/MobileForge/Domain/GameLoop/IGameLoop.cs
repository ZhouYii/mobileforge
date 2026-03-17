using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Interface for genre-specific game loops.
    /// Turn-based games implement ProcessInput(); real-time games implement Tick().
    /// </summary>
    public interface IGameLoop
    {
        /// <summary>Start the game loop with a configuration.</summary>
        void Start(Dictionary<string, object> config);

        /// <summary>Process player input (turn-based games).</summary>
        PhaseResult ProcessInput(Dictionary<string, object> input);

        /// <summary>Tick the loop forward by delta seconds (real-time games).</summary>
        PhaseResult Tick(float delta);

        /// <summary>Get a snapshot of the current game state.</summary>
        GameLoopState GetState();

        /// <summary>Whether the game loop is currently active.</summary>
        bool IsActive { get; }
    }
}

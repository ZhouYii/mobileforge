using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// The result of processing a phase (input or tick).
    /// </summary>
    public class PhaseResult
    {
        public string PhaseName { get; set; } = "";
        public bool Completed { get; set; }
        public string NextPhase { get; set; } = "";
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// Snapshot of a game loop's current state.
    /// </summary>
    public class GameLoopState
    {
        public string Phase { get; set; } = "";
        public int TurnNumber { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, object> Custom { get; set; } = new();
    }
}

using System.Collections.Generic;

namespace TowerOfSaviors
{
    /// <summary>
    /// Tracks combo count for display during cascade animation.
    /// Pure data model — no MonoBehaviour, no rendering.
    /// </summary>
    public class ComboTracker
    {
        /// <summary>
        /// Current combo count this turn.
        /// </summary>
        public int ComboCount { get; private set; }

        /// <summary>
        /// Whether the combo display should be visible.
        /// </summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// Time remaining before the combo display auto-hides (seconds).
        /// </summary>
        public float DisplayTimer { get; private set; }

        /// <summary>
        /// History of combos for the current turn (element, gem count per combo).
        /// </summary>
        public List<ComboEntry> ComboHistory { get; } = new List<ComboEntry>();

        public class ComboEntry
        {
            public int ElementId { get; set; }
            public int GemCount { get; set; }
            public int ComboNumber { get; set; }
        }

        /// <summary>
        /// Reset for a new turn.
        /// </summary>
        public void Reset()
        {
            ComboCount = 0;
            IsVisible = false;
            DisplayTimer = 0f;
            ComboHistory.Clear();
        }

        /// <summary>
        /// Record a new combo hit from a cascade step.
        /// </summary>
        public void RecordCombo(int elementId, int gemCount)
        {
            ComboCount++;
            IsVisible = true;
            DisplayTimer = 2.0f;
            ComboHistory.Add(new ComboEntry
            {
                ElementId = elementId,
                GemCount = gemCount,
                ComboNumber = ComboCount
            });
        }

        /// <summary>
        /// Tick the display timer. Call with delta time each frame.
        /// Returns true if visibility changed (became hidden).
        /// </summary>
        public bool Tick(float deltaTime)
        {
            if (!IsVisible)
                return false;

            DisplayTimer -= deltaTime;
            if (DisplayTimer <= 0f)
            {
                IsVisible = false;
                return true;
            }
            return false;
        }
    }
}

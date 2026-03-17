using System;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Definition of a single phase in a PhaseSequencer pipeline.
    /// Advance modes (first match wins):
    ///   - AutoAdvance = true: advances immediately after OnEnter
    ///   - Duration > 0: advances after Duration seconds
    ///   - Barrier != null: advances when Barrier fires AllResolved
    ///   - None: manual Advance() required
    /// </summary>
    public class PhaseDef
    {
        public string Id { get; set; }
        public Action OnEnter { get; set; }
        public Action<float> OnUpdate { get; set; }
        public Action OnExit { get; set; }
        public bool AutoAdvance { get; set; }
        public AsyncBarrier Barrier { get; set; }
        public float Duration { get; set; }

        public PhaseDef(string id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }
    }
}

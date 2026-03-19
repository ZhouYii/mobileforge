using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Makes one element act as another for damage calculation for N turns.
    /// Matches ToS SO_Element1AlsoActAsElement2 — a core mechanic where e.g.
    /// Fire gems also count as Water for damage.
    /// Params: "source_element" (int), "target_element" (int), "duration_turns" (int)
    /// </summary>
    public class ElementShiftOutcome : SkillOutcomeBase
    {
        private int _sourceElement;
        private int _targetElement;

        public ElementShiftOutcome(Dictionary<string, object> parameters = null) : base(parameters) { }

        public override void Activate(SkillContext context, SkillResult result)
        {
            _sourceElement = GetParam("source_element", 0);
            _targetElement = GetParam("target_element", 0);
            TurnsLeft = GetParam("duration_turns", 3);

            // Store the shift mapping in context for combat resolver
            var key = $"element_shift_{_sourceElement}";
            context.Extra[key] = _targetElement;

            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "element_shift" },
                { "source_element", _sourceElement },
                { "target_element", _targetElement },
                { "turns", TurnsLeft },
            });
        }

        public override void OnTurnStart(SkillContext context)
        {
            var key = $"element_shift_{_sourceElement}";
            context.Extra[key] = _targetElement;
        }

        public override void Deactivate(SkillContext context)
        {
            var key = $"element_shift_{_sourceElement}";
            context.Extra.Remove(key);
        }
    }
}

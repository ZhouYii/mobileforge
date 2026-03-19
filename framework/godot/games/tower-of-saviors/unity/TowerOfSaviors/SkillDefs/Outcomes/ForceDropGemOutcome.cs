using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Forces newly dropped gems to be a specific element for N turns.
    /// Matches ToS SO_MustDropGem / SO_ChangeDropGem — controls what elements
    /// appear when gems are cleared and new ones fall in.
    /// Params: "element" (int), "duration_turns" (int),
    ///         "chance" (float 0.0-1.0, default 1.0 — probability each new gem is forced)
    /// </summary>
    public class ForceDropGemOutcome : SkillOutcomeBase
    {
        private int _element;
        private float _chance;

        public ForceDropGemOutcome(Dictionary<string, object> parameters = null) : base(parameters) { }

        public override void Activate(SkillContext context, SkillResult result)
        {
            _element = GetParam("element", 1);
            _chance = GetParam("chance", 1.0f);
            TurnsLeft = GetParam("duration_turns", 1);

            // Store in context for cascade resolver to read during gem spawning
            context.Extra["force_drop_element"] = _element;
            context.Extra["force_drop_chance"] = _chance;

            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "force_drop_gem" },
                { "element", _element },
                { "chance", _chance },
                { "turns", TurnsLeft },
            });
        }

        public override void OnTurnStart(SkillContext context)
        {
            context.Extra["force_drop_element"] = _element;
            context.Extra["force_drop_chance"] = _chance;
        }

        public override void Deactivate(SkillContext context)
        {
            context.Extra.Remove("force_drop_element");
            context.Extra.Remove("force_drop_chance");
        }
    }
}

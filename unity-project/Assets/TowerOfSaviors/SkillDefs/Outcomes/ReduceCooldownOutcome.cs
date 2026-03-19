using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Reduces cooldowns of all team skills by N each turn for a duration.
    /// Matches ToS SO_ReduceCDEachRound / ChargePlayerSkill.
    /// Params: "cd_reduction" (int, default 1), "duration_turns" (int, default 1),
    ///         "immediate" (bool, default true — also apply once on activation)
    /// </summary>
    public class ReduceCooldownOutcome : SkillOutcomeBase
    {
        private int _cdReduction;
        private bool _immediate;

        public ReduceCooldownOutcome(Dictionary<string, object> parameters = null) : base(parameters) { }

        public override void Activate(SkillContext context, SkillResult result)
        {
            _cdReduction = GetParam("cd_reduction", 1);
            _immediate = GetParam("immediate", true);
            TurnsLeft = GetParam("duration_turns", 1);

            if (_immediate)
                ApplyCdReduction(context);

            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "reduce_cooldown" },
                { "cd_reduction", _cdReduction },
                { "turns", TurnsLeft },
            });
        }

        public override void OnTurnStart(SkillContext context)
        {
            // Don't double-apply on the activation turn if immediate was true
            if (!_immediate || TurnsLeft < GetParam("duration_turns", 1))
                ApplyCdReduction(context);
        }

        private void ApplyCdReduction(SkillContext context)
        {
            // Signal to the dungeon runner / skill manager to reduce CDs
            if (!context.Extra.ContainsKey("cd_reduction_pending"))
                context.Extra["cd_reduction_pending"] = 0;
            context.Extra["cd_reduction_pending"] =
                (int)context.Extra["cd_reduction_pending"] + _cdReduction;
        }
    }
}

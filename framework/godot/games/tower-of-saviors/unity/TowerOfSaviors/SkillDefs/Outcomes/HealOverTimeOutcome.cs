using System;
using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Heals the team each turn based on recovery_multiplier * team REC.
    /// Params: "recovery_multiplier" (float), "duration_turns" (int)
    /// </summary>
    public class HealOverTimeOutcome : SkillOutcomeBase
    {
        public HealOverTimeOutcome(Dictionary<string, object> parameters = null)
            : base(parameters) { }

        public override void Activate(SkillContext context, SkillResult result)
        {
            TurnsLeft = GetParam<int>("duration_turns", 1);
            ApplyHeal(context, result);
        }

        public override void OnTurnStart(SkillContext context)
        {
            float mult = GetParam<float>("recovery_multiplier", 1.0f);
            float teamRec = GetTeamRec(context);
            int healing = (int)(teamRec * mult);

            if (healing > 0 && context.Extra != null)
            {
                int pending = 0;
                if (context.Extra.TryGetValue("pending_healing", out var val))
                    pending = Convert.ToInt32(val);
                context.Extra["pending_healing"] = pending + healing;
            }
        }

        private void ApplyHeal(SkillContext context, SkillResult result)
        {
            float mult = GetParam<float>("recovery_multiplier", 1.0f);
            float teamRec = GetTeamRec(context);
            result.Healing += (int)(teamRec * mult);
        }

        private static float GetTeamRec(SkillContext context)
        {
            float rec = 0f;
            if (context.TeamStats != null)
            {
                foreach (var stats in context.TeamStats)
                {
                    if (stats is MonsterStats ms)
                        rec += ms.Rec;
                }
            }
            return rec > 0 ? rec : 100f; // fallback
        }
    }
}

using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// True when the current turn number meets a criterion.
    /// Supports: exact turn, every N turns, after turn N.
    /// Params: "turn" (int, exact match), "every" (int, modulo), "after" (int, minimum)
    /// </summary>
    public class TurnNumberCondition : SkillConditionBase
    {
        public TurnNumberCondition(Dictionary<string, object> parameters = null) : base(parameters) { }

        public override bool IsValid(SkillContext context)
        {
            int exactTurn = GetParam("turn", -1);
            if (exactTurn >= 0)
                return context.TurnNumber == exactTurn;

            int every = GetParam("every", -1);
            if (every > 0)
                return context.TurnNumber % every == 0;

            int after = GetParam("after", -1);
            if (after >= 0)
                return context.TurnNumber >= after;

            return true;
        }
    }
}

using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// True when the number of alive enemies meets a criterion.
    /// Params: "min" (int, default 1), "max" (int, default int.MaxValue)
    /// </summary>
    public class EnemiesAliveCondition : SkillConditionBase
    {
        public EnemiesAliveCondition(Dictionary<string, object> parameters = null) : base(parameters) { }

        public override bool IsValid(SkillContext context)
        {
            int min = GetParam("min", 1);
            int max = GetParam("max", int.MaxValue);

            int alive = 0;
            foreach (var enemyObj in context.Enemies)
            {
                if (enemyObj is EnemyState enemy && enemy.IsAlive)
                    alive++;
            }

            return alive >= min && alive <= max;
        }
    }
}

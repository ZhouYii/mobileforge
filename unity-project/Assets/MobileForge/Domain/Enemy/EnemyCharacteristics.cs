using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Static helper methods for enemy damage characteristics.
    /// These model special enemy abilities that modify incoming damage.
    /// </summary>
    public static class EnemyCharacteristics
    {
        /// <summary>
        /// Returns true if this enemy can only be damaged by combos (not direct attacks).
        /// Enemy must have the "only_combo_can_damage" status.
        /// </summary>
        public static bool OnlyComboCanDamage(EnemyState enemy)
        {
            return enemy.HasStatus("only_combo_can_damage");
        }

        /// <summary>
        /// Combo shield: enemy takes no damage unless combo count >= threshold.
        /// Returns 0 if shielded, otherwise returns the original damage.
        /// </summary>
        public static int ComboShield(EnemyState enemy, int damage, int comboCount)
        {
            if (!enemy.HasStatus("combo_shield"))
                return damage;

            foreach (var se in enemy.StatusEffects)
            {
                if (se.TryGetValue("type", out var typeObj) && Convert.ToString(typeObj) == "combo_shield")
                {
                    int threshold = se.TryGetValue("threshold", out var threshObj)
                        ? Convert.ToInt32(threshObj) : 0;
                    if (comboCount < threshold)
                        return 0;
                    break;
                }
            }
            return damage;
        }

        /// <summary>
        /// Element shield: enemy takes no damage from a specific element.
        /// Returns 0 if the attacker element matches the shielded element, otherwise original damage.
        /// </summary>
        public static int ElementShield(EnemyState enemy, int damage, int attackerElement)
        {
            if (!enemy.HasStatus("element_shield"))
                return damage;

            foreach (var se in enemy.StatusEffects)
            {
                if (se.TryGetValue("type", out var typeObj) && Convert.ToString(typeObj) == "element_shield")
                {
                    int shieldedElement = se.TryGetValue("element", out var elemObj)
                        ? Convert.ToInt32(elemObj) : 0;
                    if (attackerElement == shieldedElement)
                        return 0;
                    break;
                }
            }
            return damage;
        }

        /// <summary>
        /// Damage absorb: enemy heals instead of taking damage for a specific element.
        /// Returns negative damage (healing) if absorbed, otherwise original damage.
        /// </summary>
        public static int DamageAbsorb(EnemyState enemy, int damage, int attackerElement)
        {
            if (!enemy.HasStatus("damage_absorb"))
                return damage;

            foreach (var se in enemy.StatusEffects)
            {
                if (se.TryGetValue("type", out var typeObj) && Convert.ToString(typeObj) == "damage_absorb")
                {
                    int absorbElement = se.TryGetValue("element", out var elemObj)
                        ? Convert.ToInt32(elemObj) : 0;
                    if (attackerElement == absorbElement)
                        return -damage;
                    break;
                }
            }
            return damage;
        }

        /// <summary>
        /// Apply a flat damage reduction (e.g., enemy defense shield).
        /// Damage is reduced by the percentage value (0.0 to 1.0).
        /// </summary>
        public static int ApplyDamageReduction(int damage, float reductionPercent)
        {
            return (int)(damage * (1.0f - Math.Min(Math.Max(reductionPercent, 0f), 1f)));
        }

        /// <summary>
        /// Apply a damage cap: damage cannot exceed the specified maximum.
        /// </summary>
        public static int ApplyDamageCap(int damage, int cap)
        {
            return Math.Min(damage, cap);
        }
    }
}

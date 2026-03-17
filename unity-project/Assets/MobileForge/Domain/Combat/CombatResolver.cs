using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// 5-hook damage pipeline matching ToS's SkillInstance event system.
    /// Hooks: PRE_ELEMENT -> POST_ELEMENT -> MAIN -> POST_DEFENSE -> CAN_ZERO
    /// Skills register callable hooks with priority; resolver fires them in order.
    /// </summary>
    public class CombatResolver
    {
        private readonly ElementChart _elementChart;
        private readonly Dictionary<DamageHook, List<HookEntry>> _hooks = new();

        public CombatResolver(ElementChart elementChart = null)
        {
            _elementChart = elementChart ?? new ElementChart();
            foreach (DamageHook hookType in Enum.GetValues(typeof(DamageHook)))
            {
                _hooks[hookType] = new List<HookEntry>();
            }
        }

        /// <summary>
        /// Register a hook callable. Lower priority fires first.
        /// </summary>
        public void RegisterHook(DamageHook hookType, Action<DamageContext> callback, int priority = 100, string hookName = "")
        {
            _hooks[hookType].Add(new HookEntry(callback, priority, hookName));
            _hooks[hookType].Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        /// <summary>
        /// Unregister a hook by callback reference.
        /// </summary>
        public void UnregisterHook(DamageHook hookType, Action<DamageContext> callback)
        {
            var hooks = _hooks[hookType];
            for (int i = hooks.Count - 1; i >= 0; i--)
            {
                if (hooks[i].Callback == callback)
                {
                    hooks.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Clear all hooks of a specific type.
        /// </summary>
        public void ClearHooks(DamageHook hookType)
        {
            _hooks[hookType].Clear();
        }

        /// <summary>
        /// Clear ALL hooks.
        /// </summary>
        public void ClearAllHooks()
        {
            foreach (var hookType in _hooks.Keys)
                _hooks[hookType].Clear();
        }

        /// <summary>
        /// Resolve player attack damage through the 5-hook pipeline.
        /// </summary>
        public DamageResult ResolvePlayerAttack(DamageContext ctx)
        {
            var hooksApplied = new List<string>();

            // 1. Calculate base gem damage
            ctx.BaseDamage = ComboCalculator.GemDamage(ctx.AttackerAtk, ctx.GemsMatched);
            ctx.Damage = ctx.BaseDamage;

            // 2. Apply combo multiplier
            float comboMult = ComboCalculator.Calculate(ctx.ComboCount);
            ctx.Damage *= comboMult;

            // 3. PRE_ELEMENT hooks
            FireHooks(DamageHook.PreElement, ctx, hooksApplied);

            // 4. Element multiplier
            float elemMult = _elementChart.GetMultiplier(ctx.AttackerElement, ctx.DefenderElement);
            ctx.Damage *= elemMult;

            // 5. POST_ELEMENT hooks
            FireHooks(DamageHook.PostElement, ctx, hooksApplied);

            // 6. MAIN hooks (leader skills, team skills typically here)
            FireHooks(DamageHook.Main, ctx, hooksApplied);

            // 7. Subtract defense
            ctx.Damage = Math.Max(ctx.Damage - ctx.DefenderDefense, 1.0f);

            // 8. POST_DEFENSE hooks
            FireHooks(DamageHook.PostDefense, ctx, hooksApplied);

            // 9. CAN_ZERO hooks (some skills can force damage to 0)
            FireHooks(DamageHook.CanZero, ctx, hooksApplied);

            return new DamageResult(
                (int)ctx.Damage,
                elemMult,
                comboMult,
                hooksApplied
            );
        }

        /// <summary>
        /// Resolve enemy attack (simpler — no combos, no element matching).
        /// </summary>
        public int ResolveEnemyAttack(float enemyAtk, float teamDefense = 0f)
        {
            return (int)Math.Max(enemyAtk - teamDefense, 1.0f);
        }

        /// <summary>
        /// Get the element multiplier for UI display.
        /// </summary>
        public float CalculateElementMultiplier(int attacker, int defender)
        {
            return _elementChart.GetMultiplier(attacker, defender);
        }

        private void FireHooks(DamageHook hookType, DamageContext ctx, List<string> hooksApplied)
        {
            foreach (var entry in _hooks[hookType])
            {
                entry.Callback(ctx);
                if (!string.IsNullOrEmpty(entry.Name))
                    hooksApplied.Add(entry.Name);
            }
        }

        private class HookEntry
        {
            public Action<DamageContext> Callback;
            public int Priority;
            public string Name;

            public HookEntry(Action<DamageContext> callback, int priority, string name)
            {
                Callback = callback;
                Priority = priority;
                Name = name;
            }
        }
    }
}

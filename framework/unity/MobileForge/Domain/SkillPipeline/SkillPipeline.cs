using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Orchestrates skill activation/deactivation.
    /// Manages condition/outcome registries and active persistent outcomes.
    /// </summary>
    public class SkillPipeline
    {
        public ConditionRegistry ConditionRegistry { get; }
        public OutcomeRegistry OutcomeRegistry { get; }
        public EffectRegistry EffectRegistry { get; }

        private readonly List<ActiveOutcomeEntry> _activeOutcomes = new();

        public SkillPipeline()
        {
            ConditionRegistry = new ConditionRegistry();
            OutcomeRegistry = new OutcomeRegistry();
            EffectRegistry = new EffectRegistry();
        }

        /// <summary>
        /// Load a SkillDef from parsed JSON data.
        /// </summary>
        public SkillDef LoadSkillDef(Dictionary<string, object> data)
        {
            return new SkillDef(data);
        }

        /// <summary>
        /// Activate a skill. Evaluates conditions, executes outcomes.
        /// Returns SkillResult with all changes made.
        /// </summary>
        public SkillResult ActivateSkill(SkillDef skillDef, SkillContext context)
        {
            var result = new SkillResult();

            foreach (var rule in skillDef.Rules)
            {
                // Check all conditions
                bool allMet = true;
                var conditionInstances = new List<SkillConditionBase>();
                foreach (var condData in rule.Conditions)
                {
                    string condType = condData.TryGetValue("type", out var typeObj)
                        ? Convert.ToString(typeObj) : "";
                    var condParams = condData.TryGetValue("params", out var paramsObj)
                        && paramsObj is Dictionary<string, object> p
                        ? p : new Dictionary<string, object>();

                    var cond = ConditionRegistry.Create(condType, condParams);
                    if (cond == null || !cond.IsValid(context))
                    {
                        allMet = false;
                        break;
                    }
                    conditionInstances.Add(cond);
                }

                if (!allMet)
                    continue;

                // Execute all outcomes for this rule
                foreach (var outData in rule.Outcomes)
                {
                    string outcomeType = outData.TryGetValue("type", out var typeObj2)
                        ? Convert.ToString(typeObj2) : "";
                    var outParams = outData.TryGetValue("params", out var paramsObj2)
                        && paramsObj2 is Dictionary<string, object> p2
                        ? new Dictionary<string, object>(p2) : new Dictionary<string, object>();

                    if (outData.TryGetValue("duration", out var durObj))
                        outParams["duration"] = durObj;

                    // Try simple effect registry first
                    if (EffectRegistry.HasEffect(outcomeType))
                    {
                        EffectRegistry.Execute(outcomeType, outParams, context, result);
                        continue;
                    }

                    // Try outcome registry (complex outcomes)
                    var outcome = OutcomeRegistry.Create(outcomeType, outParams);
                    if (outcome == null)
                        continue;

                    outcome.Activate(context, result);

                    // If persistent, track it
                    if (outcome.IsPersistent)
                    {
                        _activeOutcomes.Add(new ActiveOutcomeEntry(outcome, skillDef.Id));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Deactivate all outcomes for a specific skill.
        /// </summary>
        public void DeactivateSkill(int skillId, SkillContext context)
        {
            for (int i = _activeOutcomes.Count - 1; i >= 0; i--)
            {
                if (_activeOutcomes[i].SkillId == skillId)
                {
                    _activeOutcomes[i].Outcome.Deactivate(context);
                    _activeOutcomes.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Called at the start of each turn.
        /// </summary>
        public void ProcessTurnStart(SkillContext context)
        {
            foreach (var entry in _activeOutcomes)
            {
                entry.Outcome.OnTurnStart(context);
            }
        }

        /// <summary>
        /// Called at the end of each turn. Ticks duration and removes expired outcomes.
        /// </summary>
        public void ProcessTurnEnd(SkillContext context)
        {
            for (int i = _activeOutcomes.Count - 1; i >= 0; i--)
            {
                var outcome = _activeOutcomes[i].Outcome;
                outcome.OnTurnEnd(context);
                if (outcome.TurnsLeft > 0)
                {
                    outcome.TurnsLeft -= 1;
                    if (outcome.TurnsLeft <= 0)
                    {
                        outcome.Deactivate(context);
                        _activeOutcomes.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Get count of active persistent outcomes.
        /// </summary>
        public int ActiveOutcomeCount => _activeOutcomes.Count;

        /// <summary>
        /// Clear all active outcomes.
        /// </summary>
        public void ClearActiveOutcomes(SkillContext context)
        {
            foreach (var entry in _activeOutcomes)
            {
                entry.Outcome.Deactivate(context);
            }
            _activeOutcomes.Clear();
        }

        private class ActiveOutcomeEntry
        {
            public SkillOutcomeBase Outcome;
            public int SkillId;

            public ActiveOutcomeEntry(SkillOutcomeBase outcome, int skillId)
            {
                Outcome = outcome;
                SkillId = skillId;
            }
        }
    }
}

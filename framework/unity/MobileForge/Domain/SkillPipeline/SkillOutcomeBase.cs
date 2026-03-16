using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Abstract base class for skill outcomes.
    /// Game code extends this for complex outcomes (e.g., AreaDamage, ChangeGemElement).
    /// </summary>
    public abstract class SkillOutcomeBase
    {
        protected Dictionary<string, object> _params;
        public int TurnsLeft { get; set; }

        public SkillOutcomeBase(Dictionary<string, object> parameters = null)
        {
            _params = parameters ?? new Dictionary<string, object>();
            TurnsLeft = GetParam<int>("duration", 0);
        }

        /// <summary>
        /// Override: execute this outcome.
        /// </summary>
        public virtual void Activate(SkillContext context, SkillResult result)
        {
        }

        /// <summary>
        /// Override: called when this outcome expires or is manually deactivated.
        /// </summary>
        public virtual void Deactivate(SkillContext context)
        {
        }

        /// <summary>
        /// Override: called at start of each turn while active.
        /// </summary>
        public virtual void OnTurnStart(SkillContext context)
        {
        }

        /// <summary>
        /// Override: called at end of each turn while active.
        /// </summary>
        public virtual void OnTurnEnd(SkillContext context)
        {
        }

        /// <summary>
        /// Returns true if this outcome persists across turns (TurnsLeft != 0).
        /// </summary>
        public bool IsPersistent => TurnsLeft != 0;

        /// <summary>
        /// Get a parameter value with default.
        /// </summary>
        public T GetParam<T>(string key, T defaultValue = default)
        {
            if (_params.TryGetValue(key, out var val))
            {
                try
                {
                    return (T)Convert.ChangeType(val, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// Registry of outcome factories keyed by type name.
    /// </summary>
    public class OutcomeRegistry
    {
        private readonly Dictionary<string, Func<Dictionary<string, object>, SkillOutcomeBase>> _factories = new();

        public void Register(string typeName, Func<Dictionary<string, object>, SkillOutcomeBase> factory)
        {
            _factories[typeName] = factory;
        }

        public SkillOutcomeBase Create(string typeName, Dictionary<string, object> parameters)
        {
            if (!_factories.TryGetValue(typeName, out var factory))
                return null;
            return factory(parameters);
        }

        public bool HasType(string typeName)
        {
            return _factories.ContainsKey(typeName);
        }
    }
}

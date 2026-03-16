using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Abstract base class for skill conditions.
    /// Game code extends this for complex conditions (e.g., ComboAbove, HpThreshold).
    /// Subclass must override IsValid().
    /// </summary>
    public abstract class SkillConditionBase
    {
        protected Dictionary<string, object> _params;

        public SkillConditionBase(Dictionary<string, object> parameters = null)
        {
            _params = parameters ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Override in subclass: return true if condition is met.
        /// </summary>
        public virtual bool IsValid(SkillContext context)
        {
            return false;
        }

        /// <summary>
        /// Optional: called when the parent skill activates.
        /// </summary>
        public virtual void OnActivate(SkillContext context)
        {
        }

        /// <summary>
        /// Optional: called when the parent skill deactivates.
        /// </summary>
        public virtual void OnDeactivate()
        {
        }

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
    /// Registry of condition factories keyed by type name.
    /// </summary>
    public class ConditionRegistry
    {
        private readonly Dictionary<string, Func<Dictionary<string, object>, SkillConditionBase>> _factories = new();

        public void Register(string typeName, Func<Dictionary<string, object>, SkillConditionBase> factory)
        {
            _factories[typeName] = factory;
        }

        public SkillConditionBase Create(string typeName, Dictionary<string, object> parameters)
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

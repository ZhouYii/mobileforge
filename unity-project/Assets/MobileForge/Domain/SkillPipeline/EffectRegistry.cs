using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Simple effects via callable registry.
    /// For effects that can be expressed as (params, context, result) -> void in one function.
    /// More complex effects should use SkillOutcomeBase subclasses.
    /// </summary>
    public class EffectRegistry
    {
        private readonly Dictionary<string, Action<Dictionary<string, object>, SkillContext, SkillResult>> _effects = new();

        public void Register(string name, Action<Dictionary<string, object>, SkillContext, SkillResult> callback)
        {
            _effects[name] = callback;
        }

        public bool Execute(string name, Dictionary<string, object> parameters, SkillContext context, SkillResult result)
        {
            if (!_effects.TryGetValue(name, out var callback))
                return false;
            callback(parameters, context, result);
            return true;
        }

        public bool HasEffect(string name)
        {
            return _effects.ContainsKey(name);
        }

        public List<string> GetRegisteredNames()
        {
            return new List<string>(_effects.Keys);
        }
    }
}

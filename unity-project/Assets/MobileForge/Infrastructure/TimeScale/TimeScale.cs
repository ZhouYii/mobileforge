using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Global and per-category animation speed control.
    /// Effective speed = global * category. Default 1.0 for both.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class TimeScale
    {
        private float _global = 1f;
        private readonly Dictionary<string, float> _categories = new();

        /// <summary>Fired when speed changes. Parameter: effective scale.</summary>
        public event Action<float> SpeedChanged;

        /// <summary>Set the global speed multiplier.</summary>
        public void SetGlobalSpeed(float scale)
        {
            _global = Math.Max(scale, 0f);
            SpeedChanged?.Invoke(_global);
        }

        /// <summary>Get the global speed multiplier.</summary>
        public float GetGlobalSpeed() => _global;

        /// <summary>Set speed for a specific category (e.g., "battle", "ui").</summary>
        public void SetCategorySpeed(string category, float scale)
        {
            _categories[category] = Math.Max(scale, 0f);
            SpeedChanged?.Invoke(GetEffectiveSpeed(category));
        }

        /// <summary>Get the effective speed for a category (global * category).</summary>
        public float GetEffectiveSpeed(string category = null)
        {
            float catScale = category != null && _categories.TryGetValue(category, out var s) ? s : 1f;
            return _global * catScale;
        }

        /// <summary>Reset all speeds to 1.0.</summary>
        public void Reset()
        {
            _global = 1f;
            _categories.Clear();
            SpeedChanged?.Invoke(1f);
        }

        /// <summary>Scale a delta value by effective speed for a category.</summary>
        public float ScaleDelta(float delta, string category = null) =>
            delta * GetEffectiveSpeed(category);
    }
}

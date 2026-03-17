using System;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Haptic feedback manager. Light/medium/heavy/custom presets.
    /// Pure C# — delegates to platform via callbacks.
    /// </summary>
    public class HapticManager
    {
        private bool _enabled = true;

        /// <summary>Injected: play haptic by preset name. ("light", "medium", "heavy")</summary>
        public Action<string> OnPlay { get; set; }

        /// <summary>Injected: play custom haptic. (intensity 0-1, durationMs)</summary>
        public Action<float, int> OnPlayCustom { get; set; }

        public void SetEnabled(bool enabled) => _enabled = enabled;
        public bool IsEnabled => _enabled;

        public void Light() => Play("light");
        public void Medium() => Play("medium");
        public void Heavy() => Play("heavy");

        public void Custom(float intensity, int durationMs)
        {
            if (!_enabled) return;
            OnPlayCustom?.Invoke(intensity, durationMs);
        }

        private void Play(string preset)
        {
            if (!_enabled) return;
            OnPlay?.Invoke(preset);
        }
    }
}

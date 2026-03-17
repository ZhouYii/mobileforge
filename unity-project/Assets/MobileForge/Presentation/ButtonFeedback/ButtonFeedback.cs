using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Composable button feedback decorators.
    /// Pure C# — delegates visual/audio effects via callbacks.
    /// Chain decorators fluently: new ButtonFeedback(btn).AddScale().AddColor().AddSound();
    /// </summary>
    public class ButtonFeedback
    {
        private readonly List<Action<bool>> _decorators = new();

        /// <summary>Injected: apply scale tween. (targetScale, duration, pressed)</summary>
        public Action<float, float, bool> OnScaleEffect { get; set; }

        /// <summary>Injected: apply color tint. (r, g, b, a, duration, pressed)</summary>
        public Action<float, float, float, float, float, bool> OnColorEffect { get; set; }

        /// <summary>Injected: apply position offset. (offsetX, offsetY, duration, pressed)</summary>
        public Action<float, float, float, bool> OnOffsetEffect { get; set; }

        /// <summary>Injected: play sound effect. (sfxKey)</summary>
        public Action<string> OnPlaySound { get; set; }

        public ButtonFeedback AddScale(float pressScale = 0.9f, float duration = 0.08f)
        {
            _decorators.Add(pressed => OnScaleEffect?.Invoke(pressScale, duration, pressed));
            return this;
        }

        public ButtonFeedback AddColor(float r = 0.8f, float g = 0.8f, float b = 0.8f, float a = 1f, float duration = 0.05f)
        {
            _decorators.Add(pressed => OnColorEffect?.Invoke(r, g, b, a, duration, pressed));
            return this;
        }

        public ButtonFeedback AddOffset(float offsetX = 0f, float offsetY = 2f, float duration = 0.05f)
        {
            _decorators.Add(pressed => OnOffsetEffect?.Invoke(offsetX, offsetY, duration, pressed));
            return this;
        }

        public ButtonFeedback AddSound(string sfxKey = "btn_click")
        {
            _decorators.Add(pressed => { if (pressed) OnPlaySound?.Invoke(sfxKey); });
            return this;
        }

        /// <summary>Call from engine-specific button press handler.</summary>
        public void OnPress()
        {
            foreach (var d in _decorators) d.Invoke(true);
        }

        /// <summary>Call from engine-specific button release handler.</summary>
        public void OnRelease()
        {
            foreach (var d in _decorators) d.Invoke(false);
        }
    }
}

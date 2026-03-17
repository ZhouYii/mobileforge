using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Floating combat/damage text system.
    /// Pure C# — delegates visual creation and animation to engine layer.
    /// </summary>
    public class FloatingTextLayer
    {
        /// <summary>
        /// Injected: spawn a floating text label.
        /// Parameters: (text, posX, posY, r, g, b, a, scale, duration, rise)
        /// </summary>
        public Action<string, float, float, float, float, float, float, float, float, float> OnSpawn { get; set; }

        /// <summary>Spawn floating text at a position with config.</summary>
        public void Spawn(string text, float x, float y, FloatingTextConfig config = null)
        {
            config ??= new FloatingTextConfig();
            float scale = 1f;
            if (config.Amount > 0)
                scale = Math.Clamp(1f + (float)Math.Log(config.Amount) * 0.1f, 1f, 3f);

            OnSpawn?.Invoke(text, x, y, config.R, config.G, config.B, config.A, scale, config.Duration, config.Rise);
        }

        /// <summary>Convenience: spawn damage number.</summary>
        public void SpawnDamage(int amount, float x, float y, bool isCritical = false)
        {
            Spawn(amount.ToString(), x, y, new FloatingTextConfig
            {
                R = isCritical ? 1f : 1f,
                G = isCritical ? 1f : 0f,
                B = 0f,
                Amount = amount,
            });
        }

        /// <summary>Convenience: spawn heal number.</summary>
        public void SpawnHeal(int amount, float x, float y)
        {
            Spawn("+" + amount, x, y, new FloatingTextConfig
            {
                R = 0f, G = 1f, B = 0f, Amount = amount,
            });
        }
    }

    public class FloatingTextConfig
    {
        public float R { get; set; } = 1f;
        public float G { get; set; } = 1f;
        public float B { get; set; } = 1f;
        public float A { get; set; } = 1f;
        public float Amount { get; set; }
        public float Duration { get; set; } = 0.8f;
        public float Rise { get; set; } = 60f;
    }
}

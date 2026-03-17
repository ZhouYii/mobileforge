using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Screen/camera shake using trauma accumulator + damped sinusoidal.
    /// Pure C# — delegates actual position updates via callback.
    /// </summary>
    public class ScreenShake
    {
        private float _trauma;
        private float _time;

        public float MaxOffsetX { get; set; } = 16f;
        public float MaxOffsetY { get; set; } = 12f;
        public float MaxRotation { get; set; } = 0.05f;
        public float DecayRate { get; set; } = 3f;
        public float Frequency { get; set; } = 15f;

        /// <summary>Injected: apply offset. (offsetX, offsetY, rotation)</summary>
        public Action<float, float, float> OnApplyShake { get; set; }

        /// <summary>Call each frame with delta time.</summary>
        public void Update(float delta)
        {
            if (_trauma <= 0f) return;
            _time += delta;
            _trauma = Math.Max(_trauma - DecayRate * delta, 0f);
            float shake = _trauma * _trauma;
            float ox = MaxOffsetX * shake * (float)Math.Sin(Frequency * _time);
            float oy = MaxOffsetY * shake * (float)Math.Sin(Frequency * _time * 1.3);
            float rot = MaxRotation * shake * (float)Math.Sin(Frequency * _time * 0.7);
            OnApplyShake?.Invoke(ox, oy, rot);
        }

        /// <summary>Add trauma (0-1). Trauma stacks but clamps to 1.</summary>
        public void AddTrauma(float amount)
        {
            _trauma = Math.Clamp(_trauma + amount, 0f, 1f);
        }

        public void ShakeLight() => AddTrauma(0.2f);
        public void ShakeMedium() => AddTrauma(0.5f);
        public void ShakeHeavy() => AddTrauma(0.8f);
    }
}

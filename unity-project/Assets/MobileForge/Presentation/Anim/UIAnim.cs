using System;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Reusable UI animation descriptors. Pure C# — no engine dependency.
    /// Each method returns a UIAnimSequence that the caller drives via Update(delta).
    /// Animation progress is reported via the OnUpdate callback.
    /// </summary>
    public class UIAnimSequence
    {
        private readonly float _duration;
        private readonly Action<float> _onUpdate; // receives normalized t (0..1)
        private readonly Action _onComplete;
        private float _elapsed;
        private float _delay;

        public bool IsComplete { get; private set; }
        public float Progress => _duration > 0f ? Math.Min(_elapsed / _duration, 1f) : 1f;

        public UIAnimSequence(float duration, Action<float> onUpdate, Action onComplete = null, float delay = 0f)
        {
            _duration = Math.Max(duration, 0.001f);
            _onUpdate = onUpdate;
            _onComplete = onComplete;
            _delay = delay;
        }

        /// <summary>Tick the animation. Call once per frame.</summary>
        public void Update(float delta)
        {
            if (IsComplete) return;
            if (_delay > 0f)
            {
                _delay -= delta;
                return;
            }
            _elapsed += delta;
            float t = Math.Min(_elapsed / _duration, 1f);
            _onUpdate?.Invoke(t);
            if (t >= 1f)
            {
                IsComplete = true;
                _onComplete?.Invoke();
            }
        }

        /// <summary>Reset to beginning.</summary>
        public void Reset()
        {
            _elapsed = 0f;
            IsComplete = false;
        }
    }

    /// <summary>
    /// Static factory methods for common UI animations.
    /// All callbacks receive values suitable for directly setting UI properties.
    /// </summary>
    public static class UIAnim
    {
        /// <summary>Fade from 0 to 1. onAlpha(float alpha).</summary>
        public static UIAnimSequence FadeIn(Action<float> onAlpha, float duration = 0.3f, float delay = 0f) =>
            new(duration, t => onAlpha?.Invoke(EaseOutQuad(t)), delay: delay);

        /// <summary>Fade from 1 to 0. onAlpha(float alpha).</summary>
        public static UIAnimSequence FadeOut(Action<float> onAlpha, float duration = 0.3f) =>
            new(duration, t => onAlpha?.Invoke(1f - EaseInQuad(t)));

        /// <summary>Scale punch: overshoot then return. onScale(float scale).</summary>
        public static UIAnimSequence ScalePunch(Action<float> onScale, float intensity = 1.3f, float duration = 0.2f) =>
            new(duration, t =>
            {
                float scale;
                if (t < 0.4f)
                    scale = 1f + (intensity - 1f) * EaseOutBack(t / 0.4f);
                else
                    scale = intensity + (1f - intensity) * EaseOutElastic((t - 0.4f) / 0.6f);
                onScale?.Invoke(scale);
            });

        /// <summary>Bounce in from above. onProgress(float yOffset, float alpha).</summary>
        public static UIAnimSequence BounceIn(Action<float, float> onProgress, float dropDistance = 50f, float duration = 0.4f) =>
            new(duration, t =>
            {
                float y = -dropDistance * (1f - EaseOutBounce(t));
                float alpha = Math.Min(t / 0.3f, 1f);
                onProgress?.Invoke(y, alpha);
            });

        /// <summary>Rotation wobble. onRotation(float degrees).</summary>
        public static UIAnimSequence Wobble(Action<float> onRotation, float degrees = 5f, float duration = 0.4f) =>
            new(duration, t =>
            {
                float decay = 1f - t;
                float angle = (float)Math.Sin(t * Math.PI * 4) * degrees * decay;
                onRotation?.Invoke(angle);
            });

        /// <summary>Fly along bezier arc. onPosition(float x, float y).</summary>
        public static UIAnimSequence FlyTo(
            float startX, float startY, float targetX, float targetY,
            Action<float, float> onPosition, float duration = 0.5f, float arcHeight = 50f) =>
            new(duration, t =>
            {
                float et = EaseInQuad(t);
                float midX = (startX + targetX) * 0.5f;
                float midY = Math.Min(startY, targetY) - arcHeight;
                float u = 1f - et;
                float x = u * u * startX + 2f * u * et * midX + et * et * targetX;
                float y = u * u * startY + 2f * u * et * midY + et * et * targetY;
                onPosition?.Invoke(x, y);
            });

        /// <summary>Number counter. onValue(int current).</summary>
        public static UIAnimSequence CountTo(Action<int> onValue, int startValue, int endValue, float duration = 0.5f) =>
            new(duration, t =>
            {
                int v = startValue + (int)((endValue - startValue) * EaseOutQuad(t));
                onValue?.Invoke(v);
            });

        /// <summary>Pulse scale. onScale(float scale).</summary>
        public static UIAnimSequence Pulse(Action<float> onScale, float scaleAmount = 1.15f, float duration = 0.6f) =>
            new(duration, t =>
            {
                float scale;
                if (t < 0.4f)
                    scale = 1f + (scaleAmount - 1f) * EaseOutSine(t / 0.4f);
                else
                    scale = scaleAmount + (1f - scaleAmount) * EaseInOutSine((t - 0.4f) / 0.6f);
                onScale?.Invoke(scale);
            });

        // ── Easing functions ──

        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseInQuad(float t) => t * t;
        private static float EaseOutSine(float t) => (float)Math.Sin(t * Math.PI * 0.5);
        private static float EaseInOutSine(float t) => -(float)(Math.Cos(Math.PI * t) - 1) * 0.5f;

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * (float)Math.Pow(t - 1, 3) + c1 * (float)Math.Pow(t - 1, 2);
        }

        private static float EaseOutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = (float)(2 * Math.PI) / 3f;
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c4) + 1);
        }

        private static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) return n1 * (t -= 1.5f / d1) * t + 0.75f;
            if (t < 2.5f / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }
    }
}

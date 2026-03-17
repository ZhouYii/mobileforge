using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Flies icons from source to target via bezier arc.
    /// Pure C# — reports positions via callbacks, caller handles rendering.
    /// </summary>
    public class CurrencyFlyTo
    {
        private readonly List<FlyingIcon> _active = new();

        /// <summary>Fired for each icon each frame with its position. Args: iconIndex, x, y.</summary>
        public event Action<int, float, float> OnIconMoved;

        /// <summary>Fired when an icon arrives at target. Args: iconIndex.</summary>
        public event Action<int> OnIconArrived;

        /// <summary>
        /// Start flying icons from source to target.
        /// Returns an AsyncBarrier that resolves when all icons arrive.
        /// </summary>
        public Infrastructure.AsyncBarrier Fly(
            int count,
            float fromX, float fromY,
            float targetX, float targetY,
            float duration = 0.5f,
            float spread = 30f,
            int maxIcons = 10,
            float stagger = 0.03f)
        {
            var barrier = new Infrastructure.AsyncBarrier();
            int iconCount = Math.Min(count, maxIcons);
            if (iconCount <= 0) return barrier;

            var rng = new Random();

            for (int i = 0; i < iconCount; i++)
            {
                string tokenId = $"fly_{i}";
                barrier.Add(tokenId);

                float startX = fromX + (float)(rng.NextDouble() * 2 - 1) * spread;
                float startY = fromY + (float)(rng.NextDouble() * 2 - 1) * spread;
                float arcHeight = 50f + (float)rng.NextDouble() * 30f;

                _active.Add(new FlyingIcon
                {
                    Index = i,
                    TokenId = tokenId,
                    StartX = startX,
                    StartY = startY,
                    TargetX = targetX,
                    TargetY = targetY,
                    ArcHeight = arcHeight,
                    Duration = duration,
                    Delay = stagger * i,
                    Elapsed = 0f,
                    Barrier = barrier,
                });
            }

            return barrier;
        }

        /// <summary>Tick all active animations. Call once per frame.</summary>
        public void Update(float delta)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var icon = _active[i];

                // Handle delay
                if (icon.Delay > 0f)
                {
                    icon.Delay -= delta;
                    continue;
                }

                icon.Elapsed += delta;
                float t = Math.Min(icon.Elapsed / icon.Duration, 1f);
                float et = t * t; // ease-in quad

                // Bezier
                float midX = (icon.StartX + icon.TargetX) * 0.5f;
                float midY = Math.Min(icon.StartY, icon.TargetY) - icon.ArcHeight;
                float u = 1f - et;
                float x = u * u * icon.StartX + 2f * u * et * midX + et * et * icon.TargetX;
                float y = u * u * icon.StartY + 2f * u * et * midY + et * et * icon.TargetY;

                OnIconMoved?.Invoke(icon.Index, x, y);

                if (t >= 1f)
                {
                    icon.Barrier.Resolve(icon.TokenId);
                    OnIconArrived?.Invoke(icon.Index);
                    _active.RemoveAt(i);
                }
            }
        }

        /// <summary>Whether any icons are still flying.</summary>
        public bool IsActive => _active.Count > 0;

        private class FlyingIcon
        {
            public int Index;
            public string TokenId;
            public float StartX, StartY, TargetX, TargetY;
            public float ArcHeight, Duration, Delay, Elapsed;
            public Infrastructure.AsyncBarrier Barrier;
        }
    }
}

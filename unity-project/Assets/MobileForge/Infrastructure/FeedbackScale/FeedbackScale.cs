using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Maps a numeric value to a proportional feedback intensity.
    /// Bigger damage = more particles, deeper sounds, larger shakes.
    /// Pure C# — static utility methods.
    /// </summary>
    public static class FeedbackScale
    {
        /// <summary>Linear interpolation between minOut and maxOut.</summary>
        public static float Linear(float value, float minOut, float maxOut, float maxValue)
        {
            if (maxValue <= 0f) return minOut;
            float t = Math.Clamp(value / maxValue, 0f, 1f);
            return minOut + (maxOut - minOut) * t;
        }

        /// <summary>Logarithmic scaling — diminishing returns for large values.</summary>
        public static float Logarithmic(float value, float minOut, float maxOut)
        {
            if (value <= 0f) return minOut;
            float t = (float)Math.Clamp(Math.Log(value + 1) / Math.Log(101), 0, 1);
            return minOut + (maxOut - minOut) * t;
        }

        /// <summary>
        /// Tiered: returns output for the highest threshold not exceeded.
        /// Tiers must be sorted ascending by threshold.
        /// </summary>
        public static float Tiered(float value, List<(float threshold, float output)> tiers)
        {
            float result = 0f;
            foreach (var (threshold, output) in tiers)
            {
                if (value >= threshold)
                    result = output;
                else
                    break;
            }
            return result;
        }

        /// <summary>Clamp an integer count to a range.</summary>
        public static int ClampCount(int value, int minCount, int maxCount) =>
            Math.Clamp(value, minCount, maxCount);
    }
}

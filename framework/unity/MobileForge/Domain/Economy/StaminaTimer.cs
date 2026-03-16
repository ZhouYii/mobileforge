using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Calculates stamina refill based on elapsed time.
    /// </summary>
    public class StaminaTimer
    {
        public StaminaConfig Config { get; }
        private double _lastUpdateTime;

        public StaminaTimer(StaminaConfig config = null)
        {
            Config = config;
            _lastUpdateTime = 0.0;
        }

        /// <summary>
        /// Calculate how much stamina has regenerated since last update.
        /// Returns dictionary with "stamina" (int) and "remainder_seconds" (double).
        /// </summary>
        public Dictionary<string, object> CalculateRefill(int currentStamina, double currentTime)
        {
            if (Config == null || _lastUpdateTime <= 0.0)
            {
                _lastUpdateTime = currentTime;
                return new Dictionary<string, object>
                {
                    { "stamina", currentStamina },
                    { "remainder_seconds", 0.0 }
                };
            }

            double elapsed = currentTime - _lastUpdateTime;
            int pointsGained = (int)(elapsed / Config.RefillRateSeconds);
            double remainder = elapsed % Config.RefillRateSeconds;

            int newStamina = Math.Min(currentStamina + pointsGained, Config.MaxStamina);
            _lastUpdateTime = currentTime - remainder;

            return new Dictionary<string, object>
            {
                { "stamina", newStamina },
                { "remainder_seconds", remainder }
            };
        }

        /// <summary>
        /// Get seconds until next stamina point.
        /// </summary>
        public double SecondsUntilNext(int currentStamina, double currentTime)
        {
            if (Config == null || currentStamina >= Config.MaxStamina)
                return 0.0;
            double elapsed = currentTime - _lastUpdateTime;
            return Math.Max(Config.RefillRateSeconds - (elapsed % Config.RefillRateSeconds), 0.0);
        }

        public void SetLastUpdate(double time)
        {
            _lastUpdateTime = time;
        }
    }
}

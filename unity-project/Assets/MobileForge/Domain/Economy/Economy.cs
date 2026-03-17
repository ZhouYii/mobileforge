using System;
using System.Collections.Generic;
using MobileForge.Infrastructure;

namespace MobileForge.Domain
{
    /// <summary>
    /// Currency and stamina management. Uses PlayerState for persistence.
    /// </summary>
    public class Economy
    {
        private readonly PlayerState _playerState;
        private readonly EventBus _eventBus;
        private StaminaTimer _staminaTimer;
        private StaminaConfig _staminaConfig;

        public Economy(PlayerState playerState = null, EventBus eventBus = null)
        {
            _playerState = playerState;
            _eventBus = eventBus;
        }

        public void SetupStamina(StaminaConfig config)
        {
            _staminaConfig = config;
            _staminaTimer = new StaminaTimer(config);
        }

        public bool CanAfford(string currency, int amount)
        {
            if (_playerState == null)
                return false;
            int current = Convert.ToInt32(_playerState.GetValue("currencies", currency, 0));
            return current >= amount;
        }

        public bool Spend(string currency, int amount)
        {
            if (!CanAfford(currency, amount))
                return false;
            int current = Convert.ToInt32(_playerState.GetValue("currencies", currency, 0));
            _playerState.SetValue("currencies", currency, current - amount);
            EmitCurrencyChanged(currency, current, current - amount);
            return true;
        }

        public void Earn(string currency, int amount)
        {
            if (_playerState == null)
                return;
            int current = Convert.ToInt32(_playerState.GetValue("currencies", currency, 0));
            int newVal = current + amount;
            _playerState.SetValue("currencies", currency, newVal);
            EmitCurrencyChanged(currency, current, newVal);
        }

        public int GetBalance(string currency)
        {
            if (_playerState == null)
                return 0;
            return Convert.ToInt32(_playerState.GetValue("currencies", currency, 0));
        }

        public bool CheckStamina(int cost)
        {
            return GetBalance("stamina") >= cost;
        }

        public bool SpendStamina(int cost)
        {
            return Spend("stamina", cost);
        }

        private void EmitCurrencyChanged(string currency, int oldVal, int newVal)
        {
            _eventBus?.Emit(EventNames.CurrencyChanged, new Dictionary<string, object>
            {
                { "currency_type", currency },
                { "old_value", oldVal },
                { "new_value", newVal },
            });
        }
    }
}

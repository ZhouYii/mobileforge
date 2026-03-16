using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Currency display model with animated count interpolation.
    /// Smoothly animates the displayed value toward the target.
    /// Pure C# — rendering is delegated to engine-specific code.
    /// </summary>
    public class CurrencyBar
    {
        private string _currencyType;
        private Infrastructure.EventBus _eventBus;
        private Action<Dictionary<string, object>> _stateChangedHandler;

        /// <summary>
        /// The currently displayed (interpolated) value.
        /// </summary>
        public long CurrentDisplay { get; private set; }

        /// <summary>
        /// The target value we are animating toward.
        /// </summary>
        public long TargetValue { get; private set; }

        /// <summary>
        /// The currency type this bar is bound to.
        /// </summary>
        public string CurrencyType => _currencyType;

        /// <summary>
        /// Speed of the count animation, in units per second.
        /// Higher values = faster animation.
        /// </summary>
        public float AnimationSpeed { get; set; } = 500f;

        /// <summary>
        /// Whether the display is currently animating toward the target.
        /// </summary>
        public bool IsAnimating => CurrentDisplay != TargetValue;

        /// <summary>
        /// Fired when the displayed value changes. Parameter: newDisplayValue.
        /// </summary>
        public Action<long> OnDisplayChanged;

        /// <summary>
        /// Bind this currency bar to a currency type and event bus.
        /// Subscribes to state change events for the given currency.
        /// </summary>
        public void Bind(string currencyType, Infrastructure.EventBus eventBus)
        {
            Unbind();

            _currencyType = currencyType;
            _eventBus = eventBus;

            _stateChangedHandler = OnStateChanged;
            _eventBus.Subscribe(Infrastructure.EventNames.StateChanged, _stateChangedHandler);
        }

        /// <summary>
        /// Unbind from the event bus.
        /// </summary>
        public void Unbind()
        {
            if (_eventBus != null && _stateChangedHandler != null)
            {
                _eventBus.Unsubscribe(Infrastructure.EventNames.StateChanged, _stateChangedHandler);
                _stateChangedHandler = null;
            }

            _eventBus = null;
            _currencyType = null;
        }

        /// <summary>
        /// Directly set the target value. The displayed value will animate toward it.
        /// </summary>
        public void SetValue(long value)
        {
            TargetValue = value;
        }

        /// <summary>
        /// Immediately snap the display to the target value (no animation).
        /// </summary>
        public void SetImmediate(long value)
        {
            TargetValue = value;
            CurrentDisplay = value;
            OnDisplayChanged?.Invoke(CurrentDisplay);
        }

        /// <summary>
        /// Tick the animation. Call each frame with elapsed time in seconds.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (CurrentDisplay == TargetValue)
                return;

            long diff = TargetValue - CurrentDisplay;
            long step = (long)(AnimationSpeed * deltaTime);

            if (step < 1) step = 1;

            if (Math.Abs(diff) <= step)
            {
                CurrentDisplay = TargetValue;
            }
            else if (diff > 0)
            {
                CurrentDisplay += step;
            }
            else
            {
                CurrentDisplay -= step;
            }

            OnDisplayChanged?.Invoke(CurrentDisplay);
        }

        private void OnStateChanged(Dictionary<string, object> payload)
        {
            if (payload == null) return;

            // Check if this state change is for our currency
            if (!payload.TryGetValue("section", out var section)) return;
            if (!payload.TryGetValue("key", out var key)) return;

            // Match currency type: section "currency" with key matching our type,
            // or section matching our type directly.
            bool isOurCurrency =
                (Convert.ToString(section) == "currency" && Convert.ToString(key) == _currencyType) ||
                (Convert.ToString(section) == _currencyType);

            if (isOurCurrency && payload.TryGetValue("new_value", out var newValue))
            {
                TargetValue = Convert.ToInt64(newValue);
            }
        }
    }
}

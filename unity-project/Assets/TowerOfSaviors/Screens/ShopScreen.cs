using System.Collections.Generic;
using MobileForge.Domain;
using MobileForge.Presentation;

namespace TowerOfSaviors
{
    /// <summary>
    /// Simple shop screen — stamina refill and free gem debug button.
    /// Mirrors shop_screen.gd.
    /// </summary>
    public class ShopScreen : IScreen
    {
        private Economy _economy;
        private UIRouter _router;

        // Currency display state
        public int GemsBalance => _economy.GetBalance("gems");
        public int CoinsBalance => _economy.GetBalance("coins");
        public int StaminaBalance => _economy.GetBalance("stamina");
        public string StatusMessage { get; private set; } = "";

        public void Setup(Economy economy, UIRouter router)
        {
            _economy = economy;
            _router = router;
        }

        public void OnEnter(Dictionary<string, object> parameters)
        {
            StatusMessage = "";
        }

        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        /// <summary>
        /// Spend 1 gem to gain 50 stamina.
        /// Returns true if the purchase succeeded.
        /// </summary>
        public bool RefillStamina()
        {
            if (_economy.Spend("gems", 1))
            {
                _economy.Earn("stamina", 50);
                StatusMessage = "Stamina refilled! +50";
                return true;
            }
            else
            {
                StatusMessage = "Not enough gems!";
                return false;
            }
        }

        /// <summary>
        /// Debug helper — grants 50 free gems.
        /// </summary>
        public void AddFreeGems(int amount = 50)
        {
            _economy.Earn("gems", amount);
            StatusMessage = $"Received {amount} free gems!";
        }

        /// <summary>
        /// Navigate back to the previous screen.
        /// </summary>
        public void GoBack() => _router.Pop();
    }
}

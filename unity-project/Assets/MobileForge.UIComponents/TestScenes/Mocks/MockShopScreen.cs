using System.Reflection;
using System.Collections.Generic;
using MobileForge.Domain;
using MobileForge.Infrastructure;
using TowerOfSaviors;
using UnityEngine;

namespace MobileForge.UIComponents.TestScenes.Mocks
{
    /// <summary>
    /// Mock ShopScreen for page preview.
    /// Sets up a real Economy so balance properties don't NRE.
    /// </summary>
    public class MockShopScreen : ShopScreen
    {
        public MockShopScreen()
        {
            var eventBus = new EventBus();
            var playerState = new PlayerState(eventBus);
            playerState.RegisterSection("currencies", new Dictionary<string, object>
            {
                { "gems", 50 },
                { "coins", 10000 },
                { "stamina", 75 }
            });
            var economy = new Economy(playerState, eventBus);

            var field = typeof(ShopScreen).GetField("_economy",
                BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(this, economy);

            Debug.Log("[MockShopScreen] Initialized with sample economy");
        }
    }
}

using System;
using System.Reflection;
using System.Collections.Generic;
using MobileForge.Domain;
using MobileForge.Infrastructure;
using MobileForge.Presentation;
using TowerOfSaviors;
using UnityEngine;

namespace MobileForge.UIComponents.TestScenes.Mocks
{
    /// <summary>
    /// Mock GachaScreen for page preview.
    /// Sets up a real Economy with sample currency so GemsBalance doesn't NRE.
    /// Populates Pools with a sample pool for pull button display.
    /// </summary>
    public class MockGachaScreen : GachaScreen
    {
        public MockGachaScreen()
        {
            // Create a minimal Economy with sample currencies
            var eventBus = new EventBus();
            var playerState = new PlayerState(eventBus);
            playerState.RegisterSection("currencies", new Dictionary<string, object>
            {
                { "gems", 500 },
                { "coins", 10000 }
            });
            var economy = new Economy(playerState, eventBus);

            // Set private fields via reflection
            SetField("_economy", economy);

            // Add a sample pool to Pools list
            var sampleEntries = new List<GachaEntry>
            {
                new GachaEntry(1, 3, 100, false),
                new GachaEntry(2, 4, 50, false),
                new GachaEntry(3, 5, 10, true),
            };
            var samplePool = new GachaPool(1, "Featured Banner", "gems", 5,
                sampleEntries, 50, new[] { 3 });

            Pools.Add(new GachaPoolDisplay
            {
                Pool = samplePool,
                Rates = new Dictionary<int, double> { { 3, 0.6 }, { 4, 0.3 }, { 5, 0.1 } }
            });

            Debug.Log("[MockGachaScreen] Initialized with sample economy and pool");
        }

        private void SetField(string fieldName, object value)
        {
            var field = typeof(GachaScreen).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(this, value);
        }
    }
}

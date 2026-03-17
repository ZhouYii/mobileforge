using System.Collections.Generic;
using MobileForge.Presentation;
using TowerOfSaviors;
using UnityEngine;

namespace MobileForge.UIComponents.TestScenes.Mocks
{
    /// <summary>
    /// Mock ResultScreen for page preview. Shows a victory with sample rewards.
    /// OnContinue will log instead of crashing because we pass a real (empty) router.
    /// </summary>
    public class MockResultScreen : ResultScreen
    {
        // Keep a reference so the preview's Continue button has a working router
        public UIRouter MockRouter { get; }

        public MockResultScreen()
        {
            MockRouter = new UIRouter(new ScreenRegistry());

            // Register a dummy title screen so Navigate("title") doesn't throw
            MockRouter.Register("title", _ =>
            {
                Debug.Log("[MockResultScreen] Would navigate to title");
                return null;
            });

            Setup(new Dictionary<string, object>
            {
                { "won", true },
                { "rewards", new Dictionary<string, object>
                    {
                        { "coins", 500 },
                        { "exp", 100 },
                        { "gems", 5 }
                    }
                }
            });
            OnEnter(null);
        }
    }
}

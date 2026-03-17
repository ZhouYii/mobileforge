using System.Reflection;
using MobileForge.Presentation;
using TowerOfSaviors;
using UnityEngine;

namespace MobileForge.UIComponents.TestScenes.Mocks
{
    /// <summary>
    /// Mock TitleScreen for page preview.
    /// Sets a mock router that logs instead of navigating.
    /// </summary>
    public class MockTitleScreen : TitleScreen
    {
        public MockTitleScreen()
            : base(new UIRouter(new ScreenRegistry()))
        {
            Debug.Log("[MockTitleScreen] Initialized with mock router");
        }
    }
}

using System.Collections.Generic;

namespace MobileForge.UIComponents.TestScenes.Mocks
{
    /// <summary>
    /// Factory that creates all mock screens for the Page Preview scene.
    /// </summary>
    public static class MockScreenFactory
    {
        public static Dictionary<string, object> CreateAll()
        {
            return new Dictionary<string, object>
            {
                { "title", new MockTitleScreen() },
                { "battle", new MockBattleScreen() },
                { "gacha", new MockGachaScreen() },
                { "shop", new MockShopScreen() },
                { "result", new MockResultScreen() },
            };
        }
    }
}

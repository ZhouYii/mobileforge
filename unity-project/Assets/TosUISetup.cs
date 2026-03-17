using UnityEngine;
using UnityEngine.UI;
using MobileForge.Presentation;
using MobileForge.UIComponents;
using MobileForge.UIComponents.Pages;
using MobileForge.UIComponents.Primitives;
using TowerOfSaviors;

/// <summary>
/// Registers ToS screen → page mappings and sets up the UIComponentRouter.
/// Attach to a GameObject in the scene alongside GameBootstrap.
/// Replaces GameRenderer's monolithic switch with composable page components.
/// </summary>
public class TosUISetup : MonoBehaviour
{
    [Header("Optional: assign a MFPrimitiveLibrary to override default primitives")]
    [SerializeField] private MFPrimitiveLibrary _primitiveLibrary;

    private UIComponentRouter _router;

    void Start()
    {
        var bootstrap = FindFirstObjectByType<GameBootstrap>();
        if (bootstrap == null)
        {
            Debug.LogError("[TosUISetup] GameBootstrap not found!");
            return;
        }

        // Optional: override primitive library
        if (_primitiveLibrary != null)
            MFPrimitiveLibrary.SetInstance(_primitiveLibrary);

        // Find or create Canvas
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[TosUISetup] Canvas not found!");
            return;
        }

        // Configure CanvasScaler for responsive layout
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Create screen root
        var screenRoot = new GameObject("UIComponentRoot");
        screenRoot.transform.SetParent(canvas.transform, false);
        var rootRect = screenRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // Build registry
        var registry = new UIComponentRegistry();
        var game = bootstrap.Game;
        RegisterPages(registry, game);

        // Setup router
        _router = gameObject.AddComponent<UIComponentRouter>();
        _router.Setup(game.Router, registry, screenRoot.transform);

        // Wire JS bridge notification for WebGL automation
#if UNITY_WEBGL && !UNITY_EDITOR
        _router.OnScreenRendered = screenId => AutomationBridge.NotifyScreenChanged(screenId);
#endif

        Debug.Log("[TosUISetup] UI component system initialized");
    }

    private void RegisterPages(UIComponentRegistry registry, TosGame game)
    {
        // Title
        registry.Register("title", () =>
        {
            var go = new GameObject("TitlePage");
            return go.AddComponent<TitlePage>();
        });

        // Dungeon Select
        registry.Register("dungeon_select", () =>
        {
            var go = new GameObject("LevelSelectPage");
            var page = go.AddComponent<LevelSelectPage>();
            page.SetRouter(game.Router);
            return page;
        });

        // Team Select
        registry.Register("team_select", () =>
        {
            var go = new GameObject("TeamSelectPage");
            var page = go.AddComponent<TeamSelectPage>();
            page.SetGameData(game.GameData);
            return page;
        });

        // Battle
        registry.Register("battle", () =>
        {
            var go = new GameObject("BattlePage");
            return go.AddComponent<BattlePage>();
        });

        // Result
        registry.Register("result", () =>
        {
            var go = new GameObject("ResultPage");
            var page = go.AddComponent<ResultPage>();
            page.SetRouter(game.Router);
            return page;
        });

        // Gacha
        registry.Register("gacha", () =>
        {
            var go = new GameObject("GachaPage");
            return go.AddComponent<GachaPage>();
        });

        // Monster Box
        registry.Register("monster_box", () =>
        {
            var go = new GameObject("InventoryPage");
            return go.AddComponent<InventoryPage>();
        });

        // Shop
        registry.Register("shop", () =>
        {
            var go = new GameObject("ShopPage");
            return go.AddComponent<ShopPage>();
        });

        // Fallback for any unregistered screens
        foreach (var screenId in new[] { "achievements", "pvp", "settings" })
        {
            string id = screenId;
            registry.Register(id, () =>
            {
                var go = new GameObject("PlaceholderPage");
                var page = go.AddComponent<PlaceholderPage>();
                page.SetScreenId(id);
                page.SetRouter(game.Router);
                return page;
            });
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using MobileForge.Presentation;
using MobileForge.UIComponents;
using MobileForge.UIComponents.Pages;
using MobileForge.UIComponents.Primitives;
using TowerOfSaviors;

/// <summary>
/// Registers ToS screen -> page mappings and sets up the UIComponentRouter.
/// Attach to a GameObject in the scene alongside GameBootstrap.
/// Now also creates a persistent bottom navigation bar matching original TOS MenuLayer.
/// </summary>
public class TosUISetup : MonoBehaviour
{
    [Header("Optional: assign a MFPrimitiveLibrary to override default primitives")]
    [SerializeField] private MFPrimitiveLibrary _primitiveLibrary;

    private UIComponentRouter _router;
    private MFPopupHost _popupHost;
    private MFToastHost _toastHost;
    private MFOverlayHost _overlayHost;
    private MainNavBar _navBar;

    /// <summary>Global popup host — also exposed via UIServices for cross-assembly access.</summary>
    public static MFPopupHost Popups { get; private set; }
    /// <summary>Global toast host — also exposed via UIServices for cross-assembly access.</summary>
    public static MFToastHost Toasts { get; private set; }
    /// <summary>Global overlay host — also exposed via UIServices for cross-assembly access.</summary>
    public static MFOverlayHost Overlays { get; private set; }
    /// <summary>Global nav bar reference.</summary>
    public static MainNavBar NavBar { get; private set; }

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

        // Create screen root (leaves space for nav bar at bottom)
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
        _router.Setup(game.Router, registry, screenRoot.transform, TosTheme.AnimTransition);

        // ── Persistent Bottom Navigation Bar ──
        var navBarGo = new GameObject("MainNavBar");
        navBarGo.transform.SetParent(canvas.transform, false);
        _navBar = navBarGo.AddComponent<MainNavBar>();
        _navBar.Setup(game.Router);
        _navBar.OnTabSelected = screenId =>
        {
            // Navigate (replace stack) to maintain clean nav state
            game.Router.Navigate(screenId);
        };
        // Set badge dots
        _navBar.SetBadge("social", true);  // unread mail/friend requests
        NavBar = _navBar;

        // ── Popup/Toast/Overlay Infrastructure ──

        // Overlay host (z: 1000)
        var overlayRoot = new GameObject("OverlayRoot");
        overlayRoot.transform.SetParent(canvas.transform, false);
        SetupFullStretch(overlayRoot);
        _overlayHost = gameObject.AddComponent<MFOverlayHost>();
        _overlayHost.Setup(overlayRoot.transform);
        Overlays = _overlayHost;
        UIServices.Overlays = _overlayHost;

        // Popup host (z: 2000)
        var popupRoot = new GameObject("PopupRoot");
        popupRoot.transform.SetParent(canvas.transform, false);
        SetupFullStretch(popupRoot);
        _popupHost = gameObject.AddComponent<MFPopupHost>();
        _popupHost.Setup(popupRoot.transform);
        Popups = _popupHost;
        UIServices.Popups = _popupHost;

        // Toast host (z: 3000)
        var toastRoot = new GameObject("ToastRoot");
        toastRoot.transform.SetParent(canvas.transform, false);
        SetupFullStretch(toastRoot);
        _toastHost = gameObject.AddComponent<MFToastHost>();
        _toastHost.Setup(toastRoot.transform);
        Toasts = _toastHost;
        UIServices.Toasts = _toastHost;

        // Wire JS bridge notification for WebGL automation
#if UNITY_WEBGL && !UNITY_EDITOR
        _router.OnScreenRendered = screenId => AutomationBridge.NotifyScreenChanged(screenId);
#endif

        Debug.Log("[TosUISetup] UI component system initialized (with nav bar + popup/toast/overlay)");
    }

    private static void SetupFullStretch(GameObject go)
    {
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Try loading a prefab; fall back to creating a new GameObject with the component.
    /// Prefab path is relative to a Resources folder (e.g. "Pages/SettingsPage").
    /// </summary>
    private static T CreatePage<T>(string prefabPath = null) where T : MonoBehaviour
    {
        if (!string.IsNullOrEmpty(prefabPath))
        {
            var prefab = Resources.Load<T>(prefabPath);
            if (prefab != null)
                return Object.Instantiate(prefab);
        }
        var go = new GameObject(typeof(T).Name);
        return go.AddComponent<T>();
    }

    private void RegisterPages(UIComponentRegistry registry, TosGame game)
    {
        // Title / Home
        registry.Register("title", () => CreatePage<TitlePage>("Pages/TitlePage"));

        // Dungeon Select
        registry.Register("dungeon_select", () =>
        {
            var page = CreatePage<LevelSelectPage>("Pages/LevelSelectPage");
            page.SetRouter(game.Router);
            return page;
        });

        // Team Select (pre-battle)
        registry.Register("team_select", () =>
        {
            var page = CreatePage<TeamSelectPage>("Pages/TeamSelectPage");
            page.SetGameData(game.GameData);
            return page;
        });

        // Battle
        registry.Register("battle", () => CreatePage<BattlePage>("Pages/BattlePage"));

        // Result
        registry.Register("result", () =>
        {
            var page = CreatePage<ResultPage>("Pages/ResultPage");
            page.SetRouter(game.Router);
            return page;
        });

        // Gacha
        registry.Register("gacha", () => CreatePage<GachaPage>("Pages/GachaPage"));

        // Monster Box
        registry.Register("monster_box", () => CreatePage<InventoryPage>("Pages/InventoryPage"));

        // Shop
        registry.Register("shop", () => CreatePage<ShopPage>("Pages/ShopPage"));

        // ── New screens matching original TOS ──

        // World Map (zone navigation)
        registry.Register("world_map", () => CreatePage<WorldMapPage>("Pages/WorldMapPage"));

        // Social Hub
        registry.Register("social", () => CreatePage<SocialPage>("Pages/SocialPage"));

        // Settings / Preferences
        registry.Register("settings", () => CreatePage<SettingsPage>("Pages/SettingsPage"));

        // Daily Check-In
        registry.Register("daily_checkin", () => CreatePage<DailyCheckInPage>("Pages/DailyCheckInPage"));

        // Mail Inbox
        registry.Register("mail", () => CreatePage<MailPage>("Pages/MailPage"));

        // Team Management
        registry.Register("team_manage", () => CreatePage<TeamManagePage>("Pages/TeamManagePage"));

        // Fallback for unimplemented screens
        foreach (var screenId in new[]
        {
            "achievements", "pvp", "friends", "rankings", "profile"
        })
        {
            string id = screenId;
            registry.Register(id, () =>
            {
                var page = CreatePage<PlaceholderPage>("Pages/PlaceholderPage");
                page.SetScreenId(id);
                page.SetRouter(game.Router);
                return page;
            });
        }
    }
}

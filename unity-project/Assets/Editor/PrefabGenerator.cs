using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Generates skeletal UI prefabs for all ToS page screens.
/// Each prefab contains the static layout hierarchy with serialized field references
/// wired up. Dynamic content (grids, lists, boards) is still built in code by the page.
///
/// Usage: Unity menu → MobileForge → Generate All Page Prefabs
/// Output: Assets/Resources/Pages/*.prefab
/// </summary>
public static class PrefabGenerator
{
    private const string OutputDir = "Assets/Resources/Pages";
    private static Font _font;

    [MenuItem("MobileForge/Generate All Page Prefabs")]
    public static void GenerateAll()
    {
        EnsureDirectory();
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GenerateTitlePage();
        GenerateBattlePage();
        GenerateLevelSelectPage();
        GenerateTeamSelectPage();
        GenerateResultPage();
        GenerateGachaPage();
        GenerateInventoryPage();
        GenerateShopPage();
        GenerateWorldMapPage();
        GenerateSocialPage();
        GenerateSettingsPage();
        GenerateDailyCheckInPage();
        GenerateMailPage();
        GenerateTeamManagePage();
        GeneratePlaceholderPage();

        AssetDatabase.Refresh();
        Debug.Log($"[PrefabGenerator] Generated all page prefabs in {OutputDir}/");
    }

    // ─── Title Page ─────────────────────────────────────────────────────

    [MenuItem("MobileForge/Generate Page Prefabs/TitlePage")]
    public static void GenerateTitlePage()
    {
        var root = CreatePageRoot("TitlePage");
        var bg = AddBg(root, DarkBg);

        // Title label
        var titleLabel = CreateLabel(root.transform, "TitleLabel",
            "Tower of Saviors", 36, GoldText, TextAnchor.MiddleCenter);
        AnchorTop(titleLabel, 16, 56);
        AddShadow(titleLabel.gameObject);

        // Player info card
        var infoCard = CreatePanel(root.transform, "PlayerInfoCard", 76, 64, PanelBg);

        // Player info row
        var playerName = CreateLabel(infoCard.transform, "PlayerNameText",
            "Player", 18, Color.white, TextAnchor.MiddleLeft);
        AnchorRegion(playerName, 0.02f, 0.5f, 0.5f, 1f, 12, 0, 0, 0);

        var playerRank = CreateLabel(infoCard.transform, "PlayerRankText",
            "Rank 1", 12, MutedText, TextAnchor.MiddleLeft);
        AnchorRegion(playerRank, 0.02f, 0f, 0.5f, 0.5f, 12, 0, 0, 0);

        // Currencies
        var gemsText = CreateLabel(infoCard.transform, "GemsText",
            "0 Gems", 12, GoldText, TextAnchor.MiddleRight);
        AnchorRegion(gemsText, 0.55f, 0.66f, 0.98f, 1f, 0, 0, 8, 0);

        var coinsText = CreateLabel(infoCard.transform, "CoinsText",
            "0 Coins", 12, new Color(0.9f, 0.8f, 0.3f), TextAnchor.MiddleRight);
        AnchorRegion(coinsText, 0.55f, 0.33f, 0.98f, 0.66f, 0, 0, 8, 0);

        var staminaText = CreateLabel(infoCard.transform, "StaminaText",
            "0/0 ST", 12, new Color(0.3f, 0.9f, 0.3f), TextAnchor.MiddleRight);
        AnchorRegion(staminaText, 0.55f, 0f, 0.98f, 0.33f, 0, 0, 8, 0);

        // Feature grid container
        var featureGrid = CreateContainer(root.transform, "FeatureGrid");
        AnchorFill(featureGrid, 14, 190, 14, 80);

        // Wire serialized fields
        var page = root.AddComponent<MobileForge.UIComponents.Pages.TitlePage>();
        SetField(page, "_titleLabel", titleLabel);
        SetField(page, "_playerInfoCard", infoCard);
        SetField(page, "_featureGrid", featureGrid.transform);
        SetField(page, "_playerNameText", playerName);
        SetField(page, "_playerRankText", playerRank);
        SetField(page, "_gemsText", gemsText);
        SetField(page, "_coinsText", coinsText);
        SetField(page, "_staminaText", staminaText);

        SavePrefab(root, "TitlePage");
    }

    // ─── Battle Page ────────────────────────────────────────────────────

    [MenuItem("MobileForge/Generate Page Prefabs/BattlePage")]
    public static void GenerateBattlePage()
    {
        var root = CreatePageRoot("BattlePage");
        AddBg(root, DarkBg);

        // Header bar
        var header = CreatePanel(root.transform, "Header", 0, 40, new Color(0.1f, 0.1f, 0.14f, 0.92f));
        AnchorTop(header.GetComponent<RectTransform>(), 0, 40);

        var waveLabel = CreateLabel(header.transform, "WaveLabel",
            "Wave 1", 14, Color.white, TextAnchor.MiddleCenter);
        AnchorRegion(waveLabel, 0.15f, 0f, 0.6f, 1f, 0, 0, 0, 0);

        var turnLabel = CreateLabel(header.transform, "TurnLabel",
            "Turn 1", 14, Color.white, TextAnchor.MiddleCenter);
        AnchorRegion(turnLabel, 0.6f, 0f, 0.95f, 1f, 0, 0, 0, 0);

        // Enemy area
        var enemyArea = CreateContainer(root.transform, "EnemyArea");
        var eaRect = enemyArea.GetComponent<RectTransform>();
        eaRect.anchorMin = new Vector2(0f, 0.8f);
        eaRect.anchorMax = new Vector2(1f, 0.96f);
        eaRect.offsetMin = new Vector2(4, 0);
        eaRect.offsetMax = new Vector2(-4, -44);

        // Board area
        var boardArea = CreateContainer(root.transform, "BoardArea");
        AnchorFill(boardArea, 8, 230, 8, 110);

        // Skill area
        var skillArea = CreateContainer(root.transform, "SkillArea");
        var saRect = skillArea.GetComponent<RectTransform>();
        saRect.anchorMin = new Vector2(0, 0);
        saRect.anchorMax = new Vector2(1, 0);
        saRect.pivot = new Vector2(0.5f, 0);
        saRect.offsetMin = new Vector2(4, 112);
        saRect.offsetMax = new Vector2(-4, 160);

        // Action area
        var actionArea = CreateContainer(root.transform, "ActionArea");
        var aaRect = actionArea.GetComponent<RectTransform>();
        aaRect.anchorMin = Vector2.zero;
        aaRect.anchorMax = new Vector2(1, 0);
        aaRect.pivot = new Vector2(0.5f, 0);
        aaRect.offsetMin = new Vector2(8, 8);
        aaRect.offsetMax = new Vector2(-8, 108);

        var page = root.AddComponent<MobileForge.UIComponents.Pages.BattlePage>();
        SetField(page, "_waveLabel", waveLabel);
        SetField(page, "_turnLabel", turnLabel);
        SetField(page, "_enemyArea", eaRect);
        SetField(page, "_boardArea", boardArea.GetComponent<RectTransform>());
        SetField(page, "_skillArea", skillArea.GetComponent<RectTransform>());
        SetField(page, "_actionArea", actionArea.GetComponent<RectTransform>());

        SavePrefab(root, "BattlePage");
    }

    // ─── Level Select Page ──────────────────────────────────────────────

    [MenuItem("MobileForge/Generate Page Prefabs/LevelSelectPage")]
    public static void GenerateLevelSelectPage()
    {
        var root = CreatePageRoot("LevelSelectPage");
        AddBg(root, DarkBg);

        var header = BuildStandardHeader(root.transform, "Select Dungeon", Color.white,
            out var backBtn, out var titleLabel);

        var staminaLabel = CreateLabel(header.transform, "StaminaLabel",
            "ST: 0/0", 14, new Color(0.6f, 1f, 0.6f), TextAnchor.MiddleRight);
        AnchorRegion(staminaLabel, 0.7f, 0f, 0.98f, 1f, 0, 0, 8, 0);

        var page = root.AddComponent<MobileForge.UIComponents.Pages.LevelSelectPage>();
        SetField(page, "_backButton", backBtn);
        SetField(page, "_titleLabel", titleLabel);
        SetField(page, "_staminaLabel", staminaLabel);

        SavePrefab(root, "LevelSelectPage");
    }

    // ─── Team Select Page ───────────────────────────────────────────────

    [MenuItem("MobileForge/Generate Page Prefabs/TeamSelectPage")]
    public static void GenerateTeamSelectPage()
    {
        var root = CreatePageRoot("TeamSelectPage");
        AddBg(root, DarkBg);

        var header = BuildStandardHeader(root.transform, "Select Team", Color.white,
            out var backBtn, out var titleLabel);

        var statsLabel = CreateLabel(root.transform, "StatsLabel",
            "Select monsters for your team", 12, MutedText, TextAnchor.MiddleCenter);
        AnchorRegion(statsLabel, 0.02f, 0f, 0.98f, 0f, 0, 140, 0, 160);

        var page = root.AddComponent<MobileForge.UIComponents.Pages.TeamSelectPage>();
        SetField(page, "_backButton", backBtn);
        SetField(page, "_titleLabel", titleLabel);
        SetField(page, "_statsLabel", statsLabel);

        SavePrefab(root, "TeamSelectPage");
    }

    // ─── Result Page ────────────────────────────────────────────────────

    [MenuItem("MobileForge/Generate Page Prefabs/ResultPage")]
    public static void GenerateResultPage()
    {
        var root = CreatePageRoot("ResultPage");
        AddBg(root, DarkBg);

        var resultLabel = CreateLabel(root.transform, "ResultLabel",
            "VICTORY!", 40, GoldText, TextAnchor.MiddleCenter);
        AnchorRegion(resultLabel, 0.05f, 0.7f, 0.95f, 0.9f, 0, 0, 0, 0);
        AddShadow(resultLabel.gameObject);

        // Rewards area
        var rewardsArea = CreateContainer(root.transform, "RewardsArea");
        AnchorFill(rewardsArea, 30, 200, 30, 120);

        var rewardsContent = CreateContainer(rewardsArea.transform, "RewardsContent");
        StretchFill(rewardsContent);

        var page = root.AddComponent<MobileForge.UIComponents.Pages.ResultPage>();
        SetField(page, "_resultLabel", resultLabel);
        SetField(page, "_rewardsArea", rewardsArea.GetComponent<RectTransform>());
        SetField(page, "_rewardsContent", rewardsContent.transform);

        SavePrefab(root, "ResultPage");
    }

    // ─── Gacha Page ─────────────────────────────────────────────────────

    [MenuItem("MobileForge/Generate Page Prefabs/GachaPage")]
    public static void GenerateGachaPage()
    {
        var root = CreatePageRoot("GachaPage");
        AddBg(root, DarkBg);

        var header = BuildStandardHeader(root.transform, "Gacha", GoldText,
            out var backBtn, out var titleLabel);

        var statusLabel = CreateLabel(root.transform, "StatusLabel",
            "", 14, GoldText, TextAnchor.MiddleCenter);
        AnchorRegion(statusLabel, 0.05f, 0.19f, 0.95f, 0.23f, 0, 0, 0, 0);

        // Pool container
        var poolContainer = CreateContainer(root.transform, "PoolContainer");
        AnchorFill(poolContainer, 8, 56, 8, 200);

        var page = root.AddComponent<MobileForge.UIComponents.Pages.GachaPage>();
        SetField(page, "_backButton", backBtn);
        SetField(page, "_titleLabel", titleLabel);
        SetField(page, "_statusLabel", statusLabel);
        SetField(page, "_poolContainer", poolContainer.transform);

        SavePrefab(root, "GachaPage");
    }

    // ─── Inventory Page ─────────────────────────────────────────────────

    [MenuItem("MobileForge/Generate Page Prefabs/InventoryPage")]
    public static void GenerateInventoryPage()
    {
        var root = CreatePageRoot("InventoryPage");
        AddBg(root, DarkBg);

        var header = BuildStandardHeader(root.transform, "Monster Box", new Color(0.251f, 1f, 0.243f),
            out var backBtn, out var titleLabel);

        // Detail panel
        var detailPanel = CreatePanel(root.transform, "DetailPanel", 0, 210, PanelBg);
        var dpRect = detailPanel.GetComponent<RectTransform>();
        dpRect.anchorMin = Vector2.zero;
        dpRect.anchorMax = new Vector2(1, 0);
        dpRect.pivot = new Vector2(0.5f, 0);
        dpRect.offsetMin = new Vector2(6, 6);
        dpRect.offsetMax = new Vector2(-6, 216);

        var detailName = CreateLabel(detailPanel.transform, "DetailName",
            "Tap a monster to see details", 18, Color.white, TextAnchor.MiddleLeft);
        AnchorRegion(detailName, 0.02f, 0.85f, 0.98f, 1f, 10, 0, 10, 0);

        var detailStats = CreateLabel(detailPanel.transform, "DetailStats",
            "", 14, Color.white, TextAnchor.MiddleLeft);
        AnchorRegion(detailStats, 0.02f, 0.72f, 0.98f, 0.85f, 10, 0, 10, 0);

        var detailInfo = CreateLabel(detailPanel.transform, "DetailInfo",
            "", 12, MutedText, TextAnchor.MiddleLeft);
        AnchorRegion(detailInfo, 0.02f, 0.55f, 0.98f, 0.72f, 10, 0, 10, 0);

        var statusLabel = CreateLabel(detailPanel.transform, "StatusLabel",
            "", 12, GoldText, TextAnchor.MiddleCenter);
        AnchorRegion(statusLabel, 0.02f, 0f, 0.98f, 0.12f, 0, 0, 0, 0);

        var page = root.AddComponent<MobileForge.UIComponents.Pages.InventoryPage>();
        SetField(page, "_backButton", backBtn);
        SetField(page, "_titleLabel", titleLabel);
        SetField(page, "_detailName", detailName);
        SetField(page, "_detailStats", detailStats);
        SetField(page, "_detailInfo", detailInfo);
        SetField(page, "_statusLabel", statusLabel);

        SavePrefab(root, "InventoryPage");
    }

    // ─── Shop Page ──────────────────────────────────────────────────────

    [MenuItem("MobileForge/Generate Page Prefabs/ShopPage")]
    public static void GenerateShopPage()
    {
        var root = CreatePageRoot("ShopPage");
        AddBg(root, DarkBg);

        var header = BuildStandardHeader(root.transform, "Shop", new Color(0.7f, 0.5f, 1f),
            out var backBtn, out var titleLabel);

        // Currency chips
        var currBar = CreatePanel(root.transform, "CurrencyBar", 56, 24,
            new Color(0, 0, 0, 0)); // transparent
        AnchorTop(currBar.GetComponent<RectTransform>(), 56, 24);

        var gemsLabel = CreateLabel(currBar.transform, "GemsLabel",
            "Gems: 0", 12, GoldText, TextAnchor.MiddleCenter);
        AnchorRegion(gemsLabel, 0f, 0f, 0.33f, 1f, 8, 0, 0, 0);

        var coinsLabel = CreateLabel(currBar.transform, "CoinsLabel",
            "Coins: 0", 12, new Color(0.9f, 0.8f, 0.3f), TextAnchor.MiddleCenter);
        AnchorRegion(coinsLabel, 0.33f, 0f, 0.66f, 1f, 0, 0, 0, 0);

        var staminaLabel = CreateLabel(currBar.transform, "StaminaLabel",
            "ST: 0", 12, new Color(0.3f, 0.9f, 0.3f), TextAnchor.MiddleCenter);
        AnchorRegion(staminaLabel, 0.66f, 0f, 1f, 1f, 0, 0, 8, 0);

        var statusLabel = CreateLabel(root.transform, "StatusLabel",
            "", 14, GoldText, TextAnchor.MiddleCenter);
        AnchorRegion(statusLabel, 0.05f, 0.05f, 0.95f, 0.1f, 0, 0, 0, 0);

        var page = root.AddComponent<MobileForge.UIComponents.Pages.ShopPage>();
        SetField(page, "_backButton", backBtn);
        SetField(page, "_titleLabel", titleLabel);
        SetField(page, "_gemsLabel", gemsLabel);
        SetField(page, "_coinsLabel", coinsLabel);
        SetField(page, "_staminaLabel", staminaLabel);
        SetField(page, "_statusLabel", statusLabel);

        SavePrefab(root, "ShopPage");
    }

    // ─── World Map Page ─────────────────────────────────────────────────

    [MenuItem("MobileForge/Generate Page Prefabs/WorldMapPage")]
    public static void GenerateWorldMapPage()
    {
        var root = CreatePageRoot("WorldMapPage");
        AddBg(root, DarkBg);

        var header = BuildStandardHeader(root.transform, "World Map",
            new Color(0.251f, 1f, 1f), out var backBtn, out var titleLabel);

        var scrollContent = CreateContainer(root.transform, "ScrollContent");
        AnchorFill(scrollContent, 8, 56, 8, 62);

        var page = root.AddComponent<MobileForge.UIComponents.Pages.WorldMapPage>();
        SetField(page, "_backButton", backBtn);
        SetField(page, "_titleLabel", titleLabel);
        SetField(page, "_scrollContent", scrollContent.transform);

        SavePrefab(root, "WorldMapPage");
    }

    // ─── Social Page ────────────────────────────────────────────────────

    [MenuItem("MobileForge/Generate Page Prefabs/SocialPage")]
    public static void GenerateSocialPage()
    {
        var root = CreatePageRoot("SocialPage");
        AddBg(root, DarkBg);

        var header = BuildStandardHeader(root.transform, "Social",
            new Color(0.4f, 0.8f, 1f), out var backBtn, out var titleLabel);

        // Player info card
        var playerInfo = CreatePanel(root.transform, "PlayerInfoCard", 56, 64,
            new Color(0.08f, 0.1f, 0.16f, 0.95f));
        AnchorTop(playerInfo.GetComponent<RectTransform>(), 56, 64);

        var playerName = CreateLabel(playerInfo.transform, "PlayerNameLabel",
            "Player", 18, Color.white, TextAnchor.MiddleLeft);
        AnchorRegion(playerName, 0.02f, 0.4f, 0.98f, 1f, 14, 0, 14, 0);

        var playerId = CreateLabel(playerInfo.transform, "PlayerIdLabel",
            "ID: 000000  |  Rank: 1", 12, MutedText, TextAnchor.MiddleLeft);
        AnchorRegion(playerId, 0.02f, 0f, 0.98f, 0.4f, 14, 0, 14, 0);

        // Menu scroll content
        var scrollContent = CreateContainer(root.transform, "ScrollContent");
        AnchorFill(scrollContent, 8, 126, 8, 62);

        var page = root.AddComponent<MobileForge.UIComponents.Pages.SocialPage>();
        SetField(page, "_backButton", backBtn);
        SetField(page, "_titleLabel", titleLabel);
        SetField(page, "_playerInfoCard", playerInfo.GetComponent<RectTransform>());
        SetField(page, "_playerNameLabel", playerName);
        SetField(page, "_playerIdLabel", playerId);
        SetField(page, "_scrollContent", scrollContent.transform);

        SavePrefab(root, "SocialPage");
    }

    // ─── Settings Page ──────────────────────────────────────────────────

    [MenuItem("MobileForge/Generate Page Prefabs/SettingsPage")]
    public static void GenerateSettingsPage()
    {
        var root = CreatePageRoot("SettingsPage");
        AddBg(root, DarkBg);

        var header = BuildStandardHeader(root.transform, "Settings",
            new Color(0.7f, 0.7f, 0.8f), out var backBtn, out var titleLabel);

        var playerName = CreateLabel(root.transform, "PlayerNameLabel",
            "Player", 18, Color.white, TextAnchor.MiddleLeft);
        AnchorRegion(playerName, 0.02f, 0f, 0.98f, 0f, 14, 56, 14, 100);

        var scrollContent = CreateContainer(root.transform, "ScrollContent");
        AnchorFill(scrollContent, 8, 56, 8, 62);

        var page = root.AddComponent<MobileForge.UIComponents.Pages.SettingsPage>();
        SetField(page, "_backButton", backBtn);
        SetField(page, "_titleLabel", titleLabel);
        SetField(page, "_scrollContent", scrollContent.transform);
        SetField(page, "_playerNameLabel", playerName);

        SavePrefab(root, "SettingsPage");
    }

    // ─── Daily Check-In Page ────────────────────────────────────────────

    [MenuItem("MobileForge/Generate Page Prefabs/DailyCheckInPage")]
    public static void GenerateDailyCheckInPage()
    {
        var root = CreatePageRoot("DailyCheckInPage");
        AddBg(root, DarkBg);

        var header = BuildStandardHeader(root.transform, "Daily Check-In", GoldText,
            out var backBtn, out var titleLabel);

        var streakLabel = CreateLabel(root.transform, "StreakLabel",
            "Login Streak: 0 days", 18, GoldText, TextAnchor.MiddleCenter);
        AnchorRegion(streakLabel, 0.05f, 0f, 0.95f, 0f, 0, 60, 0, 90);

        var statusLabel = CreateLabel(root.transform, "StatusLabel",
            "", 14, GoldText, TextAnchor.MiddleCenter);
        AnchorRegion(statusLabel, 0.1f, 0.07f, 0.9f, 0.1f, 0, 0, 0, 0);

        var page = root.AddComponent<MobileForge.UIComponents.Pages.DailyCheckInPage>();
        SetField(page, "_backButton", backBtn);
        SetField(page, "_titleLabel", titleLabel);
        SetField(page, "_streakLabel", streakLabel);
        SetField(page, "_statusLabel", statusLabel);

        SavePrefab(root, "DailyCheckInPage");
    }

    // ─── Mail Page ──────────────────────────────────────────────────────

    [MenuItem("MobileForge/Generate Page Prefabs/MailPage")]
    public static void GenerateMailPage()
    {
        var root = CreatePageRoot("MailPage");
        AddBg(root, DarkBg);

        var header = BuildStandardHeader(root.transform, "Mail",
            new Color(0.4f, 0.7f, 1f), out var backBtn, out var titleLabel);

        var countLabel = CreateLabel(header.transform, "CountLabel",
            "0 new", 12, MutedText, TextAnchor.MiddleRight);
        AnchorRegion(countLabel, 0.7f, 0f, 0.98f, 1f, 0, 0, 8, 0);

        var mailListContent = CreateContainer(root.transform, "MailListContent");
        AnchorFill(mailListContent, 8, 98, 8, 70);

        var statusLabel = CreateLabel(root.transform, "StatusLabel",
            "", 14, GoldText, TextAnchor.MiddleCenter);
        AnchorRegion(statusLabel, 0.1f, 0.06f, 0.9f, 0.09f, 0, 0, 0, 0);

        var page = root.AddComponent<MobileForge.UIComponents.Pages.MailPage>();
        SetField(page, "_backButton", backBtn);
        SetField(page, "_titleLabel", titleLabel);
        SetField(page, "_mailListContent", mailListContent.transform);
        SetField(page, "_statusLabel", statusLabel);
        SetField(page, "_countLabel", countLabel);

        SavePrefab(root, "MailPage");
    }

    // ─── Team Manage Page ───────────────────────────────────────────────

    [MenuItem("MobileForge/Generate Page Prefabs/TeamManagePage")]
    public static void GenerateTeamManagePage()
    {
        var root = CreatePageRoot("TeamManagePage");
        AddBg(root, DarkBg);

        var header = BuildStandardHeader(root.transform, "Team Management",
            new Color(0.3f, 0.8f, 0.6f), out var backBtn, out var titleLabel);

        var teamNameLabel = CreateLabel(root.transform, "TeamNameLabel",
            "Team 1", 14, Color.white, TextAnchor.MiddleLeft);
        AnchorRegion(teamNameLabel, 0.02f, 0f, 0.7f, 0f, 12, 92, 0, 120);

        var statusLabel = CreateLabel(root.transform, "StatusLabel",
            "", 14, GoldText, TextAnchor.MiddleCenter);
        AnchorRegion(statusLabel, 0.05f, 0.06f, 0.95f, 0.09f, 0, 0, 0, 0);

        var page = root.AddComponent<MobileForge.UIComponents.Pages.TeamManagePage>();
        SetField(page, "_backButton", backBtn);
        SetField(page, "_titleLabel", titleLabel);
        SetField(page, "_teamNameLabel", teamNameLabel);
        SetField(page, "_statusLabel", statusLabel);

        SavePrefab(root, "TeamManagePage");
    }

    // ─── Placeholder Page ───────────────────────────────────────────────

    [MenuItem("MobileForge/Generate Page Prefabs/PlaceholderPage")]
    public static void GeneratePlaceholderPage()
    {
        var root = CreatePageRoot("PlaceholderPage");
        AddBg(root, DarkBg);

        var header = BuildStandardHeader(root.transform, "Coming Soon", MutedText,
            out var backBtn, out var titleLabel);

        // Content area
        var contentArea = CreateContainer(root.transform, "ContentArea");
        AnchorFill(contentArea, 0, 56, 0, 62);

        var iconText = CreateLabel(contentArea.transform, "IconText",
            "\u2026", 48, new Color(0.4f, 0.4f, 0.5f), TextAnchor.MiddleCenter);
        AnchorRegion(iconText, 0.2f, 0.45f, 0.8f, 0.7f, 0, 0, 0, 0);

        var screenLabel = CreateLabel(contentArea.transform, "ScreenLabel",
            "Unknown", 36, MutedText, TextAnchor.MiddleCenter);
        AnchorRegion(screenLabel, 0.1f, 0.3f, 0.9f, 0.45f, 0, 0, 0, 0);
        AddShadow(screenLabel.gameObject);

        var subtitleText = CreateLabel(contentArea.transform, "SubtitleText",
            "Coming Soon", 18, GoldText, TextAnchor.MiddleCenter);
        AnchorRegion(subtitleText, 0.1f, 0.22f, 0.9f, 0.3f, 0, 0, 0, 0);

        var page = root.AddComponent<MobileForge.UIComponents.Pages.PlaceholderPage>();
        SetField(page, "_backButton", backBtn);
        SetField(page, "_titleLabel", titleLabel);
        SetField(page, "_contentArea", contentArea.GetComponent<RectTransform>());
        SetField(page, "_iconText", iconText);
        SetField(page, "_screenLabel", screenLabel);
        SetField(page, "_subtitleText", subtitleText);

        SavePrefab(root, "PlaceholderPage");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Helper methods
    // ═══════════════════════════════════════════════════════════════════

    private static readonly Color DarkBg = new(0.05f, 0.05f, 0.08f);
    private static readonly Color PanelBg = new(0.1f, 0.1f, 0.14f, 0.92f);
    private static readonly Color GoldText = new(1f, 0.85f, 0f);
    private static readonly Color MutedText = new(0.6f, 0.6f, 0.6f);
    private static readonly Color HeaderBg = new(0.08f, 0.08f, 0.12f, 0.96f);
    private static readonly Color BackBtnBg = new(0.18f, 0.18f, 0.24f, 0.95f);

    private static void EnsureDirectory()
    {
        if (!Directory.Exists(OutputDir))
        {
            Directory.CreateDirectory(OutputDir);
            AssetDatabase.Refresh();
        }
    }

    private static GameObject CreatePageRoot(string name)
    {
        var go = new GameObject(name);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go;
    }

    private static Image AddBg(GameObject go, Color color)
    {
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static Text CreateLabel(Transform parent, string name, string text,
        int fontSize, Color color, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.font = _font;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = alignment;
        return txt;
    }

    private static GameObject CreatePanel(Transform parent, string name,
        float topOffset, float height, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1);
        rect.offsetMin = new Vector2(0, -topOffset - height);
        rect.offsetMax = new Vector2(0, -topOffset);
        go.AddComponent<Image>().color = color;
        return go;
    }

    private static GameObject CreateContainer(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go;
    }

    private static RectTransform BuildStandardHeader(Transform parent, string title,
        Color titleColor, out Button backBtn, out Text titleLabel)
    {
        var headerGo = new GameObject("Header");
        headerGo.transform.SetParent(parent, false);
        var headerRect = headerGo.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = Vector2.one;
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.offsetMin = new Vector2(0, -50);
        headerRect.offsetMax = Vector2.zero;
        headerGo.AddComponent<Image>().color = HeaderBg;

        // Back button
        var backGo = new GameObject("BackButton");
        backGo.transform.SetParent(headerGo.transform, false);
        var backRect = backGo.AddComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0, 0);
        backRect.anchorMax = new Vector2(0, 1);
        backRect.pivot = new Vector2(0, 0.5f);
        backRect.offsetMin = new Vector2(6, 7);
        backRect.offsetMax = new Vector2(76, -7);
        backGo.AddComponent<Image>().color = BackBtnBg;
        backBtn = backGo.AddComponent<Button>();

        var backLabel = CreateLabel(backGo.transform, "Label", "\u25C0 Back",
            14, new Color(0.8f, 0.8f, 0.85f), TextAnchor.MiddleCenter);
        StretchFill(backLabel.gameObject);

        // Title
        titleLabel = CreateLabel(headerGo.transform, "Title", title,
            24, titleColor, TextAnchor.MiddleCenter);
        AnchorRegion(titleLabel, 0.15f, 0f, 0.85f, 1f, 0, 0, 0, 0);

        return headerRect;
    }

    private static void AnchorTop(RectTransform rect, float topOffset, float height)
    {
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1);
        rect.offsetMin = new Vector2(0, -topOffset - height);
        rect.offsetMax = new Vector2(0, -topOffset);
    }

    private static void AnchorTop(Text label, float topOffset, float height)
    {
        var rect = label.GetComponent<RectTransform>();
        AnchorTop(rect, topOffset, height);
    }

    private static void AnchorFill(GameObject go, float left, float top, float right, float bottom)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void AnchorRegion(Text label,
        float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY,
        float padLeft, float padBottom, float padRight, float padTop)
    {
        var rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(anchorMinX, anchorMinY);
        rect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        rect.offsetMin = new Vector2(padLeft, padBottom);
        rect.offsetMax = new Vector2(-padRight, -padTop);
    }

    private static void StretchFill(GameObject go)
    {
        var rect = go.GetComponent<RectTransform>();
        if (rect == null) rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AddShadow(GameObject go)
    {
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.6f);
        shadow.effectDistance = new Vector2(2, -2);
    }

    private static void SetField(Component component, string fieldName, object value)
    {
        var so = new SerializedObject(component);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value as Object;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning($"[PrefabGenerator] Field '{fieldName}' not found on {component.GetType().Name}");
        }
    }

    private static void SavePrefab(GameObject go, string name)
    {
        string path = $"{OutputDir}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[PrefabGenerator] Saved {path}");
    }
}

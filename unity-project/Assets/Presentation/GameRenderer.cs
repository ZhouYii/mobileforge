using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MobileForge.Domain;
using MobileForge.Presentation;
using TowerOfSaviors;

/// <summary>
/// Subscribes to UIRouter.OnNavigated and creates Unity UI for each screen.
/// Each button gets a name attribute for Playwright identification.
/// Board gems are colored Image components in a GridLayoutGroup.
/// </summary>
public class GameRenderer : MonoBehaviour
{
    private GameBootstrap _bootstrap;
    private Transform _screenRoot;
    private GameObject _currentScreen;

    // Element ID → color mapping (matches ToS: 1=water, 2=fire, 3=grass, 4=light, 5=dark, 6=heart)
    private static readonly Dictionary<int, Color> ElementColors = new()
    {
        { 1, new Color(0.2f, 0.5f, 1f) },    // Water — blue
        { 2, new Color(1f, 0.3f, 0.2f) },     // Fire — red
        { 3, new Color(0.2f, 0.8f, 0.3f) },   // Grass — green
        { 4, new Color(1f, 0.9f, 0.3f) },     // Light — yellow
        { 5, new Color(0.6f, 0.2f, 0.8f) },   // Dark — purple
        { 6, new Color(1f, 0.5f, 0.7f) },     // Heart — pink
    };

    void Start()
    {
        _bootstrap = FindFirstObjectByType<GameBootstrap>();
        if (_bootstrap == null)
        {
            Debug.LogError("[GameRenderer] GameBootstrap not found!");
            return;
        }

        // Create screen root container under the Canvas
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            var rootGO = new GameObject("ScreenRoot");
            rootGO.transform.SetParent(canvas.transform, false);
            var rootRect = rootGO.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            _screenRoot = rootGO.transform;
        }

        // Subscribe to navigation events
        _bootstrap.Game.Router.OnNavigated += OnScreenChanged;
    }

    void OnDestroy()
    {
        if (_bootstrap != null && _bootstrap.Game != null)
            _bootstrap.Game.Router.OnNavigated -= OnScreenChanged;
    }

    private void OnScreenChanged(string screenId, Dictionary<string, object> parameters)
    {
        Debug.Log($"[GameRenderer] Navigated to: {screenId}");

        // Destroy previous screen UI
        if (_currentScreen != null)
            Destroy(_currentScreen);

        // Get the current IScreen from the router
        var router = _bootstrap.Game.Router;
        var screen = router.GetScreenAt(router.StackDepth - 1);

        switch (screenId)
        {
            case "title":
                RenderTitle(screen as TitleScreen);
                break;
            case "dungeon_select":
                RenderDungeonSelect(screen as DungeonSelectScreen);
                break;
            case "team_select":
                RenderTeamSelect(screen as TeamSelectScreen);
                break;
            case "battle":
                RenderBattle(screen as BattleScreen);
                break;
            case "result":
                RenderResult(screen as ResultScreen);
                break;
            default:
                RenderPlaceholder(screenId);
                break;
        }

        // Notify JS bridge
        NotifyBridge(screenId);
    }

    // ── Title Screen ─────────────────────────────────────────────

    private void RenderTitle(TitleScreen screen)
    {
        if (screen == null) return;
        var root = CreateScreenContainer("TitleScreen");

        // Title label
        CreateLabel(root.transform, "TitleLabel", screen.Title,
            new Vector2(0.5f, 0.85f), 48, Color.white);

        // Navigation buttons
        float startY = 0.6f;
        foreach (var entry in screen.NavEntries)
        {
            string sid = entry.ScreenId;
            CreateButton(root.transform, $"btn_{sid}", entry.Label,
                new Vector2(0.5f, startY), () => screen.OnNavSelected(sid));
            startY -= 0.12f;
        }
    }

    // ── Dungeon Select Screen ────────────────────────────────────

    private void RenderDungeonSelect(DungeonSelectScreen screen)
    {
        if (screen == null) return;
        var root = CreateScreenContainer("DungeonSelectScreen");

        CreateLabel(root.transform, "HeaderLabel", "Dungeon Select",
            new Vector2(0.5f, 0.92f), 36, Color.white);

        // Back button
        CreateButton(root.transform, "btn_back", "Back",
            new Vector2(0.15f, 0.92f), () => _bootstrap.Game.Router.Pop(),
            width: 120, height: 50, fontSize: 20);

        // Stage list (scrollable area)
        float y = 0.8f;
        int index = 0;
        foreach (var stage in screen.Stages)
        {
            if (index >= 8) break; // Show first 8 stages
            int stageId = stage.StageId;
            int cost = stage.StaminaCost;
            string label = $"{stage.Name} ({cost} ST)";
            Color textColor = stage.CanAfford ? Color.white : Color.gray;

            CreateButton(root.transform, $"btn_stage_{stageId}", label,
                new Vector2(0.5f, y),
                () => screen.SelectStage(stageId, cost),
                width: 400, height: 50, fontSize: 20, textColor: textColor);
            y -= 0.08f;
            index++;
        }
    }

    // ── Team Select Screen ───────────────────────────────────────

    private void RenderTeamSelect(TeamSelectScreen screen)
    {
        if (screen == null) return;
        var root = CreateScreenContainer("TeamSelectScreen");

        CreateLabel(root.transform, "HeaderLabel", "Select Team",
            new Vector2(0.5f, 0.92f), 36, Color.white);

        // 5 team slots at top
        for (int i = 0; i < TeamSelectScreen.MaxTeamSize; i++)
        {
            float x = 0.15f + i * 0.175f;
            var data = screen.GetSlotData(i);
            string slotLabel = data != null && data.ContainsKey("name") ? (string)data["name"] : $"Slot {i + 1}";
            CreateButton(root.transform, $"btn_slot_{i}", slotLabel,
                new Vector2(x, 0.82f), null, width: 80, height: 60, fontSize: 14);
        }

        // Monster grid (first 10 monsters from data as selectable)
        var allMonsters = _bootstrap.Game.GameData.GetAllDefinitions("monsters");
        float gridY = 0.65f;
        int col = 0;
        int shown = 0;
        foreach (var def in allMonsters)
        {
            if (shown >= 10) break;
            int monsterId = def.Id;
            string name = def.GetString("name", $"Mon#{monsterId}");
            float gx = 0.12f + col * 0.19f;
            CreateButton(root.transform, $"btn_monster_{monsterId}", name,
                new Vector2(gx, gridY), () =>
                {
                    screen.SelectMonster(monsterId);
                    // Re-render to update slots
                    OnScreenChanged("team_select", null);
                }, width: 90, height: 50, fontSize: 12);
            col++;
            if (col >= 5) { col = 0; gridY -= 0.08f; }
            shown++;
        }

        // Enter Dungeon button
        Color enterColor = screen.CanStart ? new Color(0.2f, 0.7f, 0.3f) : Color.gray;
        CreateButton(root.transform, "btn_enter_dungeon", "Enter Dungeon",
            new Vector2(0.5f, 0.1f),
            () => { if (screen.CanStart) screen.OnStartBattle(); },
            width: 300, height: 60, bgColor: enterColor);

        // Back button
        CreateButton(root.transform, "btn_back", "Back",
            new Vector2(0.15f, 0.92f), () => screen.OnBack(),
            width: 120, height: 50, fontSize: 20);
    }

    // ── Battle Screen ────────────────────────────────────────────

    private void RenderBattle(BattleScreen screen)
    {
        if (screen == null) return;
        var root = CreateScreenContainer("BattleScreen");

        // Enemy HP bar area
        string enemyInfo = "Enemy";
        if (screen.State != null)
        {
            int enemyHp = 0;
            if (screen.State.Enemies != null)
                foreach (var e in screen.State.Enemies)
                    if (e.IsAlive) enemyHp += e.Hp;
            enemyInfo = $"Wave {screen.State.CurrentWaveIndex + 1} — HP: {enemyHp}";
        }
        CreateLabel(root.transform, "EnemyInfo", enemyInfo,
            new Vector2(0.5f, 0.92f), 24, Color.red);

        // Team HP
        string teamInfo = screen.State != null ? $"Team HP: {screen.State.TeamHp}" : "Team HP: ---";
        CreateLabel(root.transform, "TeamInfo", teamInfo,
            new Vector2(0.5f, 0.85f), 22, Color.green);

        // 5x6 Gem Board
        RenderGemBoard(root.transform, screen);

        // Resolve Turn button
        CreateButton(root.transform, "btn_resolve", "Resolve Turn",
            new Vector2(0.5f, 0.12f),
            () =>
            {
                screen.ResolveTurn();
                // Re-render battle after turn
                var currentScreen = _bootstrap.Game.Router.CurrentScreenId;
                if (currentScreen == "battle")
                    OnScreenChanged("battle", null);
            },
            width: 250, height: 55, bgColor: new Color(0.8f, 0.3f, 0.1f));
    }

    private void RenderGemBoard(Transform parent, BattleScreen screen)
    {
        int rows = 5, cols = 6;
        int[] elements = screen.GetBoardElements();

        var boardGO = new GameObject("GemBoard");
        boardGO.transform.SetParent(parent, false);
        var boardRect = boardGO.AddComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0.05f, 0.25f);
        boardRect.anchorMax = new Vector2(0.95f, 0.78f);
        boardRect.offsetMin = Vector2.zero;
        boardRect.offsetMax = Vector2.zero;

        var grid = boardGO.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        grid.cellSize = new Vector2(70, 70);
        grid.spacing = new Vector2(4, 4);
        grid.childAlignment = TextAnchor.MiddleCenter;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                int elem = idx < elements.Length ? elements[idx] : 0;

                var gemGO = new GameObject($"gem_{r}_{c}");
                gemGO.transform.SetParent(boardGO.transform, false);
                var img = gemGO.AddComponent<Image>();
                img.color = ElementColors.GetValueOrDefault(elem, Color.gray);

                // Round the gems slightly
                img.type = Image.Type.Simple;
            }
        }
    }

    // ── Result Screen ────────────────────────────────────────────

    private void RenderResult(ResultScreen screen)
    {
        if (screen == null) return;
        var root = CreateScreenContainer("ResultScreen");

        Color resultColor = screen.Won ? Color.yellow : Color.red;
        CreateLabel(root.transform, "ResultLabel", screen.ResultText,
            new Vector2(0.5f, 0.75f), 52, resultColor);

        // Rewards
        float rewardY = 0.55f;
        foreach (var reward in screen.Rewards)
        {
            CreateLabel(root.transform, $"reward_{reward.Type}",
                $"{reward.Type}: +{reward.Count}",
                new Vector2(0.5f, rewardY), 24, Color.white);
            rewardY -= 0.06f;
        }

        // Continue button
        CreateButton(root.transform, "btn_continue", "Continue",
            new Vector2(0.5f, 0.2f),
            () => screen.OnContinue(_bootstrap.Game.Router),
            width: 250, height: 60, bgColor: new Color(0.2f, 0.6f, 0.9f));
    }

    // ── Placeholder for unimplemented screens ────────────────────

    private void RenderPlaceholder(string screenId)
    {
        var root = CreateScreenContainer(screenId);
        CreateLabel(root.transform, "PlaceholderLabel", screenId.Replace("_", " ").ToUpper(),
            new Vector2(0.5f, 0.5f), 36, Color.white);
        CreateButton(root.transform, "btn_back", "Back",
            new Vector2(0.5f, 0.3f), () => _bootstrap.Game.Router.Pop());
    }

    // ── UI Helpers ───────────────────────────────────────────────

    private GameObject CreateScreenContainer(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_screenRoot, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Dark background
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

        _currentScreen = go;
        return go;
    }

    private void CreateLabel(Transform parent, string name, string text,
        Vector2 anchorPos, int fontSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchorPos;
        rect.sizeDelta = new Vector2(500, fontSize + 20);

        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
    }

    private void CreateButton(Transform parent, string name, string label,
        Vector2 anchorPos, Action onClick,
        float width = 300, float height = 60, int fontSize = 24,
        Color? bgColor = null, Color? textColor = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchorPos;
        rect.sizeDelta = new Vector2(width, height);

        var bg = go.AddComponent<Image>();
        bg.color = bgColor ?? new Color(0.25f, 0.25f, 0.35f, 1f);

        var btn = go.AddComponent<Button>();
        if (onClick != null)
            btn.onClick.AddListener(() => onClick());

        // Button label
        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var txt = textGO.AddComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = fontSize;
        txt.color = textColor ?? Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
    }

    // ── JS Bridge Notification ───────────────────────────────────

    private void NotifyBridge(string screenId)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        AutomationBridge.NotifyScreenChanged(screenId);
#endif
    }
}

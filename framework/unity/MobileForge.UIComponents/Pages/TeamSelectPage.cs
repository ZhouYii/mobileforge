using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Team selection page. Shows 5 team slots with element-tinted cards,
    /// team stats summary, monster grid with dimming for selected monsters,
    /// and an "Enter Dungeon" button. Matches Godot team_select_screen.gd.
    /// </summary>
    public class TeamSelectPage : PageBase<TeamSelectScreen>
    {
        [Header("Prefab References")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _statsLabel;

        private MFScrollGrid _monsterGrid;
        private readonly List<MFButton> _slotButtons = new();
        private MFButton _enterBtn;
        private MobileForge.Infrastructure.GameData _gameData;
        private Text _headerLabel;
        [SerializeField] private RectTransform _slotArea;
        [SerializeField] private RectTransform _gridArea;
        [SerializeField] private RectTransform _bottomBar;

        public void SetGameData(MobileForge.Infrastructure.GameData gameData) => _gameData = gameData;

        protected override void OnBind(TeamSelectScreen screen)
        {
            if (!IsPrefabPage)
            {
                BuildSkeleton(screen);
            }
            else
            {
                if (_backButton != null)
                    _backButton.onClick.AddListener(() => screen.OnBack());
                if (_titleLabel != null)
                    _titleLabel.text = $"Select Team \u2014 Stage {screen.StageId}";
                if (_statsLabel != null)
                    _statsLabel.text = "Select monsters for your team";
            }

            InitContent(screen);
        }

        private void BuildSkeleton(TeamSelectScreen screen)
        {
            gameObject.AddComponent<Image>().color = TosTheme.BgDark;

            // ── Standardized Header ──
            var h = TosPageHelper.BuildHeader(this, transform,
                $"Select Team \u2014 Stage {screen.StageId}", Color.white,
                () => screen.OnBack());
            _headerLabel = h.TitleText;

            // ── Team Slots container ──
            _slotArea = CreateRegion("Slots", MFAnchor.Top,
                new MFPadding(TosTheme.ContentPadH, TosTheme.ContentTopOffset, TosTheme.ContentPadH, 0));
            _slotArea.sizeDelta = new Vector2(0, 80);

            // ── Team Stats ──
            var statsArea = CreateRegion("Stats", MFAnchor.Top, new MFPadding(10, 140, 10, 0));
            statsArea.sizeDelta = new Vector2(0, 20);
            var statsGo = new GameObject("StatsLabel");
            statsGo.transform.SetParent(statsArea, false);
            var statsRect = statsGo.AddComponent<RectTransform>();
            statsRect.anchorMin = Vector2.zero;
            statsRect.anchorMax = Vector2.one;
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;
            _statsLabel = statsGo.AddComponent<Text>();
            _statsLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statsLabel.fontSize = TosTheme.FontSmall;
            _statsLabel.color = TosTheme.TextMuted;
            _statsLabel.alignment = TextAnchor.MiddleCenter;

            // ── Friend Helper Section ──
            if (screen.AvailableHelpers.Count > 0)
            {
                var helperArea = CreateRegion("HelperArea", MFAnchor.Top, new MFPadding(8, 164, 8, 0));
                helperArea.sizeDelta = new Vector2(0, 70);

                var helperVBox = CreateVBox("HelperContent", helperArea, spacing: 2f);

                var helperTitleGo = new GameObject("HelperTitle");
                helperTitleGo.transform.SetParent(helperVBox, false);
                helperTitleGo.AddComponent<LayoutElement>().preferredHeight = 18;
                var helperTitleText = helperTitleGo.AddComponent<Text>();
                helperTitleText.text = "Choose Friend Helper";
                helperTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                helperTitleText.fontSize = TosTheme.FontSmall;
                helperTitleText.color = TosTheme.ElementColors[1]; // Blue
                helperTitleText.alignment = TextAnchor.MiddleCenter;

                var helperHBox = CreateHBox("HelperRow", helperVBox.GetComponent<RectTransform>(), spacing: 6f);

                foreach (var helper in screen.AvailableHelpers)
                {
                    int helperId = helper.MonsterId;
                    var hBtn = SpawnPrimitive<MFButton>(helperHBox);
                    hBtn.gameObject.name = $"btn_helper_{helperId}";
                    hBtn.SetLabel($"{TosTheme.ElementIcon(helper.Element)} {helper.Name}\nLv.{helper.Level}");
                    var hLe = hBtn.gameObject.GetComponent<LayoutElement>();
                    if (hLe != null) { hLe.preferredWidth = 90; hLe.flexibleWidth = 0; hLe.preferredHeight = 46; }

                    bool selected = screen.SelectedHelperId == helperId;
                    hBtn.SetColor(
                        selected ? TosTheme.ElementButtonBg(helper.Element) : new Color(0.12f, 0.12f, 0.16f),
                        selected ? TosTheme.ElementColor(helper.Element) : TosTheme.TextMuted);

                    hBtn.OnClick = () =>
                    {
                        screen.SelectHelper(helperId);
                        RefreshAll();
                    };
                }
            }

            // ── Grid Area container ──
            int gridTopOffset = screen.AvailableHelpers.Count > 0 ? 240 : 166;
            _gridArea = CreateRegion("GridArea", MFAnchor.Fill, new MFPadding(8, gridTopOffset, 8, 80));

            // ── Bottom Bar container ──
            _bottomBar = CreateRegion("BottomBar", MFAnchor.Bottom, new MFPadding(40, 0, 40, 10));
            _bottomBar.sizeDelta = new Vector2(0, 70);

            // Entrance animations
            var slotCg = _slotArea.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(MFUIAnim.FadeIn(slotCg, 0.35f));
            var gridCg = _gridArea.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(MFUIAnim.FadeIn(gridCg, 0.5f));
            var btnCg = _bottomBar.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(MFUIAnim.DelayedFadeIn(btnCg, 0.3f, 0.3f));
        }

        private void InitContent(TeamSelectScreen screen)
        {
            // ── Team Slot Buttons ──
            var slotParent = _slotArea != null ? _slotArea : CreateRegion("Slots", MFAnchor.Top,
                new MFPadding(TosTheme.ContentPadH, TosTheme.ContentTopOffset, TosTheme.ContentPadH, 0));
            if (_slotArea == null) slotParent.sizeDelta = new Vector2(0, 80);
            var slotHBox = CreateHBox("SlotRow", slotParent, spacing: 6f);

            for (int i = 0; i < TeamSelectScreen.MaxTeamSize; i++)
            {
                int slotIndex = i;
                var slotBtn = SpawnPrimitive<MFButton>(slotHBox);
                slotBtn.gameObject.name = $"btn_slot_{i}";
                var sl = slotBtn.gameObject.GetComponent<LayoutElement>();
                if (sl != null) { sl.preferredWidth = TosTheme.CardSize.x; sl.flexibleWidth = 0; sl.preferredHeight = TosTheme.CardSize.y; }
                slotBtn.OnClick = () =>
                {
                    screen.RemoveSlot(slotIndex);
                    RefreshAll();
                };
                _slotButtons.Add(slotBtn);
            }

            // ── Monster Grid ──
            var gridParent = _gridArea != null ? _gridArea : CreateRegion("GridArea", MFAnchor.Fill, new MFPadding(8, 166, 8, 80));
            _monsterGrid = SpawnPrimitive<MFScrollGrid>(gridParent);
            _monsterGrid.Setup(null, BindMonsterCell, columns: 5, cellSize: 90f);
            BuildMonsterList();

            // ── Enter Dungeon Button ──
            var bottomParent = _bottomBar != null ? _bottomBar : CreateRegion("BottomBar", MFAnchor.Bottom, new MFPadding(40, 0, 40, 10));
            if (_bottomBar == null) bottomParent.sizeDelta = new Vector2(0, 70);
            _enterBtn = SpawnPrimitive<MFButton>(bottomParent);
            _enterBtn.gameObject.name = "btn_enter_dungeon";
            _enterBtn.SetLabel("Enter Dungeon");
            _enterBtn.OnClick = () =>
            {
                if (_screen.CanStart) _screen.OnStartBattle();
            };

            RefreshAll();
        }

        private void BuildMonsterList()
        {
            var monsters = new List<object>();
            if (_gameData != null)
            {
                var allDefs = _gameData.GetAllDefinitions("monsters");
                int shown = 0;
                foreach (var def in allDefs)
                {
                    if (shown >= 30) break;
                    monsters.Add(new Dictionary<string, object>
                    {
                        { "id", def.Id },
                        { "name", def.GetString("name", $"Mon#{def.Id}") },
                        { "element", def.GetInt("element", 0) },
                        { "rarity", def.GetInt("rarity", 1) }
                    });
                    shown++;
                }
            }
            _monsterGrid.SetItems(monsters);
        }

        private void BindMonsterCell(GameObject cell, object data, int index)
        {
            var dict = data as Dictionary<string, object>;
            if (dict == null) return;

            int monsterId = System.Convert.ToInt32(dict["id"]);
            string name = (string)dict["name"];
            int element = dict.ContainsKey("element") ? System.Convert.ToInt32(dict["element"]) : 0;
            int rarity = dict.ContainsKey("rarity") ? System.Convert.ToInt32(dict["rarity"]) : 1;
            bool isSelected = _screen.GetSelectedTeam().Contains(monsterId);

            // Cell background with element tint
            var cellBg = cell.GetComponent<Image>();
            if (cellBg == null) cellBg = cell.AddComponent<Image>();
            cellBg.color = isSelected
                ? new Color(0.2f, 0.2f, 0.2f, 0.5f)
                : TosTheme.ElementPanelBg(element);

            // Dim if already selected
            var cg = cell.GetComponent<CanvasGroup>();
            if (cg == null) cg = cell.AddComponent<CanvasGroup>();
            cg.alpha = isSelected ? TosTheme.CardAlphaDisabled : 1f;

            // Update label
            var label = cell.GetComponentInChildren<Text>();
            if (label != null)
            {
                string stars = TosTheme.RarityStars(rarity);
                label.text = $"{TosTheme.ElementIcon(element)} {name}\n{stars}";
                label.fontSize = 10;
                label.color = isSelected ? TosTheme.TextMuted : TosTheme.ElementColor(element);
            }

            // Click to select
            var btn = cell.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                if (!isSelected)
                {
                    btn.onClick.AddListener(() =>
                    {
                        _screen.SelectMonster(monsterId);
                        RefreshAll();
                    });
                }
            }
        }

        private void RefreshAll()
        {
            RefreshSlots();
            RefreshStats();
            RefreshEnterButton();
            BuildMonsterList(); // Rebuild to update dimming
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < TeamSelectScreen.MaxTeamSize; i++)
            {
                var slotData = _screen.GetSlotData(i);
                if (i < _slotButtons.Count)
                {
                    if (slotData != null)
                    {
                        string name = slotData.ContainsKey("name") ? (string)slotData["name"] : $"Slot {i + 1}";
                        int element = slotData.ContainsKey("element") ? System.Convert.ToInt32(slotData["element"]) : 0;
                        int rarity = slotData.ContainsKey("rarity") ? System.Convert.ToInt32(slotData["rarity"]) : 1;
                        _slotButtons[i].SetLabel($"{TosTheme.ElementIcon(element)} {name}");
                        _slotButtons[i].SetColor(TosTheme.ElementButtonBg(element), TosTheme.ElementColor(element));
                    }
                    else
                    {
                        _slotButtons[i].SetLabel("+");
                        _slotButtons[i].SetColor(new Color(0.15f, 0.15f, 0.2f), TosTheme.TextMuted);
                    }
                }
            }
        }

        private void RefreshStats()
        {
            var team = _screen.GetSelectedTeam();
            if (team.Count == 0)
            {
                _statsLabel.text = "Select monsters for your team";
                return;
            }
            // Simple stat display (actual calculation would use MonsterManager)
            _statsLabel.text = $"Team: {team.Count}/{TeamSelectScreen.MaxTeamSize} members";
        }

        private void RefreshEnterButton()
        {
            _enterBtn.SetEnabled(_screen.CanStart);
            _enterBtn.SetColor(
                _screen.CanStart ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.3f, 0.3f, 0.3f),
                Color.white);
        }

        protected override void OnRefresh()
        {
            RefreshAll();
        }
    }
}

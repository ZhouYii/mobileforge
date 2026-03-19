using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Team management page matching original Team_Tab_Team_View.
    /// Supports multiple team presets with drag-to-assign and leader selection.
    /// </summary>
    public class TeamManagePage : PageBase<TeamManageScreen>
    {
        [Header("Prefab References")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _statusLabel;
        [SerializeField] private Text _teamNameLabel;

        private MFTabBar _teamTabs;
        private Transform _slotContainer;
        private MFScrollGrid _monsterGrid;
        [SerializeField] private RectTransform _tabArea;
        [SerializeField] private RectTransform _slotArea;
        [SerializeField] private RectTransform _gridArea;

        protected override void OnBind(TeamManageScreen screen)
        {
            if (!IsPrefabPage)
            {
                BuildSkeleton(screen);
            }
            else
            {
                if (_backButton != null)
                    _backButton.onClick.AddListener(() => screen.GoBack());
                if (_titleLabel != null)
                    _titleLabel.text = "Team Management";
                if (_statusLabel != null)
                    _statusLabel.text = "";
                if (_teamNameLabel != null)
                    _teamNameLabel.text = screen.ActiveTeam?.Name ?? "Team 1";
            }

            InitContent(screen);
        }

        private void BuildSkeleton(TeamManageScreen screen)
        {
            gameObject.AddComponent<Image>().color = TosTheme.BgDark;

            // ── Standardized Header ──
            TosPageHelper.BuildHeader(this, transform,
                "Team Management", TosTheme.TeamAccent, () => screen.GoBack());

            // ── Tab Area container ──
            _tabArea = CreateRegion("TeamTabs", MFAnchor.Top,
                new MFPadding(TosTheme.ContentPadH, TosTheme.ContentTopOffset, TosTheme.ContentPadH, 0));
            _tabArea.sizeDelta = new Vector2(0, 34);

            // ── Team Name ──
            var nameArea = CreateRegion("TeamName", MFAnchor.Top, new MFPadding(12, 92, 12, 0));
            nameArea.sizeDelta = new Vector2(0, 28);
            var nameHBox = CreateHBox("NameRow", nameArea, spacing: 8f);

            var nameGo = new GameObject("TeamNameLabel");
            nameGo.transform.SetParent(nameHBox, false);
            nameGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            _teamNameLabel = nameGo.AddComponent<Text>();
            _teamNameLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _teamNameLabel.fontSize = TosTheme.FontBody;
            _teamNameLabel.color = Color.white;
            _teamNameLabel.alignment = TextAnchor.MiddleLeft;
            _teamNameLabel.text = screen.ActiveTeam?.Name ?? "Team 1";

            var renameBtn = SpawnPrimitive<MFButton>(nameHBox);
            renameBtn.SetLabel("Rename");
            renameBtn.SetColor(new Color(0.25f, 0.25f, 0.3f), TosTheme.TextMuted);
            var renameLe = renameBtn.gameObject.GetComponent<LayoutElement>();
            if (renameLe != null) { renameLe.preferredWidth = 70; renameLe.flexibleWidth = 0; renameLe.preferredHeight = 26; }
            renameBtn.OnClick = () =>
            {
                UIServices.Popups?.ShowCustomDialog("rename_team", dialog =>
                {
                    dialog.SetTitle("Rename Team");
                    dialog.SetMessage($"Current: {screen.ActiveTeam?.Name}");
                    dialog.AddButton("Rename", () =>
                    {
                        string newName = $"Team {screen.ActiveTeamIndex + 1}-{Random.Range(1, 99)}";
                        screen.RenameTeam(newName);
                        _teamNameLabel.text = newName;
                        dialog.Dismiss();
                    }, new Color(0.3f, 0.6f, 0.4f));
                    dialog.AddButton("Cancel", () => dialog.Dismiss(), new Color(0.3f, 0.3f, 0.35f));
                });
            };

            // ── Team Slots container ──
            _slotArea = CreateRegion("TeamSlots", MFAnchor.Top, new MFPadding(8, 124, 8, 0));
            _slotArea.sizeDelta = new Vector2(0, 110);

            // ── Instructions ──
            var instrArea = CreateRegion("Instructions", MFAnchor.Top, new MFPadding(12, 238, 12, 0));
            instrArea.sizeDelta = new Vector2(0, 24);
            var instrText = instrArea.gameObject.AddComponent<Text>();
            instrText.text = "Tap a slot above, then tap a monster below to assign";
            instrText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            instrText.fontSize = TosTheme.FontSmall;
            instrText.color = TosTheme.TextMuted;
            instrText.alignment = TextAnchor.MiddleCenter;

            // ── Grid Area container ──
            _gridArea = CreateRegion("MonsterGrid", MFAnchor.Fill, new MFPadding(6, 268, 6, 90));

            // ── Status ──
            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(transform, false);
            var sRect = statusGo.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.05f, 0.06f);
            sRect.anchorMax = new Vector2(0.95f, 0.09f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;
            _statusLabel = statusGo.AddComponent<Text>();
            _statusLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusLabel.fontSize = TosTheme.FontBody;
            _statusLabel.color = TosTheme.TextGold;
            _statusLabel.alignment = TextAnchor.MiddleCenter;

            // Entrance animations (header already animates via TosPageHelper)
            StartCoroutine(MFUIAnim.SlideIn(_slotArea, new Vector2(0, -60), 0.35f));
            var gridCg = _gridArea.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(MFUIAnim.FadeIn(gridCg, 0.5f));
        }

        private void InitContent(TeamManageScreen screen)
        {
            // ── Team Tabs ──
            var tabParent = _tabArea != null ? _tabArea : CreateRegion("TeamTabs", MFAnchor.Top,
                new MFPadding(TosTheme.ContentPadH, TosTheme.ContentTopOffset, TosTheme.ContentPadH, 0));
            if (_tabArea == null) tabParent.sizeDelta = new Vector2(0, 34);
            _teamTabs = SpawnPrimitive<MFTabBar>(tabParent);

            var tabLabels = screen.Teams.Select(t => t.Name).ToArray();
            var tabColors = new Color[tabLabels.Length];
            for (int i = 0; i < tabColors.Length; i++)
                tabColors[i] = new Color(0.3f, 0.8f, 0.6f);
            _teamTabs.SetTabs(tabLabels, tabColors);
            _teamTabs.OnTabSelected = (index, _) =>
            {
                screen.SelectTeam(index);
                RefreshTeamSlots();
                if (_statusLabel != null) _statusLabel.text = screen.StatusMessage;
            };

            // ── Team Slots ──
            var slotParent = _slotArea != null ? _slotArea : CreateRegion("TeamSlots", MFAnchor.Top, new MFPadding(8, 124, 8, 0));
            if (_slotArea == null) slotParent.sizeDelta = new Vector2(0, 110);
            _slotContainer = CreateHBox("SlotRow", slotParent, spacing: 6f);
            var slotLayout = _slotContainer.GetComponent<HorizontalLayoutGroup>();
            if (slotLayout != null) slotLayout.childAlignment = TextAnchor.MiddleCenter;
            BuildTeamSlots();

            // ── Monster Grid ──
            var gridParent = _gridArea != null ? _gridArea : CreateRegion("MonsterGrid", MFAnchor.Fill, new MFPadding(6, 268, 6, 90));
            _monsterGrid = SpawnPrimitive<MFScrollGrid>(gridParent);
            _monsterGrid.Setup(null, BindMonsterCell, columns: 5, cellSize: 90f);
            _monsterGrid.SetItems(screen.AvailableMonsters.Cast<object>().ToList());
            _monsterGrid.OnItemSelected = (data, index) =>
            {
                if (screen.SelectedSlot < 0)
                {
                    if (_statusLabel != null) _statusLabel.text = "Select a team slot first";
                    return;
                }
                var entry = data as MonsterBoxEntry;
                if (entry == null) return;
                screen.AssignMonster(screen.SelectedSlot, entry.Instance.InstanceId);
                screen.SelectedSlot = -1;
                RefreshTeamSlots();
                if (_statusLabel != null) _statusLabel.text = screen.StatusMessage;
            };
        }

        private void BuildTeamSlots()
        {
            // Clear
            for (int i = _slotContainer.childCount - 1; i >= 0; i--)
                Destroy(_slotContainer.GetChild(i).gameObject);

            for (int i = 0; i < 5; i++)
            {
                int slotIndex = i;
                var monster = _screen.GetMonsterForSlot(i);
                bool isLeader = _screen.ActiveTeam?.LeaderSlot == i;
                bool isSelected = _screen.SelectedSlot == i;

                var slotGo = new GameObject($"Slot_{i}");
                slotGo.transform.SetParent(_slotContainer, false);

                Color slotBg;
                if (isSelected)
                    slotBg = new Color(0.3f, 0.3f, 0.1f, 0.95f); // highlight selected
                else if (monster != null)
                    slotBg = TosTheme.ElementPanelBg(monster.Def?.Element ?? 0);
                else
                    slotBg = new Color(0.1f, 0.1f, 0.14f, 0.8f);

                slotGo.AddComponent<Image>().color = slotBg;
                slotGo.AddComponent<LayoutElement>().flexibleWidth = 1;

                var vbox = slotGo.AddComponent<VerticalLayoutGroup>();
                vbox.padding = new RectOffset(4, 4, 4, 4);
                vbox.spacing = 2;
                vbox.childAlignment = TextAnchor.MiddleCenter;
                vbox.childForceExpandWidth = true;
                vbox.childForceExpandHeight = false;

                // Leader badge
                if (isLeader)
                {
                    var leaderGo = new GameObject("Leader");
                    leaderGo.transform.SetParent(slotGo.transform, false);
                    leaderGo.AddComponent<LayoutElement>().preferredHeight = 14;
                    var leaderText = leaderGo.AddComponent<Text>();
                    leaderText.text = "\u2605 LEADER";
                    leaderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    leaderText.fontSize = 8;
                    leaderText.color = TosTheme.TextGold;
                    leaderText.alignment = TextAnchor.MiddleCenter;
                    leaderText.fontStyle = FontStyle.Bold;
                }

                // Monster icon or empty
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(slotGo.transform, false);
                iconGo.AddComponent<LayoutElement>().preferredHeight = 30;
                var iconText = iconGo.AddComponent<Text>();
                iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                iconText.fontSize = 22;
                iconText.alignment = TextAnchor.MiddleCenter;

                if (monster != null)
                {
                    int elem = monster.Def?.Element ?? 0;
                    iconText.text = TosTheme.ElementIcon(elem);
                    iconText.color = TosTheme.ElementColor(elem);
                }
                else
                {
                    iconText.text = "+";
                    iconText.color = TosTheme.TextMuted;
                }

                // Name
                var nameGo = new GameObject("Name");
                nameGo.transform.SetParent(slotGo.transform, false);
                nameGo.AddComponent<LayoutElement>().preferredHeight = 16;
                var nameText = nameGo.AddComponent<Text>();
                nameText.text = monster?.DisplayName ?? "Empty";
                nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameText.fontSize = 9;
                nameText.color = monster != null ? Color.white : TosTheme.TextMuted;
                nameText.alignment = TextAnchor.MiddleCenter;

                // Slot number
                var numGo = new GameObject("Num");
                numGo.transform.SetParent(slotGo.transform, false);
                numGo.AddComponent<LayoutElement>().preferredHeight = 12;
                var numText = numGo.AddComponent<Text>();
                numText.text = $"Slot {i + 1}";
                numText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                numText.fontSize = 8;
                numText.color = TosTheme.TextMuted;
                numText.alignment = TextAnchor.MiddleCenter;

                // Click to select slot or long-press for options
                var btn = slotGo.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    if (_screen.SelectedSlot == slotIndex)
                    {
                        // Deselect or show options
                        if (monster != null)
                        {
                            UIServices.Popups?.ShowCustomDialog($"slot_{slotIndex}", dialog =>
                            {
                                dialog.SetTitle($"Slot {slotIndex + 1}: {monster.DisplayName}");
                                dialog.SetMessage("Choose an action:");
                                dialog.AddButton("Set Leader", () =>
                                {
                                    _screen.SetLeader(slotIndex);
                                    RefreshTeamSlots();
                                    _statusLabel.text = _screen.StatusMessage;
                                    dialog.Dismiss();
                                }, new Color(0.6f, 0.5f, 0.1f));
                                dialog.AddButton("Clear Slot", () =>
                                {
                                    _screen.ClearSlot(slotIndex);
                                    RefreshTeamSlots();
                                    dialog.Dismiss();
                                }, new Color(0.6f, 0.2f, 0.2f));
                                dialog.AddButton("Cancel", () => dialog.Dismiss(), new Color(0.3f, 0.3f, 0.35f));
                            });
                        }
                        _screen.SelectedSlot = -1;
                    }
                    else
                    {
                        _screen.SelectedSlot = slotIndex;
                        _statusLabel.text = $"Slot {slotIndex + 1} selected — tap a monster to assign";
                    }
                    RefreshTeamSlots();
                });
            }
        }

        private void RefreshTeamSlots()
        {
            BuildTeamSlots();
            _teamNameLabel.text = _screen.ActiveTeam?.Name ?? "";
        }

        private void BindMonsterCell(GameObject cell, object data, int index)
        {
            var entry = data as MonsterBoxEntry;
            if (entry == null) return;

            var img = cell.GetComponent<Image>();
            if (img != null && entry.Def != null)
                img.color = TosTheme.ElementPanelBg(entry.Def.Element);

            var label = cell.GetComponentInChildren<Text>();
            if (label != null)
            {
                int element = entry.Def?.Element ?? 0;
                int rarity = entry.Def?.Rarity ?? 1;
                label.text = $"{TosTheme.ElementIcon(element)} {entry.DisplayName}\n{TosTheme.RarityStars(rarity)} Lv.{entry.Instance.Level}";
                label.fontSize = 10;
                label.color = TosTheme.ElementColor(element);
            }
        }
    }
}

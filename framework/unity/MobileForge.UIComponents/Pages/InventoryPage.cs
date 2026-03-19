using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Monster box/inventory page. Scrollable grid with element-tinted cards,
    /// expanded detail panel with stats/skills/actions.
    /// Matches Godot monster_box_screen.gd.
    /// </summary>
    public class InventoryPage : PageBase<MonsterBoxScreen>
    {
        [Header("Prefab References")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _detailName;
        [SerializeField] private Text _detailStats;
        [SerializeField] private Text _detailInfo;
        [SerializeField] private Text _statusLabel;

        private MFScrollGrid _monsterGrid;
        private Transform _actionRow;
        [SerializeField] private RectTransform _gridArea;
        [SerializeField] private RectTransform _detailArea;

        protected override void OnBind(MonsterBoxScreen screen)
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
                    _titleLabel.text = $"Monster Box ({screen.Monsters.Count})";
                if (_detailName != null)
                    _detailName.text = "Tap a monster to see details";
                if (_detailStats != null)
                    _detailStats.text = "";
                if (_detailInfo != null)
                    _detailInfo.text = "";
                if (_statusLabel != null)
                    _statusLabel.text = "";
            }

            InitContent(screen);
        }

        private void BuildSkeleton(MonsterBoxScreen screen)
        {
            gameObject.AddComponent<Image>().color = TosTheme.BgDark;

            // ── Standardized Header ──
            var h = TosPageHelper.BuildHeader(this, transform,
                $"Monster Box ({screen.Monsters.Count})", TosTheme.ElementColors[3],
                () => screen.GoBack());

            // ── Grid Area container ──
            _gridArea = CreateRegion("GridArea", MFAnchor.Fill,
                new MFPadding(TosTheme.ContentPadH, TosTheme.ContentTopOffset, TosTheme.ContentPadH, 220));

            // ── Detail Panel ──
            _detailArea = CreateRegion("DetailArea", MFAnchor.Bottom, new MFPadding(6, 0, 6, 6));
            _detailArea.sizeDelta = new Vector2(0, 210);
            var detailBg = _detailArea.gameObject.AddComponent<Image>();
            detailBg.color = TosTheme.BgPanel;

            var detailVBox = CreateVBox("DetailContent", _detailArea, spacing: 3f);
            var detailLayout = detailVBox.GetComponent<VerticalLayoutGroup>();
            if (detailLayout != null) detailLayout.padding = new RectOffset(10, 10, 6, 6);

            // Name + Level
            var nameGo = new GameObject("DetailName");
            nameGo.transform.SetParent(detailVBox, false);
            nameGo.AddComponent<LayoutElement>().preferredHeight = 26;
            _detailName = nameGo.AddComponent<Text>();
            _detailName.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _detailName.fontSize = TosTheme.FontSubheader;
            _detailName.color = Color.white;
            _detailName.alignment = TextAnchor.MiddleLeft;

            // Stats
            var statsGo = new GameObject("DetailStats");
            statsGo.transform.SetParent(detailVBox, false);
            statsGo.AddComponent<LayoutElement>().preferredHeight = 20;
            _detailStats = statsGo.AddComponent<Text>();
            _detailStats.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _detailStats.fontSize = TosTheme.FontBody;
            _detailStats.color = Color.white;
            _detailStats.alignment = TextAnchor.MiddleLeft;

            // Info (element, rarity, cost, plus-stats, skill)
            var infoGo = new GameObject("DetailInfo");
            infoGo.transform.SetParent(detailVBox, false);
            infoGo.AddComponent<LayoutElement>().preferredHeight = 36;
            _detailInfo = infoGo.AddComponent<Text>();
            _detailInfo.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _detailInfo.fontSize = TosTheme.FontSmall;
            _detailInfo.color = TosTheme.TextMuted;
            _detailInfo.alignment = TextAnchor.MiddleLeft;

            // Action buttons row
            var actionGo = new GameObject("ActionRow");
            actionGo.transform.SetParent(detailVBox, false);
            actionGo.AddComponent<LayoutElement>().preferredHeight = 40;
            var actionLayout = actionGo.AddComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 4;
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.childForceExpandWidth = true;
            actionLayout.childForceExpandHeight = true;
            _actionRow = actionGo.transform;

            // Action buttons row 2
            var actionGo2 = new GameObject("ActionRow2");
            actionGo2.transform.SetParent(detailVBox, false);
            actionGo2.AddComponent<LayoutElement>().preferredHeight = 40;
            var actionLayout2 = actionGo2.AddComponent<HorizontalLayoutGroup>();
            actionLayout2.spacing = 4;
            actionLayout2.childAlignment = TextAnchor.MiddleCenter;
            actionLayout2.childForceExpandWidth = true;
            actionLayout2.childForceExpandHeight = true;

            // Status
            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(detailVBox, false);
            statusGo.AddComponent<LayoutElement>().preferredHeight = 18;
            _statusLabel = statusGo.AddComponent<Text>();
            _statusLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusLabel.fontSize = TosTheme.FontSmall;
            _statusLabel.color = TosTheme.TextGold;
            _statusLabel.alignment = TextAnchor.MiddleCenter;

            // Entrance animations (header already animates via TosPageHelper)
            var gridCg = _gridArea.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(MFUIAnim.FadeIn(gridCg, 0.4f));
            var detailCg = _detailArea.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(MFUIAnim.SlideIn(_detailArea, new Vector2(0, -100), 0.4f));
            StartCoroutine(MFUIAnim.FadeIn(detailCg, 0.5f));
        }

        private void InitContent(MonsterBoxScreen screen)
        {
            // ── Monster Grid ──
            _monsterGrid = SpawnPrimitive<MFScrollGrid>(_gridArea != null ? _gridArea : CreateRegion("GridArea", MFAnchor.Fill,
                new MFPadding(TosTheme.ContentPadH, TosTheme.ContentTopOffset, TosTheme.ContentPadH, 220)));
            _monsterGrid.Setup(null, BindMonsterCell, columns: 5, cellSize: 90f);
            _monsterGrid.SetItems(screen.Monsters.Cast<object>().ToList());
            _monsterGrid.OnItemSelected = (data, index) =>
            {
                screen.SelectMonster(index);
                UpdateDetail();
            };

            // ── Action Buttons ──
            if (_actionRow != null)
            {
                // View Detail button (shows MonsterDetailPopup matching original General_CardInformation_View)
                CreateActionButton(_actionRow, "Detail", new Color(0.3f, 0.3f, 0.5f), () =>
                {
                    var sel = _screen.SelectedMonster;
                    if (sel != null)
                        MonsterDetailPopup.Show(transform, sel);
                    else
                        UIServices.Toasts?.ShowToast("Select a monster first");
                });
                // Row 1 actions
                CreateActionButton(_actionRow, "Fuse", new Color(0.3f, 0.5f, 0.2f), () =>
                {
                    _screen.StartFusion();
                    RefreshAfterAction();
                });
                CreateActionButton(_actionRow, "Evolve", new Color(0.6f, 0.4f, 0.1f), () =>
                {
                    if (_screen.CanEvolve())
                    {
                        UIServices.Popups?.ShowDialog("evolve_confirm",
                            "Evolve Monster",
                            _screen.GetEvolveInfo() + "\n\nProceed?",
                            "Evolve!", () =>
                            {
                                _screen.ExecuteEvolution();
                                RefreshAfterAction();
                            });
                    }
                    else
                    {
                        _statusLabel.text = _screen.GetEvolveInfo();
                    }
                });
                CreateActionButton(_actionRow, "+Stat", new Color(0.2f, 0.4f, 0.7f), () =>
                {
                    _screen.StartPlusFuse();
                    RefreshAfterAction();
                });
                CreateActionButton(_actionRow, "Skill Up", new Color(0.5f, 0.3f, 0.6f), () =>
                {
                    _screen.StartSkillUp();
                    RefreshAfterAction();
                });

                // Row 2 actions (find ActionRow2 sibling)
                var actionGo2 = _actionRow.parent.Find("ActionRow2");
                if (actionGo2 != null)
                {
                    CreateActionButton(actionGo2, "Awaken", new Color(0.7f, 0.5f, 0.1f), () =>
                    {
                        _screen.Awaken();
                        RefreshAfterAction();
                    });
                    CreateActionButton(actionGo2, "Inherit", new Color(0.4f, 0.3f, 0.5f), () =>
                    {
                        // If selected monster already has an inherited skill, offer to remove it
                        var sel = _screen.SelectedMonster;
                        if (sel != null && sel.Instance.InheritedSkillId >= 0)
                        {
                            UIServices.Popups?.ShowCustomDialog("inherit_choice", dialog =>
                            {
                                dialog.SetTitle("Skill Inheritance");
                                dialog.SetMessage($"Current inherited skill: #{sel.Instance.InheritedSkillId}\n\nReplace or remove?");
                                dialog.AddButton("Replace", () =>
                                {
                                    _screen.RemoveInheritedSkill();
                                    _screen.StartInherit();
                                    RefreshAfterAction();
                                    dialog.Dismiss();
                                }, new Color(0.4f, 0.3f, 0.5f));
                                dialog.AddButton("Remove", () =>
                                {
                                    _screen.RemoveInheritedSkill();
                                    RefreshAfterAction();
                                    dialog.Dismiss();
                                }, new Color(0.6f, 0.2f, 0.2f));
                                dialog.AddButton("Cancel", () => dialog.Dismiss(), new Color(0.3f, 0.3f, 0.35f));
                            });
                        }
                        else
                        {
                            _screen.StartInherit();
                            RefreshAfterAction();
                        }
                    });
                    CreateActionButton(actionGo2, "\u2605 Fav", new Color(0.6f, 0.6f, 0.2f), () =>
                    {
                        _screen.ToggleFavorite();
                        RefreshAfterAction();
                    });
                    CreateActionButton(actionGo2, "L.Break", new Color(0.7f, 0.2f, 0.2f), () =>
                    {
                        _screen.LimitBreak();
                        RefreshAfterAction();
                    });
                }
            }

            UpdateDetail();
        }

        private void BindMonsterCell(GameObject cell, object data, int index)
        {
            var entry = data as MonsterBoxEntry;
            if (entry == null) return;

            // Element-tinted background
            var img = cell.GetComponent<Image>();
            if (img != null && entry.Def != null)
                img.color = TosTheme.ElementPanelBg(entry.Def.Element);

            var label = cell.GetComponentInChildren<Text>();
            if (label != null)
            {
                int element = entry.Def?.Element ?? 0;
                int rarity = entry.Def?.Rarity ?? 1;
                string stars = TosTheme.RarityStars(rarity);
                label.text = $"{TosTheme.ElementIcon(element)} {entry.DisplayName}\n{stars} Lv.{entry.Instance.Level}";
                label.fontSize = 10;
                label.color = TosTheme.ElementColor(element);
            }
        }

        private void UpdateDetail()
        {
            // Show action mode status
            if (_screen.CurrentMode != MonsterBoxScreen.ActionMode.None)
            {
                _detailName.text = _screen.StatusMessage;
                _detailName.color = TosTheme.TextGold;
                _detailStats.text = "Tap a monster in the grid to use as fodder";
                _detailInfo.text = "Tap Back to cancel";
                return;
            }

            var sel = _screen.SelectedMonster;
            if (sel == null)
            {
                _detailName.text = "Tap a monster to see details";
                _detailName.color = Color.white;
                _detailStats.text = "";
                _detailInfo.text = "";
                return;
            }

            int element = sel.Def?.Element ?? 0;
            int rarity = sel.Def?.Rarity ?? 1;

            _detailName.text = $"{TosTheme.ElementIcon(element)} {sel.DisplayName} (Lv.{sel.Instance.Level})";
            _detailName.color = TosTheme.ElementColor(element);

            if (sel.Stats != null)
                _detailStats.text = $"HP: {sel.Stats.Hp}  |  ATK: {sel.Stats.Atk}  |  REC: {sel.Stats.Rec}";
            else
                _detailStats.text = "Stats: N/A";

            string info = $"{TosTheme.RarityStars(rarity)} {sel.ElementName}";
            info += $"  |  Skill Lv.{sel.Instance.SkillLevel}";
            if (sel.Instance.InheritedSkillId >= 0)
                info += $"  |  Inherited: Skill #{sel.Instance.InheritedSkillId}";
            int plusTotal = sel.Instance.PlusHp + sel.Instance.PlusAtk + sel.Instance.PlusRec;
            if (plusTotal > 0)
                info += $"  |  +{plusTotal}";
            int awakened = sel.Instance.Awakenings.Count(a => a);
            if (awakened > 0)
                info += $"  |  Awaken: {awakened}/{sel.Instance.Awakenings.Count}";
            if (sel.Instance.LimitBreakLevel > 0)
                info += $"  |  LB+{sel.Instance.LimitBreakLevel}";
            _detailInfo.text = info;
        }

        private void RefreshAfterAction()
        {
            _statusLabel.text = _screen.StatusMessage;
            _monsterGrid.SetItems(_screen.Monsters.Cast<object>().ToList());
            UpdateDetail();

            if (!string.IsNullOrEmpty(_screen.StatusMessage))
                UIServices.Toasts?.ShowToast(_screen.StatusMessage);
        }

        private void CreateActionButton(Transform parent, string label, Color color, System.Action onClick)
        {
            var btn = TosPageHelper.MakeActionButton(parent, label, color, onClick);
            TosPageHelper.AddPressFeedback(btn.gameObject);
        }
    }
}

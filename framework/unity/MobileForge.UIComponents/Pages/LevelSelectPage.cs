using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;
using MobileForge.Presentation;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Dungeon/level select page with difficulty tabs, stamina display,
    /// floor effects badges, and turn limit info. Matches Godot dungeon_select_screen.gd.
    /// </summary>
    public class LevelSelectPage : PageBase<DungeonSelectScreen>
    {
        [Header("Prefab References")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _staminaLabel;

        private MFScrollList _stageList;
        private MFTabBar _tabBar;
        private UIRouter _router;
        [SerializeField] private RectTransform _tabArea;
        [SerializeField] private RectTransform _listArea;

        public void SetRouter(UIRouter router) => _router = router;

        protected override void OnBind(DungeonSelectScreen screen)
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
                    _titleLabel.text = "Select Dungeon";
                if (_staminaLabel != null)
                    UpdateStaminaLabel();
            }

            InitContent(screen);
        }

        private void BuildSkeleton(DungeonSelectScreen screen)
        {
            gameObject.AddComponent<Image>().color = TosTheme.BgDark;

            // ── Standardized Header ──
            var h = TosPageHelper.BuildHeader(this, transform,
                "Select Dungeon", Color.white, () => screen.GoBack());

            // Stamina display in header
            var staminaGo = new GameObject("Stamina");
            staminaGo.transform.SetParent(h.Header, false);
            staminaGo.AddComponent<RectTransform>();
            var staminaLe = staminaGo.AddComponent<LayoutElement>();
            staminaLe.preferredWidth = 90;
            staminaLe.flexibleWidth = 0;
            _staminaLabel = staminaGo.AddComponent<Text>();
            _staminaLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _staminaLabel.fontSize = TosTheme.FontBody;
            _staminaLabel.alignment = TextAnchor.MiddleRight;
            _staminaLabel.color = new Color(0.6f, 1f, 0.6f);
            UpdateStaminaLabel();

            // ── Tab Bar container ──
            _tabArea = CreateRegion("TabArea", MFAnchor.Top,
                new MFPadding(TosTheme.ContentPadH, TosTheme.ContentTopOffset, TosTheme.ContentPadH, 0));
            _tabArea.sizeDelta = new Vector2(0, 36);

            // ── List container ──
            _listArea = CreateRegion("ListArea", MFAnchor.Fill,
                new MFPadding(TosTheme.ContentPadH, TosTheme.ContentTopOffset + 42, TosTheme.ContentPadH, TosTheme.ContentBottomNav));

            // Entrance animation (header already animates via TosPageHelper)
            var listCg = _listArea.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(MFUIAnim.FadeIn(listCg, 0.4f));
        }

        private void InitContent(DungeonSelectScreen screen)
        {
            // ── Difficulty Tab Bar ──
            var tabParent = _tabArea != null ? _tabArea : CreateRegion("TabArea", MFAnchor.Top,
                new MFPadding(TosTheme.ContentPadH, TosTheme.ContentTopOffset, TosTheme.ContentPadH, 0));
            if (_tabArea == null) tabParent.sizeDelta = new Vector2(0, 36);
            _tabBar = SpawnPrimitive<MFTabBar>(tabParent);

            var diffs = screen.AvailableDifficulties;
            var labels = diffs.Select(d => char.ToUpper(d[0]) + d.Substring(1)).ToArray();
            var colors = diffs.Select(d => TosTheme.DifficultyColor(d)).ToArray();
            _tabBar.SetTabs(labels, colors);

            int activeIdx = diffs.IndexOf(screen.ActiveDifficulty);
            if (activeIdx >= 0) _tabBar.SetActive(activeIdx, silent: true);

            _tabBar.OnTabSelected = (index, label) =>
            {
                screen.SetDifficulty(diffs[index]);
                RefreshStageList();
            };

            // ── Stage List ──
            var listParent = _listArea != null ? _listArea : CreateRegion("ListArea", MFAnchor.Fill,
                new MFPadding(TosTheme.ContentPadH, TosTheme.ContentTopOffset + 42, TosTheme.ContentPadH, TosTheme.ContentBottomNav));
            _stageList = SpawnPrimitive<MFScrollList>(listParent);
            _stageList.Setup(null, BindStageCell, itemHeight: 80f);
            RefreshStageList();
        }

        private void RefreshStageList()
        {
            _stageList.SetItems(_screen.Stages.Cast<object>().ToList());
            UpdateStaminaLabel();
        }

        private void UpdateStaminaLabel()
        {
            if (_staminaLabel != null)
                _staminaLabel.text = $"ST: {_screen.CurrentStamina}/{_screen.MaxStamina}";
        }

        private void BindStageCell(GameObject cell, object data, int index)
        {
            var stage = data as DungeonSelectScreen.StageEntry;
            if (stage == null) return;

            // Configure cell background
            var cellBg = cell.GetComponent<Image>();
            if (cellBg == null) cellBg = cell.AddComponent<Image>();
            cellBg.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);

            // Clear old children (recycled cells)
            for (int i = cell.transform.childCount - 1; i >= 0; i--)
                Destroy(cell.transform.GetChild(i).gameObject);

            // Difficulty color strip
            var strip = new GameObject("Strip");
            strip.transform.SetParent(cell.transform, false);
            var stripRect = strip.AddComponent<RectTransform>();
            stripRect.anchorMin = Vector2.zero;
            stripRect.anchorMax = new Vector2(0, 1);
            stripRect.offsetMin = Vector2.zero;
            stripRect.offsetMax = new Vector2(4, 0);
            var stripImg = strip.AddComponent<Image>();
            stripImg.color = TosTheme.DifficultyColor(stage.Difficulty);

            // Content area
            var content = new GameObject("Content");
            content.transform.SetParent(cell.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(10, 4);
            contentRect.offsetMax = new Vector2(-90, -4);

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 2;

            // Stage name
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(content.transform, false);
            var nameText = nameGo.AddComponent<Text>();
            nameText.text = stage.Name;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 16;
            nameText.color = TosTheme.DifficultyColor(stage.Difficulty);
            nameGo.AddComponent<LayoutElement>().preferredHeight = 22;

            // Details row
            var detailGo = new GameObject("Details");
            detailGo.transform.SetParent(content.transform, false);
            var detailText = detailGo.AddComponent<Text>();
            string details = $"Stamina: {stage.StaminaCost} | {stage.WaveCount} waves";
            if (stage.TurnLimit > 0) details += $" | {stage.TurnLimit} turns";
            if (stage.IsDaily) details += " | Daily";
            if (stage.BoardRows != 5 || stage.BoardCols != 6)
                details += $" | {stage.BoardRows}x{stage.BoardCols}";
            detailText.text = details;
            detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailText.fontSize = 11;
            detailText.color = TosTheme.TextMuted;
            detailGo.AddComponent<LayoutElement>().preferredHeight = 16;

            // Floor effects row
            if (stage.FloorEffects.Count > 0)
            {
                var effectsGo = new GameObject("Effects");
                effectsGo.transform.SetParent(content.transform, false);
                var effectsLayout = effectsGo.AddComponent<HorizontalLayoutGroup>();
                effectsLayout.spacing = 4;
                effectsLayout.childForceExpandWidth = false;
                effectsLayout.childForceExpandHeight = false;
                effectsGo.AddComponent<LayoutElement>().preferredHeight = 18;

                foreach (var effect in stage.FloorEffects)
                {
                    var badgeGo = new GameObject($"Badge_{effect}");
                    badgeGo.transform.SetParent(effectsGo.transform, false);
                    var badgeBg = badgeGo.AddComponent<Image>();
                    badgeBg.color = new Color(0.3f, 0.15f, 0.1f, 0.9f);
                    var badgeLe = badgeGo.AddComponent<LayoutElement>();
                    badgeLe.preferredWidth = 70;
                    badgeLe.preferredHeight = 16;

                    var badgeLbl = new GameObject("Label");
                    badgeLbl.transform.SetParent(badgeGo.transform, false);
                    var blRect = badgeLbl.AddComponent<RectTransform>();
                    blRect.anchorMin = Vector2.zero;
                    blRect.anchorMax = Vector2.one;
                    blRect.offsetMin = Vector2.zero;
                    blRect.offsetMax = Vector2.zero;
                    var blText = badgeLbl.AddComponent<Text>();
                    blText.text = FormatEffectName(effect);
                    blText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    blText.fontSize = 9;
                    blText.alignment = TextAnchor.MiddleCenter;
                    blText.color = new Color(1f, 0.5f, 0.3f);
                }
            }

            // Enter button
            var btnGo = new GameObject("EnterBtn");
            btnGo.transform.SetParent(cell.transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.offsetMin = new Vector2(-80, 8);
            btnRect.offsetMax = new Vector2(-4, -8);
            var btnBg = btnGo.AddComponent<Image>();
            btnBg.color = stage.CanAfford
                ? TosTheme.DifficultyColor(stage.Difficulty)
                : new Color(0.3f, 0.3f, 0.3f);
            var btn = btnGo.AddComponent<Button>();
            btn.interactable = stage.CanAfford;
            btn.onClick.AddListener(() =>
            {
                if (!_screen.SelectStage(stage.StageId, stage.StaminaCost))
                {
                    // Show refill popup instead of just a toast
                    UIServices.Popups?.ShowCustomDialog("stamina_refill", dialog =>
                    {
                        dialog.SetTitle("Not Enough Stamina");
                        dialog.SetMessage(
                            $"Need {stage.StaminaCost} ST, have {_screen.CurrentStamina}.\n\n" +
                            "Refill stamina for 1 Gem?");
                        dialog.AddButton("Refill (1 Gem)", () =>
                        {
                            var ps = MobileForge.Infrastructure.PlayerState.Instance;
                            int gems = System.Convert.ToInt32(ps?.GetValue("currencies", "gems", 0) ?? 0);
                            if (gems >= 1)
                            {
                                ps?.SetValue("currencies", "gems", gems - 1);
                                int st = System.Convert.ToInt32(ps?.GetValue("currencies", "stamina", 0) ?? 0);
                                ps?.SetValue("currencies", "stamina", st + 100);
                                UpdateStaminaLabel();
                                UIServices.Toasts?.ShowToast("Stamina refilled! +100");
                            }
                            else
                            {
                                UIServices.Toasts?.ShowToast("Not enough gems!");
                            }
                            dialog.Dismiss();
                        }, new Color(0.2f, 0.6f, 0.3f));
                        dialog.AddButton("Cancel", () => dialog.Dismiss(), new Color(0.3f, 0.3f, 0.35f));
                    });
                }
            });

            var btnLabel = new GameObject("BtnLabel");
            btnLabel.transform.SetParent(btnGo.transform, false);
            var btnLabelRect = btnLabel.AddComponent<RectTransform>();
            btnLabelRect.anchorMin = Vector2.zero;
            btnLabelRect.anchorMax = Vector2.one;
            btnLabelRect.offsetMin = Vector2.zero;
            btnLabelRect.offsetMax = Vector2.zero;
            var btnText = btnLabel.AddComponent<Text>();
            btnText.text = "Enter";
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = TosTheme.FontBody;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
        }

        private static string FormatEffectName(string effect)
        {
            return effect.Replace("_", " ");
        }

        protected override void OnRefresh()
        {
            RefreshStageList();
        }
    }
}

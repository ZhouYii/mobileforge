using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Gacha pull page with support for regular, step-up, one-time, and daily-free pools.
    /// Shows all pools in a scrollable list with type-specific labels and buttons.
    /// Matches Godot gacha_screen.gd feature parity.
    /// </summary>
    public class GachaPage : PageBase<GachaScreen>
    {
        [Header("Prefab References")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _statusLabel;
        [SerializeField] private Transform _poolContainer;

        private MFCurrencyDisplay _gemsDisplay;
        private MFScrollGrid _resultsGrid;

        protected override void OnBind(GachaScreen screen)
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
                    _titleLabel.text = "Gacha";
                if (_statusLabel != null)
                    _statusLabel.text = screen.StatusMessage;
            }

            InitContent(screen);
        }

        private void BuildSkeleton(GachaScreen screen)
        {
            gameObject.AddComponent<Image>().color = TosTheme.BgDark;

            // ── Standardized Header ──
            var h = TosPageHelper.BuildHeader(this, transform,
                "Gacha", TosTheme.TextGold, () => screen.GoBack());

            // Gems display in header
            _gemsDisplay = SpawnPrimitive<MFCurrencyDisplay>(h.Header);
            _gemsDisplay.SetImmediate(screen.GemsBalance);

            // ── Pool List (scrollable) ──
            var poolArea = CreateRegion("PoolArea", MFAnchor.Fill,
                new MFPadding(TosTheme.ContentPadH, TosTheme.ContentTopOffset, TosTheme.ContentPadH, 200));
            _poolContainer = TosPageHelper.BuildScrollContent(poolArea, spacing: 10f);

            // ── Status ──
            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(transform, false);
            var sRect = statusGo.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.05f, 0.19f); sRect.anchorMax = new Vector2(0.95f, 0.23f);
            sRect.offsetMin = Vector2.zero; sRect.offsetMax = Vector2.zero;
            _statusLabel = statusGo.AddComponent<Text>();
            _statusLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusLabel.fontSize = TosTheme.FontBody;
            _statusLabel.color = TosTheme.TextGold;
            _statusLabel.alignment = TextAnchor.MiddleCenter;

            // Entrance animations (header already animates via TosPageHelper)
            var poolCg = poolArea.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(MFUIAnim.FadeIn(poolCg, 0.5f));
        }

        private void InitContent(GachaScreen screen)
        {
            // Currency display (prefab path)
            if (_gemsDisplay == null)
            {
                _gemsDisplay = SpawnPrimitive<MFCurrencyDisplay>(transform);
                _gemsDisplay.SetImmediate(screen.GemsBalance);
            }

            // Pool cards into container
            if (_poolContainer != null)
                BuildPoolCards();

            // Results grid
            var resultsArea = CreateRegion("Results", MFAnchor.Bottom, new MFPadding(6, 0, 6, 6));
            resultsArea.sizeDelta = new Vector2(0, 190);
            _resultsGrid = SpawnPrimitive<MFScrollGrid>(resultsArea);
            _resultsGrid.Setup(null, BindResultCell, columns: 5, cellSize: 90f);

            if (screen.LastResults.Count > 0)
                _resultsGrid.SetItems(screen.LastResults.Cast<object>().ToList());
        }

        private void BuildPoolCards()
        {
            // Clear existing
            for (int i = _poolContainer.childCount - 1; i >= 0; i--)
                Destroy(_poolContainer.GetChild(i).gameObject);

            foreach (var pool in _screen.Pools)
                CreatePoolCard(_poolContainer, pool);

            // Monster Exchange section
            if (_screen.ExchangeOffers.Count > 0)
                BuildExchangeSection(_poolContainer);
        }

        private void CreatePoolCard(Transform parent, GachaPoolDisplay pd)
        {
            var cardGo = new GameObject($"Pool_{pd.Pool.Id}");
            cardGo.transform.SetParent(parent, false);
            cardGo.AddComponent<Image>().color = TosTheme.BgPanel;
            var cardLe = cardGo.AddComponent<LayoutElement>();
            cardLe.preferredHeight = 120;

            var vbox = cardGo.AddComponent<VerticalLayoutGroup>();
            vbox.padding = new RectOffset(10, 10, 6, 6);
            vbox.spacing = 3;
            vbox.childForceExpandWidth = true;
            vbox.childForceExpandHeight = false;

            // Pool name + type badge
            string typeBadge = pd.PoolType switch
            {
                PoolType.StepUp => " [STEP-UP]",
                PoolType.OneTime => " [ONE-TIME]",
                _ => pd.HasDailyFree ? " [DAILY FREE]" : ""
            };
            var nameGo = MakeLabel(cardGo.transform, $"{pd.Pool.Name}{typeBadge}", TosTheme.FontBody + 2, Color.white, 24);

            // Info line
            string infoText;
            if (pd.PoolType == PoolType.StepUp)
            {
                if (pd.IsStepUpComplete)
                    infoText = "All steps complete!";
                else
                {
                    int cost = (int)(pd.Pool.CostAmount * pd.StepCostMult) * pd.StepPullCount;
                    string guarStr = pd.StepGuaranteedRarity > 0 ? $" | Guaranteed {TosTheme.RarityStars(pd.StepGuaranteedRarity)}" : "";
                    infoText = $"Step {pd.CurrentStep + 1}/{pd.TotalSteps} \u2014 {cost} {pd.Pool.CostCurrency} for {pd.StepPullCount} pulls{guarStr}";
                }
            }
            else if (pd.IsOneTime)
            {
                infoText = pd.IsOneTimeUsed ? "Completed" :
                    (pd.GuaranteedTopRarity ? "One-time only \u2014 Guaranteed top rarity!" : "One-time only");
            }
            else
            {
                string discountStr = pd.MultiPullDiscount > 0 ? $" | 10-pull: pay for {10 - pd.MultiPullDiscount}" : "";
                infoText = $"Cost: {pd.Pool.CostAmount} {pd.Pool.CostCurrency}/pull{discountStr}";
            }
            var infoGo = MakeLabel(cardGo.transform, infoText, 11,
                pd.IsOneTimeUsed || pd.IsStepUpComplete ? TosTheme.TextMuted : new Color(0.8f, 0.8f, 0.6f), 18);

            // Rate display
            if (pd.Rates != null && pd.Rates.Count > 0)
            {
                string rateStr = string.Join("  ", pd.Rates.Select(r => $"\u2605{r.Key}: {r.Value:P1}"));
                MakeLabel(cardGo.transform, rateStr, 10, TosTheme.TextMuted, 14);
            }

            // Buttons
            var btnRow = new GameObject("Buttons");
            btnRow.transform.SetParent(cardGo.transform, false);
            var btnLayout = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnLayout.spacing = 8; btnLayout.childForceExpandWidth = true;
            btnLayout.childForceExpandHeight = true;
            btnRow.AddComponent<LayoutElement>().preferredHeight = 40;

            if (pd.PoolType == PoolType.StepUp)
            {
                bool canPull = !pd.IsStepUpComplete;
                string label = pd.IsStepUpComplete ? "Done" :
                    (pd.StepCostMult <= 0 ? "FREE Step!" : $"Step {pd.CurrentStep + 1}");
                CreatePullButton(btnRow.transform, label, canPull, new Color(0.6f, 0.3f, 0.1f), () =>
                {
                    _screen.DoPull(pd, pd.StepPullCount);
                    RefreshAll();
                });
            }
            else if (pd.IsOneTime)
            {
                bool canPull = !pd.IsOneTimeUsed;
                CreatePullButton(btnRow.transform, canPull ? "Pull x10" : "Completed", canPull,
                    new Color(0.7f, 0.5f, 0.1f), () =>
                {
                    _screen.DoPull(pd, 10);
                    RefreshAll();
                });
            }
            else
            {
                // Free pull button
                if (pd.HasDailyFree && !pd.DailyFreeUsedToday)
                {
                    CreatePullButton(btnRow.transform, "FREE x1", true, new Color(0.2f, 0.7f, 0.3f), () =>
                    {
                        _screen.DoPull(pd, 1);
                        RefreshAll();
                    });
                }

                // Pull x1
                CreatePullButton(btnRow.transform, "Pull x1", true, new Color(0.2f, 0.4f, 0.8f), () =>
                {
                    _screen.DoPull(pd, 1);
                    RefreshAll();
                });

                // Pull x10
                string x10Label = pd.MultiPullDiscount > 0
                    ? $"Pull x10 ({pd.Pool.CostAmount * (10 - pd.MultiPullDiscount)})"
                    : "Pull x10";
                CreatePullButton(btnRow.transform, x10Label, true, new Color(0.7f, 0.4f, 0.1f), () =>
                {
                    _screen.DoPull(pd, 10);
                    RefreshAll();
                });
            }
        }

        private void BuildExchangeSection(Transform parent)
        {
            // Separator
            var sepGo = new GameObject("ExchangeSep");
            sepGo.transform.SetParent(parent, false);
            sepGo.AddComponent<Image>().color = new Color(0.3f, 0.2f, 0.4f, 0.6f);
            sepGo.AddComponent<LayoutElement>().preferredHeight = 2;

            // Header
            MakeLabel(parent, "Monster Exchange", TosTheme.FontSubheader,
                new Color(0.7f, 0.5f, 1f), 28);
            MakeLabel(parent, "Trade duplicate monsters for guaranteed targets", 11,
                TosTheme.TextMuted, 16);

            foreach (var offer in _screen.ExchangeOffers)
            {
                var offerGo = new GameObject($"Exchange_{offer.Id}");
                offerGo.transform.SetParent(parent, false);
                offerGo.AddComponent<Image>().color = new Color(0.12f, 0.1f, 0.18f, 0.92f);
                offerGo.AddComponent<LayoutElement>().preferredHeight = 60;

                var hbox = offerGo.AddComponent<HorizontalLayoutGroup>();
                hbox.padding = new RectOffset(10, 10, 6, 6);
                hbox.spacing = 8;
                hbox.childForceExpandWidth = false;
                hbox.childForceExpandHeight = true;

                // Info
                var infoGo = new GameObject("Info");
                infoGo.transform.SetParent(offerGo.transform, false);
                infoGo.AddComponent<LayoutElement>().flexibleWidth = 1;
                var infoLayout = infoGo.AddComponent<VerticalLayoutGroup>();
                infoLayout.childForceExpandHeight = false;
                infoLayout.spacing = 2;

                var nameGo = new GameObject("Name");
                nameGo.transform.SetParent(infoGo.transform, false);
                nameGo.AddComponent<LayoutElement>().preferredHeight = 22;
                var nameTxt = nameGo.AddComponent<Text>();
                nameTxt.text = $"Get: {offer.TargetMonsterName}";
                nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameTxt.fontSize = TosTheme.FontBody;
                nameTxt.color = Color.white;

                string limitStr = offer.ExchangeLimit > 0 ? $", Limit: {offer.TimesUsed}/{offer.ExchangeLimit}" : "";
                var reqGo = new GameObject("Req");
                reqGo.transform.SetParent(infoGo.transform, false);
                reqGo.AddComponent<LayoutElement>().preferredHeight = 18;
                var reqTxt = reqGo.AddComponent<Text>();
                reqTxt.text = $"Requires: {offer.RequiredCount} monsters, {offer.RequiredMinRarity}\u2605+ rarity{limitStr}";
                reqTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                reqTxt.fontSize = 11;
                reqTxt.color = TosTheme.TextMuted;

                // Button
                int offerId = offer.Id;
                CreatePullButton(offerGo.transform,
                    offer.IsDone ? "Done" : "Exchange",
                    !offer.IsDone,
                    offer.IsDone ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.5f, 0.3f, 0.7f),
                    () =>
                    {
                        _screen.ExecuteExchange(offerId);
                        RefreshAll();
                    });
            }
        }

        private void CreatePullButton(Transform parent, string label, bool enabled, Color color, System.Action onClick)
        {
            Color btnColor = enabled ? color : new Color(0.2f, 0.2f, 0.2f);
            var btn = TosPageHelper.MakeActionButton(parent, label, btnColor, onClick);
            btn.interactable = enabled;
            TosPageHelper.AddPressFeedback(btn.gameObject);
        }

        private GameObject MakeLabel(Transform parent, string text, int fontSize, Color color, float height)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleLeft;
            return go;
        }

        private void BindResultCell(GameObject cell, object data, int index)
        {
            var result = data as GachaResultDisplay;
            if (result == null) return;

            var cellBg = cell.GetComponent<Image>();
            if (cellBg != null) cellBg.color = TosTheme.ElementPanelBg(0);

            var label = cell.GetComponentInChildren<Text>();
            if (label != null)
            {
                string stars = TosTheme.RarityStars(result.Rarity);
                string suffix = result.IsPity ? "\n(PITY!)" : result.IsFeatured ? "\n(\u2605FEAT)" : "";
                label.text = $"{stars}\n{result.MonsterName}{suffix}";
                label.fontSize = 10;
                label.color = result.IsFeatured ? TosTheme.TextGold
                    : result.IsPity ? new Color(0.3f, 1f, 1f)
                    : TosTheme.RarityColor(result.Rarity);
            }

            // Tap to show detail popup
            var btn = cell.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    UIServices.Popups?.ShowDialog($"gacha_detail_{index}",
                        result.MonsterName,
                        $"{TosTheme.RarityStars(result.Rarity)}\n" +
                        $"Rarity: {result.Rarity}\u2605\n" +
                        (result.IsFeatured ? "Featured!\n" : "") +
                        (result.IsPity ? "Pity pull!\n" : ""),
                        "OK");
                });
            }
        }

        private void RefreshAll()
        {
            _gemsDisplay.SetValue(_screen.GemsBalance);
            _statusLabel.text = _screen.StatusMessage;
            BuildPoolCards();
            if (_screen.LastResults.Count > 0)
                _resultsGrid.SetItems(_screen.LastResults.Cast<object>().ToList());
        }

        protected override void OnRefresh()
        {
            RefreshAll();
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Shop page with General/Event tabs. Shows currency balances,
    /// purchase options, and event shop items. Matches Godot shop_screen.gd.
    /// </summary>
    public class ShopPage : PageBase<ShopScreen>
    {
        [Header("Prefab References")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _gemsLabel;
        [SerializeField] private Text _coinsLabel;
        [SerializeField] private Text _staminaLabel;
        [SerializeField] private Text _statusLabel;

        private MFTabBar _tabBar;
        private Transform _generalContent;
        private Transform _eventContent;

        protected override void OnBind(ShopScreen screen)
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
                    _titleLabel.text = "Shop";
                if (_gemsLabel != null)
                    _gemsLabel.text = $"Gems: {screen.GemsBalance}";
                if (_coinsLabel != null)
                    _coinsLabel.text = $"Coins: {screen.CoinsBalance}";
                if (_staminaLabel != null)
                    _staminaLabel.text = $"ST: {screen.StaminaBalance}";
                if (_statusLabel != null)
                    _statusLabel.text = screen.StatusMessage;
            }

            InitContent(screen);
        }

        private void BuildSkeleton(ShopScreen screen)
        {
            gameObject.AddComponent<Image>().color = TosTheme.BgDark;

            // ── Standardized Header ──
            var h = TosPageHelper.BuildHeader(this, transform,
                "Shop", new Color(0.7f, 0.5f, 1f), () => screen.GoBack());

            // ── Currency Bar ──
            var currArea = CreateRegion("Currencies", MFAnchor.Top,
                new MFPadding(TosTheme.ContentPadH, TosTheme.ContentTopOffset, TosTheme.ContentPadH, 0));
            currArea.sizeDelta = new Vector2(0, 24);
            var currHBox = CreateHBox("CurrRow", currArea, spacing: 4f);

            _gemsLabel = CreateCurrencyChip(currHBox, "Gems", screen.GemsBalance, TosTheme.TextGold);
            _coinsLabel = CreateCurrencyChip(currHBox, "Coins", screen.CoinsBalance, new Color(0.9f, 0.8f, 0.3f));
            _staminaLabel = CreateCurrencyChip(currHBox, "ST", screen.StaminaBalance, new Color(0.3f, 0.9f, 0.3f));

            // ── Status Label ──
            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(transform, false);
            var statusRect = statusGo.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.05f, 0.05f);
            statusRect.anchorMax = new Vector2(0.95f, 0.1f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
            _statusLabel = statusGo.AddComponent<Text>();
            _statusLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusLabel.fontSize = TosTheme.FontBody;
            _statusLabel.color = TosTheme.TextGold;
            _statusLabel.alignment = TextAnchor.MiddleCenter;
        }

        private void InitContent(ShopScreen screen)
        {
            // ── Tab Bar ──
            var tabArea = CreateRegion("TabArea", MFAnchor.Top,
                new MFPadding(40, TosTheme.ContentTopOffset + 28, 40, 0));
            tabArea.sizeDelta = new Vector2(0, 34);
            _tabBar = SpawnPrimitive<MFTabBar>(tabArea);
            _tabBar.SetTabs(
                new[] { "General", "Event" },
                new[] { TosTheme.TextGold, new Color(0.7f, 0.5f, 1f) }
            );
            _tabBar.OnTabSelected = (index, _) => SwitchTab(index);

            // ── General Tab Content ──
            var generalGo = new GameObject("GeneralContent");
            generalGo.transform.SetParent(transform, false);
            SetupContentArea(generalGo);
            _generalContent = generalGo.transform;
            BuildGeneralShop(generalGo.transform, screen);

            // ── Event Tab Content ──
            var eventGo = new GameObject("EventContent");
            eventGo.transform.SetParent(transform, false);
            SetupContentArea(eventGo);
            _eventContent = eventGo.transform;
            BuildEventShop(eventGo.transform);
            eventGo.SetActive(false);

            // Entrance animations
            var generalCg = _generalContent.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(MFUIAnim.FadeIn(generalCg, 0.5f));
        }

        private void SwitchTab(int index)
        {
            _generalContent.gameObject.SetActive(index == 0);
            _eventContent.gameObject.SetActive(index == 1);
        }

        private void BuildGeneralShop(Transform parent, ShopScreen screen)
        {
            var vbox = CreateVBox("ShopItems", parent.GetComponent<RectTransform>(), spacing: 10f);

            CreateShopItem(vbox, "Refill Stamina (+100)", "1 Gem",
                new Color(0.2f, 0.6f, 0.3f), () => { screen.RefillStamina(); Refresh(); });

            CreateShopItem(vbox, "Coin Pack (10,000 Coins)", "2 Gems",
                new Color(0.7f, 0.6f, 0.2f), () =>
                {
                    var ps = MobileForge.Infrastructure.PlayerState.Instance;
                    int gems = System.Convert.ToInt32(ps?.GetValue("currencies", "gems", 0) ?? 0);
                    if (gems >= 2)
                    {
                        ps?.SetValue("currencies", "gems", gems - 2);
                        int coins = System.Convert.ToInt32(ps?.GetValue("currencies", "coins", 0) ?? 0);
                        ps?.SetValue("currencies", "coins", coins + 10000);
                        _statusLabel.text = "Purchased 10,000 Coins!";
                    }
                    else _statusLabel.text = "Not enough gems!";
                    Refresh();
                });

            CreateShopItem(vbox, "Free Gems (+50)", "FREE",
                TosTheme.TextGold, () => { screen.AddFreeGems(); Refresh(); });

            CreateShopItem(vbox, "Free Event Tokens (+100)", "FREE",
                new Color(0.7f, 0.5f, 1f), () => { screen.AddFreeEventTokens(); Refresh(); });
        }

        private void BuildEventShop(Transform parent)
        {
            var vbox = CreateVBox("EventItems", parent.GetComponent<RectTransform>(), spacing: 8f);

            if (_screen.EventShops.Count == 0)
            {
                var noEventGo = new GameObject("NoEvent");
                noEventGo.transform.SetParent(vbox, false);
                noEventGo.AddComponent<LayoutElement>().preferredHeight = 40;
                var noEventText = noEventGo.AddComponent<Text>();
                noEventText.text = "No active event shops";
                noEventText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                noEventText.fontSize = TosTheme.FontBody;
                noEventText.color = TosTheme.TextMuted;
                noEventText.alignment = TextAnchor.MiddleCenter;
                return;
            }

            foreach (var shop in _screen.EventShops)
            {
                // Shop header
                var headerGo = new GameObject($"ShopHeader_{shop.Id}");
                headerGo.transform.SetParent(vbox, false);
                headerGo.AddComponent<LayoutElement>().preferredHeight = 28;
                var headerBg = headerGo.AddComponent<Image>();
                headerBg.color = new Color(0.15f, 0.1f, 0.25f, 0.9f);
                var headerText = new GameObject("Label");
                headerText.transform.SetParent(headerGo.transform, false);
                var htRect = headerText.AddComponent<RectTransform>();
                htRect.anchorMin = Vector2.zero; htRect.anchorMax = Vector2.one;
                htRect.offsetMin = new Vector2(8, 0); htRect.offsetMax = new Vector2(-8, 0);
                var ht = headerText.AddComponent<Text>();
                ht.text = $"{shop.Name}  ({shop.Currency}: {_screen.EventTokenBalance})";
                ht.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                ht.fontSize = TosTheme.FontBody;
                ht.color = new Color(0.7f, 0.5f, 1f);
                ht.alignment = TextAnchor.MiddleLeft;

                // Shop items
                foreach (var item in shop.Items)
                {
                    int shopId = shop.Id;
                    int itemId = item.Id;
                    string limitStr = item.BuyLimit > 0 ? $" [{item.TimesBought}/{item.BuyLimit}]" : "";
                    string costStr = $"{item.CostAmount} {item.CostCurrency}";

                    CreateShopItem(vbox, $"{item.Name} x{item.Count}{limitStr}", costStr,
                        item.IsSoldOut ? TosTheme.TextMuted : new Color(0.7f, 0.5f, 1f),
                        () =>
                        {
                            if (_screen.BuyEventItem(shopId, itemId))
                            {
                                // Rebuild event tab to reflect updated state
                                RebuildEventTab();
                            }
                            _statusLabel.text = _screen.StatusMessage;
                            RefreshCurrencies();
                        });
                }
            }
        }

        private void RebuildEventTab()
        {
            if (_eventContent == null) return;
            // Clear and rebuild
            for (int i = _eventContent.childCount - 1; i >= 0; i--)
                Destroy(_eventContent.GetChild(i).gameObject);
            BuildEventShop(_eventContent);
        }

        private void RefreshCurrencies()
        {
            if (_screen == null) return;
            _gemsLabel.text = $"Gems: {_screen.GemsBalance}";
            _coinsLabel.text = $"Coins: {_screen.CoinsBalance}";
            _staminaLabel.text = $"ST: {_screen.StaminaBalance}";
        }

        private void CreateShopItem(Transform parent, string label, string cost, Color accentColor, System.Action onClick)
        {
            var itemGo = new GameObject($"Item_{label}");
            itemGo.transform.SetParent(parent, false);
            var itemBg = itemGo.AddComponent<Image>();
            itemBg.color = TosTheme.BgPanel;
            var itemLe = itemGo.AddComponent<LayoutElement>();
            itemLe.preferredHeight = 52;

            var itemHBox = itemGo.AddComponent<HorizontalLayoutGroup>();
            itemHBox.padding = new RectOffset(8, 8, 4, 4);
            itemHBox.spacing = 8;
            itemHBox.childAlignment = TextAnchor.MiddleLeft;
            itemHBox.childForceExpandWidth = false;
            itemHBox.childForceExpandHeight = true;

            // Label
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(itemGo.transform, false);
            lblGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            var lblText = lblGo.AddComponent<Text>();
            lblText.text = label;
            lblText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lblText.fontSize = TosTheme.FontBody;
            lblText.color = Color.white;
            lblText.alignment = TextAnchor.MiddleLeft;

            // Cost
            var costGo = new GameObject("Cost");
            costGo.transform.SetParent(itemGo.transform, false);
            costGo.AddComponent<LayoutElement>().preferredWidth = 60;
            var costText = costGo.AddComponent<Text>();
            costText.text = cost;
            costText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            costText.fontSize = TosTheme.FontSmall;
            costText.color = accentColor;
            costText.alignment = TextAnchor.MiddleRight;

            // Buy button
            var btnGo = new GameObject("BuyBtn");
            btnGo.transform.SetParent(itemGo.transform, false);
            var btnBg = btnGo.AddComponent<Image>();
            btnBg.color = new Color(accentColor.r * 0.3f, accentColor.g * 0.3f, accentColor.b * 0.3f);
            var btnLe = btnGo.AddComponent<LayoutElement>();
            btnLe.preferredWidth = 60;
            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var btnLbl = new GameObject("BtnLabel");
            btnLbl.transform.SetParent(btnGo.transform, false);
            var blRect = btnLbl.AddComponent<RectTransform>();
            blRect.anchorMin = Vector2.zero;
            blRect.anchorMax = Vector2.one;
            blRect.offsetMin = Vector2.zero;
            blRect.offsetMax = Vector2.zero;
            var blText = btnLbl.AddComponent<Text>();
            blText.text = "Buy";
            blText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            blText.fontSize = TosTheme.FontBody;
            blText.color = Color.white;
            blText.alignment = TextAnchor.MiddleCenter;
        }

        private void SetupContentArea(GameObject go)
        {
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            // Account for header + currency bar + tab bar above, nav bar below
            rt.offsetMin = new Vector2(TosTheme.ContentPadH, TosTheme.ContentBottomNav);
            rt.offsetMax = new Vector2(-TosTheme.ContentPadH, -(TosTheme.ContentTopOffset + 62));
        }

        private Text CreateCurrencyChip(Transform parent, string type, int value, Color color)
        {
            var go = new GameObject($"{type}Chip");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().flexibleWidth = 1;
            var txt = go.AddComponent<Text>();
            txt.text = $"{type}: {value}";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = TosTheme.FontSmall;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            return txt;
        }

        protected override void OnRefresh()
        {
            if (_screen == null) return;
            _gemsLabel.text = $"Gems: {_screen.GemsBalance}";
            _coinsLabel.text = $"Coins: {_screen.CoinsBalance}";
            _staminaLabel.text = $"ST: {_screen.StaminaBalance}";
            _statusLabel.text = _screen.StatusMessage;
        }
    }
}

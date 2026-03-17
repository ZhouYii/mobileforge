using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Shop page. Stamina refill, free gems, and currency display.
    /// </summary>
    public class ShopPage : PageBase<ShopScreen>
    {
        private Text _gemsLabel;
        private Text _coinsLabel;
        private Text _staminaLabel;
        private Text _statusLabel;

        protected override void OnBind(ShopScreen screen)
        {
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // Header
            var header = CreateRegion("Header", MFAnchor.Top,
                new MFPadding(0, 0, 0, 0));
            header.sizeDelta = new Vector2(0, 60);
            var hbox = CreateHBox("HeaderContent", header, spacing: 8f);

            var backBtn = SpawnPrimitive<MFButton>(hbox);
            backBtn.gameObject.name = "btn_back";
            backBtn.SetLabel("Back");
            var backLayout = backBtn.gameObject.GetComponent<LayoutElement>();
            if (backLayout != null) { backLayout.preferredWidth = 120; backLayout.flexibleWidth = 0; }
            backBtn.OnClick = () => screen.GoBack();

            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(hbox, false);
            titleGO.AddComponent<RectTransform>();
            titleGO.AddComponent<LayoutElement>().flexibleWidth = 1;
            var titleText = titleGO.AddComponent<Text>();
            titleText.text = "Shop";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 36;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;

            // Currency display
            var currArea = CreateRegion("Currencies", MFAnchor.Top,
                new MFPadding(30, 80, 30, 0));
            currArea.sizeDelta = new Vector2(0, 100);
            var currVBox = CreateVBox("CurrencyList", currArea, spacing: 4f);

            _gemsLabel = CreateCurrencyLabel(currVBox, "Gems", screen.GemsBalance);
            _coinsLabel = CreateCurrencyLabel(currVBox, "Coins", screen.CoinsBalance);
            _staminaLabel = CreateCurrencyLabel(currVBox, "Stamina", screen.StaminaBalance);

            // Shop actions
            var actionArea = CreateRegion("Actions", MFAnchor.Center,
                new MFPadding(40, 0, 40, 0));
            var actionVBox = CreateVBox("ActionList", actionArea, spacing: 16f);

            var refillBtn = SpawnPrimitive<MFButton>(actionVBox);
            refillBtn.gameObject.name = "btn_refill";
            refillBtn.SetLabel("Refill Stamina (1 Gem)");
            refillBtn.SetColor(new Color(0.2f, 0.6f, 0.3f), Color.white);
            refillBtn.OnClick = () => { screen.RefillStamina(); Refresh(); };

            var freeGemsBtn = SpawnPrimitive<MFButton>(actionVBox);
            freeGemsBtn.gameObject.name = "btn_free_gems";
            freeGemsBtn.SetLabel("Free Gems (+50)");
            freeGemsBtn.SetColor(new Color(0.7f, 0.6f, 0.1f), Color.white);
            freeGemsBtn.OnClick = () => { screen.AddFreeGems(); Refresh(); };

            // Status message
            var statusGO = new GameObject("Status");
            statusGO.transform.SetParent(transform, false);
            var statusRect = statusGO.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0.15f);
            statusRect.anchorMax = new Vector2(1, 0.25f);
            statusRect.offsetMin = new Vector2(20, 0);
            statusRect.offsetMax = new Vector2(-20, 0);
            _statusLabel = statusGO.AddComponent<Text>();
            _statusLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _statusLabel.fontSize = 22;
            _statusLabel.color = Color.yellow;
            _statusLabel.alignment = TextAnchor.MiddleCenter;
        }

        protected override void OnRefresh()
        {
            if (_screen == null) return;
            _gemsLabel.text = $"Gems: {_screen.GemsBalance}";
            _coinsLabel.text = $"Coins: {_screen.CoinsBalance}";
            _staminaLabel.text = $"Stamina: {_screen.StaminaBalance}";
            _statusLabel.text = _screen.StatusMessage;
        }

        private Text CreateCurrencyLabel(Transform parent, string type, int value)
        {
            var go = new GameObject($"{type}Label");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredHeight = 28;
            var txt = go.AddComponent<Text>();
            txt.text = $"{type}: {value}";
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 24;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            return txt;
        }
    }
}

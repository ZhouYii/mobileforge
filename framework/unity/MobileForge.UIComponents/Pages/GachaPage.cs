using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Gacha pull page. Shows currency, pool selector, pull buttons, and results.
    /// </summary>
    public class GachaPage : PageBase<GachaScreen>
    {
        private MFCurrencyDisplay _gemsDisplay;
        private MFButton _pull1Btn;
        private MFButton _pull10Btn;
        private MFScrollGrid _resultsGrid;
        private Text _statusLabel;

        protected override void OnBind(GachaScreen screen)
        {
            // Overlay backdrop
            var overlay = SpawnPrimitive<MFOverlay>();
            overlay.Show(0.3f);

            // Header with currency
            var header = CreateRegion("Header", MFAnchor.Top,
                new MFPadding(10, 10, 10, 0));
            header.sizeDelta = new Vector2(0, 60);
            var headerHBox = CreateHBox("HeaderContent", header, spacing: 8f);

            var backBtn = SpawnPrimitive<MFButton>(headerHBox);
            backBtn.gameObject.name = "btn_back";
            backBtn.SetLabel("Back");
            var backLayout = backBtn.gameObject.GetComponent<LayoutElement>();
            if (backLayout != null) { backLayout.preferredWidth = 100; backLayout.flexibleWidth = 0; }
            backBtn.OnClick = () => screen.GoBack();

            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(headerHBox, false);
            titleGO.AddComponent<RectTransform>();
            titleGO.AddComponent<LayoutElement>().flexibleWidth = 1;
            var titleText = titleGO.AddComponent<Text>();
            titleText.text = "Gacha";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 32;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;

            _gemsDisplay = SpawnPrimitive<MFCurrencyDisplay>(headerHBox);
            _gemsDisplay.SetImmediate(screen.GemsBalance);

            // Pool info area
            var poolArea = CreateRegion("PoolArea", MFAnchor.Top,
                new MFPadding(20, 80, 20, 0));
            poolArea.sizeDelta = new Vector2(0, 80);
            if (screen.Pools.Count > 0)
            {
                var pool = screen.Pools[0];
                var poolGO = new GameObject("PoolInfo");
                poolGO.transform.SetParent(poolArea, false);
                var pRect = poolGO.AddComponent<RectTransform>();
                pRect.anchorMin = Vector2.zero;
                pRect.anchorMax = Vector2.one;
                pRect.offsetMin = Vector2.zero;
                pRect.offsetMax = Vector2.zero;
                var poolText = poolGO.AddComponent<Text>();
                poolText.text = $"{pool.Pool.Name ?? "Standard Pool"} — Cost: {pool.Pool.CostAmount} {pool.Pool.CostCurrency}/pull";
                poolText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                poolText.fontSize = 20;
                poolText.color = Color.white;
                poolText.alignment = TextAnchor.MiddleCenter;
            }

            // Pull buttons
            var btnArea = CreateRegion("PullButtons", MFAnchor.Top,
                new MFPadding(30, 170, 30, 0));
            btnArea.sizeDelta = new Vector2(0, 70);
            var btnHBox = CreateHBox("BtnRow", btnArea, spacing: 16f);

            _pull1Btn = SpawnPrimitive<MFButton>(btnHBox);
            _pull1Btn.gameObject.name = "btn_pull1";
            _pull1Btn.SetLabel("Pull x1");
            _pull1Btn.SetColor(new Color(0.3f, 0.5f, 0.9f), Color.white);
            _pull1Btn.OnClick = () =>
            {
                if (screen.Pools.Count > 0)
                {
                    screen.DoPull(screen.Pools[0].Pool, 1);
                    Refresh();
                }
            };

            _pull10Btn = SpawnPrimitive<MFButton>(btnHBox);
            _pull10Btn.gameObject.name = "btn_pull10";
            _pull10Btn.SetLabel("Pull x10");
            _pull10Btn.SetColor(new Color(0.8f, 0.5f, 0.1f), Color.white);
            _pull10Btn.OnClick = () =>
            {
                if (screen.Pools.Count > 0)
                {
                    screen.DoPull(screen.Pools[0].Pool, 10);
                    Refresh();
                }
            };

            // Status message
            var statusGO = new GameObject("Status");
            statusGO.transform.SetParent(transform, false);
            var statusRect = statusGO.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0.55f);
            statusRect.anchorMax = new Vector2(1, 0.6f);
            statusRect.offsetMin = new Vector2(20, 0);
            statusRect.offsetMax = new Vector2(-20, 0);
            _statusLabel = statusGO.AddComponent<Text>();
            _statusLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _statusLabel.fontSize = 20;
            _statusLabel.color = Color.yellow;
            _statusLabel.alignment = TextAnchor.MiddleCenter;

            // Results grid
            var resultsArea = CreateRegion("Results", MFAnchor.Fill,
                new MFPadding(10, 300, 10, 10));
            _resultsGrid = SpawnPrimitive<MFScrollGrid>(resultsArea);
            _resultsGrid.Setup(null, BindResultCell, columns: 5, cellSize: 90f);

            if (screen.LastResults.Count > 0)
                _resultsGrid.SetItems(screen.LastResults.Cast<object>().ToList());
        }

        private void BindResultCell(GameObject cell, object data, int index)
        {
            var result = data as GachaResultDisplay;
            if (result == null) return;

            var label = cell.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = result.MonsterName;
                label.color = result.IsFeatured ? Color.yellow
                    : result.IsPity ? Color.cyan : Color.white;
                label.fontSize = 11;
            }
        }

        protected override void OnRefresh()
        {
            if (_screen == null) return;
            _gemsDisplay.SetValue(_screen.GemsBalance);
            _statusLabel.text = _screen.StatusMessage;

            if (_screen.LastResults.Count > 0)
                _resultsGrid.SetItems(_screen.LastResults.Cast<object>().ToList());
        }
    }
}

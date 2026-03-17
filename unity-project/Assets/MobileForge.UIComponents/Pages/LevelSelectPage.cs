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
    /// Dungeon/level select page. Scrollable list of stages with stamina cost display.
    /// </summary>
    public class LevelSelectPage : PageBase<DungeonSelectScreen>
    {
        private MFScrollList _stageList;
        private UIRouter _router;

        public void SetRouter(UIRouter router) => _router = router;

        protected override void OnBind(DungeonSelectScreen screen)
        {
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // Header bar
            var header = CreateRegion("Header", MFAnchor.Top,
                new MFPadding(0, 0, 0, 0));
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, 70);

            var hbox = CreateHBox("HeaderContent", header, spacing: 8f);

            var backBtn = SpawnPrimitive<MFButton>(hbox);
            backBtn.gameObject.name = "btn_back";
            backBtn.SetLabel("Back");
            var backLayout = backBtn.gameObject.GetComponent<LayoutElement>();
            if (backLayout != null) { backLayout.preferredWidth = 120; backLayout.flexibleWidth = 0; }
            backBtn.OnClick = () => _router?.Pop();

            var headerLabel = new GameObject("HeaderLabel");
            headerLabel.transform.SetParent(hbox, false);
            headerLabel.AddComponent<RectTransform>();
            var hlLayout = headerLabel.AddComponent<LayoutElement>();
            hlLayout.flexibleWidth = 1;
            var hlText = headerLabel.AddComponent<Text>();
            hlText.text = "Dungeon Select";
            hlText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hlText.fontSize = 36;
            hlText.color = Color.white;
            hlText.alignment = TextAnchor.MiddleCenter;

            // Stage list
            var listArea = CreateRegion("ListArea", MFAnchor.Fill,
                new MFPadding(10, 80, 10, 10));
            _stageList = SpawnPrimitive<MFScrollList>(listArea);
            _stageList.Setup(null, BindStageCell, itemHeight: 70f);
            _stageList.SetItems(screen.Stages.Cast<object>().ToList());
        }

        private void BindStageCell(GameObject cell, object data, int index)
        {
            var stage = data as DungeonSelectScreen.StageEntry;
            if (stage == null) return;

            var label = cell.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = $"{stage.Name} ({stage.StaminaCost} ST)";
                label.color = stage.CanAfford ? Color.white : Color.gray;
            }

            var btn = cell.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    if (stage.CanAfford)
                        _screen.SelectStage(stage.StageId, stage.StaminaCost);
                });
            }
        }
    }
}

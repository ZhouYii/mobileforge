using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;
using MobileForge.Presentation;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Battle result page. Shows victory/defeat status and earned rewards.
    /// </summary>
    public class ResultPage : PageBase<ResultScreen>
    {
        private UIRouter _router;

        public void SetRouter(UIRouter router) => _router = router;

        protected override void OnBind(ResultScreen screen)
        {
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // Result text
            var resultArea = CreateRegion("ResultArea", MFAnchor.Top,
                new MFPadding(20, 80, 20, 0));
            resultArea.sizeDelta = new Vector2(0, 100);
            var resultGO = new GameObject("ResultLabel");
            resultGO.transform.SetParent(resultArea, false);
            var rRect = resultGO.AddComponent<RectTransform>();
            rRect.anchorMin = Vector2.zero;
            rRect.anchorMax = Vector2.one;
            rRect.offsetMin = Vector2.zero;
            rRect.offsetMax = Vector2.zero;
            var resultText = resultGO.AddComponent<Text>();
            resultText.text = screen.ResultText;
            resultText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            resultText.fontSize = 52;
            resultText.color = screen.Won ? Color.yellow : Color.red;
            resultText.alignment = TextAnchor.MiddleCenter;

            // Rewards list
            if (screen.Rewards.Count > 0)
            {
                var rewardArea = CreateRegion("Rewards", MFAnchor.Center,
                    new MFPadding(40, 0, 40, 0));
                var vbox = CreateVBox("RewardList", rewardArea, spacing: 8f);

                foreach (var reward in screen.Rewards)
                {
                    var rewardGO = new GameObject($"reward_{reward.Type}");
                    rewardGO.transform.SetParent(vbox, false);
                    rewardGO.AddComponent<RectTransform>();
                    rewardGO.AddComponent<LayoutElement>().preferredHeight = 30;
                    var txt = rewardGO.AddComponent<Text>();
                    txt.text = $"{reward.Type}: +{reward.Count}";
                    txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    txt.fontSize = 24;
                    txt.color = Color.white;
                    txt.alignment = TextAnchor.MiddleCenter;
                }
            }

            // Continue button
            var bottomArea = CreateRegion("BottomArea", MFAnchor.Bottom,
                new MFPadding(60, 0, 60, 40));
            bottomArea.sizeDelta = new Vector2(0, 70);
            var continueBtn = SpawnPrimitive<MFButton>(bottomArea);
            continueBtn.gameObject.name = "btn_continue";
            continueBtn.SetLabel("Continue");
            continueBtn.SetColor(new Color(0.2f, 0.6f, 0.9f), Color.white);
            continueBtn.OnClick = () => screen.OnContinue(_router);
        }
    }
}

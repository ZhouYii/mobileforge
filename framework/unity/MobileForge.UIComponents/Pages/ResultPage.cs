using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;
using MobileForge.Presentation;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Battle result page. Shows victory/defeat with themed styling,
    /// reward cards, and a continue button. Matches Godot result_screen.gd.
    /// </summary>
    public class ResultPage : PageBase<ResultScreen>
    {
        [Header("Prefab References")]
        [SerializeField] private Text _resultLabel;
        [SerializeField] private RectTransform _rewardsArea;
        [SerializeField] private Transform _rewardsContent;

        private UIRouter _router;

        public void SetRouter(UIRouter router) => _router = router;

        protected override void OnBind(ResultScreen screen)
        {
            if (!IsPrefabPage)
            {
                BuildSkeleton(screen);
            }
            else
            {
                if (_resultLabel != null)
                {
                    _resultLabel.text = screen.ResultText;
                    _resultLabel.color = screen.Won ? TosTheme.TextGold : new Color(1f, 0.3f, 0.3f);
                    if (screen.Won)
                        StartCoroutine(MFUIAnim.PopIn(_resultLabel.transform, TosTheme.AnimCardPop + 0.1f, 0.5f, TosTheme.CardPopScale));
                    else
                        StartCoroutine(MFUIAnim.Shake(_resultLabel.GetComponent<RectTransform>(), 0.5f, 10f));
                }
            }

            InitContent(screen);
        }

        private void BuildSkeleton(ResultScreen screen)
        {
            var bg = gameObject.AddComponent<Image>();
            bg.color = TosTheme.BgDark;

            // ── Result Title ──
            var resultArea = CreateRegion("ResultArea", MFAnchor.Top, new MFPadding(20, 60, 20, 0));
            resultArea.sizeDelta = new Vector2(0, 100);

            var resultGo = new GameObject("ResultLabel");
            resultGo.transform.SetParent(resultArea, false);
            var rRect = resultGo.AddComponent<RectTransform>();
            rRect.anchorMin = Vector2.zero;
            rRect.anchorMax = Vector2.one;
            rRect.offsetMin = Vector2.zero;
            rRect.offsetMax = Vector2.zero;

            var resultText = resultGo.AddComponent<Text>();
            resultText.text = screen.ResultText;
            resultText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            resultText.fontSize = 40;
            resultText.alignment = TextAnchor.MiddleCenter;

            var shadow = resultGo.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(2, -2);

            if (screen.Won)
            {
                resultText.color = TosTheme.TextGold;
                shadow.effectColor = new Color(0.4f, 0.3f, 0, 0.6f);
                StartCoroutine(MFUIAnim.PopIn(resultGo.transform, TosTheme.AnimCardPop + 0.1f, 0.5f, TosTheme.CardPopScale));
            }
            else
            {
                resultText.color = new Color(1f, 0.3f, 0.3f);
                shadow.effectColor = new Color(0.3f, 0, 0, 0.6f);
                StartCoroutine(MFUIAnim.Shake(rRect, 0.5f, 10f));
            }

            // ── Rewards Section Containers ──
            if (screen.Rewards.Count > 0)
            {
                var rewardHeader = CreateRegion("RewardHeader", MFAnchor.Top, new MFPadding(40, 170, 40, 0));
                rewardHeader.sizeDelta = new Vector2(0, 30);
                var rhGo = new GameObject("RewardTitle");
                rhGo.transform.SetParent(rewardHeader, false);
                var rhRect = rhGo.AddComponent<RectTransform>();
                rhRect.anchorMin = Vector2.zero;
                rhRect.anchorMax = Vector2.one;
                rhRect.offsetMin = Vector2.zero;
                rhRect.offsetMax = Vector2.zero;
                var rhText = rhGo.AddComponent<Text>();
                rhText.text = "Rewards";
                rhText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                rhText.fontSize = TosTheme.FontSubheader;
                rhText.color = TosTheme.TextMuted;
                rhText.alignment = TextAnchor.MiddleCenter;

                _rewardsArea = CreateRegion("Rewards", MFAnchor.Top, new MFPadding(30, 200, 30, 0));
                _rewardsArea.sizeDelta = new Vector2(0, Mathf.Min(screen.Rewards.Count * 50, 200));
                _rewardsContent = CreateVBox("RewardList", _rewardsArea, spacing: 6f);
            }
            else if (!screen.Won)
            {
                var noRewardArea = CreateRegion("NoReward", MFAnchor.Center, new MFPadding(40, 0, 40, 0));
                var nrGo = new GameObject("NoRewardLabel");
                nrGo.transform.SetParent(noRewardArea, false);
                var nrRect = nrGo.AddComponent<RectTransform>();
                nrRect.anchorMin = Vector2.zero;
                nrRect.anchorMax = Vector2.one;
                nrRect.offsetMin = Vector2.zero;
                nrRect.offsetMax = Vector2.zero;
                var nrText = nrGo.AddComponent<Text>();
                nrText.text = "Better luck next time...";
                nrText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nrText.fontSize = TosTheme.FontBody;
                nrText.color = TosTheme.TextMuted;
                nrText.alignment = TextAnchor.MiddleCenter;
            }
        }

        private void InitContent(ResultScreen screen)
        {
            // ── Reward Items ──
            if (_rewardsContent != null && screen.Rewards.Count > 0)
            {
                var rewardCGs = new List<CanvasGroup>();
                foreach (var reward in screen.Rewards)
                {
                    var rewardGo = new GameObject($"reward_{reward.Type}");
                    rewardGo.transform.SetParent(_rewardsContent, false);
                    rewardGo.AddComponent<Image>().color = TosTheme.BgPanel;
                    rewardGo.AddComponent<LayoutElement>().preferredHeight = 40;
                    var txt = new GameObject("Label");
                    txt.transform.SetParent(rewardGo.transform, false);
                    var txtRect = txt.AddComponent<RectTransform>();
                    txtRect.anchorMin = Vector2.zero;
                    txtRect.anchorMax = Vector2.one;
                    txtRect.offsetMin = new Vector2(12, 0);
                    txtRect.offsetMax = new Vector2(-12, 0);
                    var label = txt.AddComponent<Text>();
                    label.text = $"  {reward.Type}  x{reward.Count}";
                    label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    label.fontSize = TosTheme.FontBody + 2;
                    label.color = Color.white;
                    label.alignment = TextAnchor.MiddleLeft;
                    var cg = rewardGo.AddComponent<CanvasGroup>();
                    cg.alpha = 0;
                    rewardCGs.Add(cg);
                }
                if (rewardCGs.Count > 0)
                    StartCoroutine(MFUIAnim.StaggerFadeIn(rewardCGs, 0.25f, 0.1f));
            }

            // ── Continue Button ──
            var bottomArea = CreateRegion("BottomArea", MFAnchor.Bottom, new MFPadding(60, 0, 60, 40));
            bottomArea.sizeDelta = new Vector2(0, 70);
            var continueBtn = SpawnPrimitive<MFButton>(bottomArea);
            continueBtn.gameObject.name = "btn_continue";
            continueBtn.SetLabel("Continue");
            continueBtn.SetColor(TosTheme.ElementButtonBg(screen.Won ? 1 : 0), Color.white);
            continueBtn.OnClick = () => screen.OnContinue(_router);
            var btnCg = continueBtn.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(MFUIAnim.DelayedFadeIn(btnCg, 0.8f, 0.3f));
        }
    }
}

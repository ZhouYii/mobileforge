using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Pages;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// In-battle overlay menu matching original Overlay_GamePlay_MainMenu_View.
    /// Slides in from the right with stage info and options:
    /// Settings, Rewards, Quit Game, Cancel (matching original menu order).
    /// Original slides contentContainer from x=960 to x=320 via iTween.
    /// </summary>
    public class BattleOverlayMenu : MonoBehaviour
    {
        public Action OnResume;
        public Action OnSurrender;
        public Action OnSettings;
        public Action OnTeamInfo;
        public Action OnRewards;

        /// <summary>Set these before calling Setup to show stage context.</summary>
        public string StageName { get; set; } = "";
        public string FloorName { get; set; } = "";
        public string BattleProgress { get; set; } = "";
        public int RoundNumber { get; set; }

        private CanvasGroup _canvasGroup;
        private RectTransform _panel;

        public void Setup()
        {
            var rt = gameObject.GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;

            // Dark backdrop (tap to dismiss, matching original BackPressed)
            var backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(transform, false);
            var bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.offsetMin = Vector2.zero;
            bdRect.offsetMax = Vector2.zero;
            backdrop.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            backdrop.AddComponent<Button>().onClick.AddListener(Close);

            // Slide-in panel (right side, matching original iTween slide from 960 to 320)
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(transform, false);
            _panel = panelGo.AddComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.33f, 0.12f);
            _panel.anchorMax = new Vector2(0.96f, 0.88f);
            _panel.offsetMin = Vector2.zero;
            _panel.offsetMax = Vector2.zero;
            panelGo.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.1f, 0.97f);

            var vbox = panelGo.AddComponent<VerticalLayoutGroup>();
            vbox.padding = new RectOffset(14, 14, 14, 14);
            vbox.spacing = 8;
            vbox.childAlignment = TextAnchor.UpperCenter;
            vbox.childForceExpandWidth = true;
            vbox.childForceExpandHeight = false;

            // ── Stage Info (matching original stageName_TM, floorName_TM, etc.) ──
            if (!string.IsNullOrEmpty(StageName))
                TosPageHelper.MakeLabel(panelGo.transform, StageName,
                    TosTheme.FontSubheader, Color.white, 24, TextAnchor.MiddleCenter);
            if (!string.IsNullOrEmpty(FloorName))
                TosPageHelper.MakeLabel(panelGo.transform, FloorName,
                    TosTheme.FontBody, TosTheme.TextMuted, 18, TextAnchor.MiddleCenter);
            if (!string.IsNullOrEmpty(BattleProgress))
                TosPageHelper.MakeLabel(panelGo.transform, BattleProgress,
                    TosTheme.FontBody, new Color(0.8f, 0.8f, 0.5f), 18, TextAnchor.MiddleCenter);
            if (RoundNumber > 0)
                TosPageHelper.MakeLabel(panelGo.transform, $"Round {RoundNumber}",
                    TosTheme.FontSmall, TosTheme.TextMuted, 16, TextAnchor.MiddleCenter);

            TosPageHelper.MakeDivider(panelGo.transform);

            // ── Menu Options (matching original order: Settings, Rewards, Quit, Cancel) ──
            CreateOption(panelGo.transform, "Settings", new Color(0.3f, 0.5f, 0.7f), () =>
            {
                OnSettings?.Invoke();
            });
            CreateOption(panelGo.transform, "Rewards", new Color(0.5f, 0.5f, 0.3f), () =>
            {
                OnRewards?.Invoke();
            });
            CreateOption(panelGo.transform, "Team Info", new Color(0.3f, 0.6f, 0.5f), () =>
            {
                OnTeamInfo?.Invoke();
            });

            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(panelGo.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleHeight = 1;

            // Quit (red, with confirmation dialog matching original _QuitGameConfirmation)
            CreateOption(panelGo.transform, "Quit Game", new Color(0.7f, 0.2f, 0.2f), () =>
            {
                UIServices.Popups?.ShowCustomDialog("quit_confirm", dialog =>
                {
                    dialog.SetTitle("Quit Game");
                    dialog.SetMessage("Are you sure you want to give up?\nYou will lose all progress.");
                    dialog.AddButton("OK", () =>
                    {
                        dialog.Dismiss();
                        Close();
                        OnSurrender?.Invoke();
                    }, new Color(0.7f, 0.2f, 0.2f));
                    dialog.AddButton("Cancel", () => dialog.Dismiss(), new Color(0.3f, 0.3f, 0.35f));
                });
            });

            // Cancel (dismiss overlay, matching original BackPressed)
            CreateOption(panelGo.transform, "Cancel", new Color(0.4f, 0.4f, 0.45f), () =>
            {
                Close();
                OnResume?.Invoke();
            });
        }

        private void CreateOption(Transform parent, string label, Color accentColor, Action onClick)
        {
            var go = new GameObject($"Opt_{label}");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(accentColor.r * 0.2f, accentColor.g * 0.2f,
                accentColor.b * 0.2f, 0.9f);
            go.AddComponent<LayoutElement>().preferredHeight = 44;

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            TosPageHelper.MakeLabel(go.transform, label, TosTheme.FontBody + 1,
                accentColor, 0, TextAnchor.MiddleCenter)
                .gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;

            TosPageHelper.AddPressFeedback(go);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            StartCoroutine(AnimateOpen());
        }

        public void Close()
        {
            StartCoroutine(AnimateClose());
        }

        private IEnumerator AnimateOpen()
        {
            // Slide panel from right + fade backdrop (matching original iTween)
            Vector2 targetPos = _panel.anchoredPosition;
            _panel.anchoredPosition = targetPos + new Vector2(400, 0);
            float t = 0;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / 0.3f);
                float ease = 1 - Mathf.Pow(1 - p, 3);
                _canvasGroup.alpha = ease;
                _panel.anchoredPosition = Vector2.Lerp(targetPos + new Vector2(400, 0), targetPos, ease);
                yield return null;
            }
            _canvasGroup.alpha = 1;
            _panel.anchoredPosition = targetPos;
        }

        private IEnumerator AnimateClose()
        {
            Vector2 startPos = _panel.anchoredPosition;
            float t = 0;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / 0.2f);
                _canvasGroup.alpha = 1 - p;
                _panel.anchoredPosition = startPos + new Vector2(400 * p, 0);
                yield return null;
            }
            gameObject.SetActive(false);
            _panel.anchoredPosition = startPos;
        }
    }
}

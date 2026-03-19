using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.Presentation;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// Persistent bottom navigation bar matching original TOS MenuLayer footer.
    /// Shows tabs for main game sections with icons, labels, and badge dots.
    /// Auto-hides during battle/team_select screens.
    /// </summary>
    public class MainNavBar : MonoBehaviour
    {
        public const float NavBarHeight = 56f;
        private static readonly Color BgColor = new(0.06f, 0.06f, 0.1f, 0.96f);
        private static readonly Color ActiveColor = TosTheme.TextGold;
        private static readonly Color InactiveColor = new(0.45f, 0.45f, 0.5f);
        private static readonly Color DividerColor = new(0.2f, 0.2f, 0.25f);

        // Tab definitions matching original TOS bottom bar
        private static readonly NavTab[] Tabs = new[]
        {
            new NavTab { Id = "world_map",       Label = "Quest",    Icon = "\u2694" }, // ⚔
            new NavTab { Id = "team_manage",    Label = "Team",     Icon = "\u2638" }, // ☸
            new NavTab { Id = "monster_box",    Label = "Box",      Icon = "\u2726" }, // ✦
            new NavTab { Id = "gacha",          Label = "Gacha",    Icon = "\u2605" }, // ★
            new NavTab { Id = "shop",           Label = "Shop",     Icon = "\u2302" }, // ⌂
            new NavTab { Id = "social",         Label = "Social",   Icon = "\u263A" }, // ☺
            new NavTab { Id = "settings",       Label = "Settings", Icon = "\u2699" }, // ⚙
        };

        // Screens where the nav bar should be hidden
        private static readonly HashSet<string> HiddenScreens = new()
        {
            "battle", "team_select", "result"
        };

        private struct NavTab
        {
            public string Id;
            public string Label;
            public string Icon;
        }

        private UIRouter _router;
        private readonly List<GameObject> _tabObjects = new();
        private readonly List<Text> _iconTexts = new();
        private readonly List<Text> _labelTexts = new();
        private readonly List<Image> _indicators = new();
        private readonly List<GameObject> _badgeDots = new();
        private int _activeIndex = -1;
        private CanvasGroup _canvasGroup;
        private RectTransform _rect;
        private bool _isVisible = true;

        public Action<string> OnTabSelected;

        public void Setup(UIRouter router)
        {
            _router = router;

            // Configure rect
            _rect = gameObject.GetComponent<RectTransform>();
            if (_rect == null) _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0, 0);
            _rect.anchorMax = new Vector2(1, 0);
            _rect.pivot = new Vector2(0.5f, 0);
            _rect.sizeDelta = new Vector2(0, NavBarHeight);
            _rect.anchoredPosition = Vector2.zero;

            // Canvas group for fade
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Background
            var bg = gameObject.AddComponent<Image>();
            bg.color = BgColor;

            // Top divider line
            var divider = new GameObject("Divider");
            divider.transform.SetParent(transform, false);
            var divRect = divider.AddComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0, 1);
            divRect.anchorMax = Vector2.one;
            divRect.sizeDelta = new Vector2(0, 1);
            divRect.anchoredPosition = Vector2.zero;
            divider.AddComponent<Image>().color = DividerColor;

            // Tab container
            var tabContainer = new GameObject("Tabs");
            tabContainer.transform.SetParent(transform, false);
            var tcRect = tabContainer.AddComponent<RectTransform>();
            tcRect.anchorMin = Vector2.zero;
            tcRect.anchorMax = Vector2.one;
            tcRect.offsetMin = new Vector2(0, 0);
            tcRect.offsetMax = new Vector2(0, -1); // below divider

            var layout = tabContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 0;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(2, 2, 2, 4);

            // Build tabs
            for (int i = 0; i < Tabs.Length; i++)
            {
                int index = i;
                CreateTab(tabContainer.transform, Tabs[i], index);
            }

            // Subscribe to navigation events
            router.OnNavigated += OnScreenChanged;

            // Set initial state
            UpdateActiveByScreenId(router.CurrentScreenId ?? "title");
        }

        private void CreateTab(Transform parent, NavTab tab, int index)
        {
            var tabGo = new GameObject($"Tab_{tab.Id}");
            tabGo.transform.SetParent(parent, false);
            var tabRect = tabGo.AddComponent<RectTransform>();

            // Clickable background
            var tabBg = tabGo.AddComponent<Image>();
            tabBg.color = Color.clear;
            var btn = tabGo.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                SetActive(index);
                OnTabSelected?.Invoke(tab.Id);
            });

            // Active indicator (small bar at top)
            var indicator = new GameObject("Indicator");
            indicator.transform.SetParent(tabGo.transform, false);
            var indRect = indicator.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0.2f, 1);
            indRect.anchorMax = new Vector2(0.8f, 1);
            indRect.sizeDelta = new Vector2(0, 2.5f);
            indRect.anchoredPosition = new Vector2(0, -1);
            var indImg = indicator.AddComponent<Image>();
            indImg.color = Color.clear;
            _indicators.Add(indImg);

            // Vertical layout for icon + label
            var content = new GameObject("Content");
            content.transform.SetParent(tabGo.transform, false);
            var cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = Vector2.zero;
            cRect.anchorMax = Vector2.one;
            cRect.offsetMin = new Vector2(0, 2);
            cRect.offsetMax = new Vector2(0, -4);
            var cLayout = content.AddComponent<VerticalLayoutGroup>();
            cLayout.spacing = 1;
            cLayout.childAlignment = TextAnchor.MiddleCenter;
            cLayout.childForceExpandWidth = true;
            cLayout.childForceExpandHeight = false;
            cLayout.childControlHeight = true;

            // Icon
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(content.transform, false);
            iconGo.AddComponent<LayoutElement>().preferredHeight = 22;
            var iconText = iconGo.AddComponent<Text>();
            iconText.text = tab.Icon;
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.fontSize = 20;
            iconText.color = InactiveColor;
            iconText.alignment = TextAnchor.MiddleCenter;
            _iconTexts.Add(iconText);

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(content.transform, false);
            labelGo.AddComponent<LayoutElement>().preferredHeight = 14;
            var labelText = labelGo.AddComponent<Text>();
            labelText.text = tab.Label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 9;
            labelText.color = InactiveColor;
            labelText.alignment = TextAnchor.MiddleCenter;
            _labelTexts.Add(labelText);

            // Badge dot (hidden by default)
            var badgeGo = new GameObject("Badge");
            badgeGo.transform.SetParent(tabGo.transform, false);
            var badgeRect = badgeGo.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.65f, 0.7f);
            badgeRect.anchorMax = new Vector2(0.65f, 0.7f);
            badgeRect.sizeDelta = new Vector2(10, 10);
            var badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.color = new Color(1f, 0.2f, 0.2f);
            // Make it round-ish via high font size in a circle
            badgeGo.SetActive(false);
            _badgeDots.Add(badgeGo);

            _tabObjects.Add(tabGo);
        }

        public void SetActive(int index, bool silent = false)
        {
            if (index < 0 || index >= Tabs.Length) return;
            _activeIndex = index;

            for (int i = 0; i < Tabs.Length; i++)
            {
                bool active = i == index;
                _iconTexts[i].color = active ? ActiveColor : InactiveColor;
                _labelTexts[i].color = active ? ActiveColor : InactiveColor;
                _labelTexts[i].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                _indicators[i].color = active ? ActiveColor : Color.clear;

                // Subtle scale feedback
                _tabObjects[i].transform.localScale = active
                    ? new Vector3(1.05f, 1.05f, 1f)
                    : Vector3.one;
            }
        }

        public void SetBadge(string tabId, bool show)
        {
            for (int i = 0; i < Tabs.Length; i++)
            {
                if (Tabs[i].Id == tabId)
                {
                    _badgeDots[i].SetActive(show);
                    break;
                }
            }
        }

        private void OnScreenChanged(string screenId, Dictionary<string, object> parameters)
        {
            // Hide during battle screens
            bool shouldHide = HiddenScreens.Contains(screenId);
            if (shouldHide && _isVisible)
                StartCoroutine(AnimateHide());
            else if (!shouldHide && !_isVisible)
                StartCoroutine(AnimateShow());

            // Update active tab
            if (!shouldHide)
                UpdateActiveByScreenId(screenId);
        }

        private void UpdateActiveByScreenId(string screenId)
        {
            for (int i = 0; i < Tabs.Length; i++)
            {
                if (Tabs[i].Id == screenId)
                {
                    SetActive(i, silent: true);
                    return;
                }
            }
            // Title screen → highlight Quest tab
            if (screenId == "title")
                SetActive(-1, silent: true); // none active on title
        }

        private IEnumerator AnimateHide()
        {
            _isVisible = false;
            float t = 0;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / 0.2f);
                _canvasGroup.alpha = 1 - p;
                _rect.anchoredPosition = new Vector2(0, -NavBarHeight * p);
                yield return null;
            }
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private IEnumerator AnimateShow()
        {
            gameObject.SetActive(true);
            _isVisible = true;
            _canvasGroup.blocksRaycasts = true;
            float t = 0;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / 0.25f);
                // Ease out
                float ease = 1 - Mathf.Pow(1 - p, 3);
                _canvasGroup.alpha = ease;
                _rect.anchoredPosition = new Vector2(0, -NavBarHeight * (1 - ease));
                yield return null;
            }
            _canvasGroup.alpha = 1;
            _rect.anchoredPosition = Vector2.zero;
        }

        void OnDestroy()
        {
            if (_router != null)
                _router.OnNavigated -= OnScreenChanged;
        }
    }
}

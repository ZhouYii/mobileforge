using UnityEngine;
using UnityEngine.UI;
using MobileForge.Presentation;
using MobileForge.UIComponents.Primitives;
using TowerOfSaviors;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Generic fallback page for unimplemented screens.
    /// Shows the screen ID with themed styling and a "Coming Soon" message.
    /// Uses TosPageHelper for consistent header/layout matching all other pages.
    /// </summary>
    public class PlaceholderPage : PageBase<IScreen>
    {
        [Header("Prefab References")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private RectTransform _contentArea;
        [SerializeField] private Text _iconText;
        [SerializeField] private Text _screenLabel;
        [SerializeField] private Text _subtitleText;

        private string _screenId;
        private UIRouter _router;

        public void SetScreenId(string screenId) => _screenId = screenId;
        public void SetRouter(UIRouter router) => _router = router;

        protected override void OnBind(IScreen screen)
        {
            if (!IsPrefabPage)
            {
                BuildSkeleton(screen);
            }
            else
            {
                string displayName = _screenId != null
                    ? System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_screenId.Replace("_", " "))
                    : "Unknown";

                if (_backButton != null)
                    _backButton.onClick.AddListener(() => _router?.Pop());
                if (_titleLabel != null)
                    _titleLabel.text = displayName;
                if (_iconText != null)
                    _iconText.text = GetScreenIcon();
                if (_screenLabel != null)
                    _screenLabel.text = displayName;
                if (_subtitleText != null)
                    _subtitleText.text = "Coming Soon";
            }

            InitContent(screen);
        }

        private void BuildSkeleton(IScreen screen)
        {
            gameObject.AddComponent<Image>().color = TosTheme.BgDark;

            string displayName = _screenId != null
                ? System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_screenId.Replace("_", " "))
                : "Unknown";

            // ── Standardized Header ──
            TosPageHelper.BuildHeader(this, transform,
                displayName, TosTheme.TextMuted, () => _router?.Pop());

            // ── Content ──
            var contentArea = TosPageHelper.BuildContentArea(transform);
            _contentArea = contentArea;

            // Decorative icon
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(contentArea, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.2f, 0.45f);
            iconRect.anchorMax = new Vector2(0.8f, 0.7f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            var iconText = iconGo.AddComponent<Text>();
            iconText.text = GetScreenIcon();
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.fontSize = 48;
            iconText.color = new Color(0.4f, 0.4f, 0.5f);
            iconText.alignment = TextAnchor.MiddleCenter;

            // Screen name
            var labelGo = new GameObject("ScreenLabel");
            labelGo.transform.SetParent(contentArea, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.1f, 0.3f);
            labelRect.anchorMax = new Vector2(0.9f, 0.45f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var txt = labelGo.AddComponent<Text>();
            txt.text = displayName;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = TosTheme.FontTitle;
            txt.color = TosTheme.TextMuted;
            txt.alignment = TextAnchor.MiddleCenter;
            labelGo.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.5f);

            // Coming soon subtitle
            var subGo = new GameObject("Subtitle");
            subGo.transform.SetParent(contentArea, false);
            var subRect = subGo.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.1f, 0.22f);
            subRect.anchorMax = new Vector2(0.9f, 0.3f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;
            var subText = subGo.AddComponent<Text>();
            subText.text = "Coming Soon";
            subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subText.fontSize = TosTheme.FontSubheader;
            subText.color = TosTheme.TextGold;
            subText.alignment = TextAnchor.MiddleCenter;
        }

        private void InitContent(IScreen screen)
        {
            // Entrance animation
            if (_contentArea != null)
            {
                var cg = _contentArea.gameObject.GetComponent<CanvasGroup>();
                if (cg == null) cg = _contentArea.gameObject.AddComponent<CanvasGroup>();
                StartCoroutine(MFUIAnim.FadeIn(cg, 0.5f));
            }
        }

        private string GetScreenIcon()
        {
            return _screenId switch
            {
                "achievements" => "\u2606",  // ☆
                "pvp" => "\u2694",            // ⚔
                "friends" => "\u263A",        // ☺
                "rankings" => "\u265B",       // ♛
                "profile" => "\u2302",        // ⌂
                _ => "\u2026"                 // …
            };
        }
    }
}

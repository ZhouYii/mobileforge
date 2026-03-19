using System;
using UnityEngine;
using UnityEngine.UI;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents
{
    /// <summary>
    /// Theme-agnostic layout helpers for building page UI in code.
    /// All visual parameters (colors, sizes, spacing) are passed via config structs,
    /// so this helper works across games without depending on any game-specific theme.
    /// </summary>
    public static class MFPageHelper
    {
        // ── Config structs ──

        public struct HeaderConfig
        {
            public float Height;
            public Color BgColor;
            public Color TitleColor;
            public Color BackBtnBgColor;
            public Color BackBtnTextColor;
            public float BackBtnWidth;
            public float BackBtnHeight;
            public int BackBtnFontSize;
            public int TitleFontSize;
            public float Spacing;
            public RectOffset Padding;
        }

        public struct ContentConfig
        {
            public float TopOffset;
            public float BottomOffset;
            public float HorizontalPad;
        }

        public struct HeaderResult
        {
            public RectTransform Header;
            public HorizontalLayoutGroup HBox;
            public MFButton BackBtn;
            public Text TitleText;
        }

        // ── Header ──

        /// <summary>
        /// Build a standardized page header with back button, title, and horizontal layout.
        /// Automatically adds slide-in animation if <paramref name="animate"/> is true.
        /// </summary>
        public static HeaderResult BuildHeader(MonoBehaviour page, Transform parent,
            string title, HeaderConfig config, Action onBack, bool animate = true)
        {
            var result = new HeaderResult();

            // Header region — anchored to top, fixed height
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(parent, false);
            result.Header = headerGo.AddComponent<RectTransform>();
            result.Header.anchorMin = new Vector2(0, 1);
            result.Header.anchorMax = Vector2.one;
            result.Header.pivot = new Vector2(0.5f, 1);
            result.Header.sizeDelta = new Vector2(0, config.Height);
            result.Header.anchoredPosition = Vector2.zero;

            // Background
            headerGo.AddComponent<Image>().color = config.BgColor;

            // Horizontal layout
            result.HBox = headerGo.AddComponent<HorizontalLayoutGroup>();
            result.HBox.spacing = config.Spacing;
            result.HBox.padding = config.Padding ?? new RectOffset(6, 6, 4, 4);
            result.HBox.childAlignment = TextAnchor.MiddleLeft;
            result.HBox.childForceExpandWidth = false;
            result.HBox.childForceExpandHeight = true;
            result.HBox.childControlWidth = true;
            result.HBox.childControlHeight = true;

            // Back button
            if (onBack != null)
            {
                result.BackBtn = MFPrimitiveLibrary.Spawn<MFButton>(headerGo.transform);
                result.BackBtn.gameObject.name = "btn_back";
                result.BackBtn.SetLabel("\u25C0"); // ◀
                result.BackBtn.SetColor(config.BackBtnBgColor, config.BackBtnTextColor);
                var backLe = result.BackBtn.gameObject.GetComponent<LayoutElement>();
                if (backLe != null)
                {
                    backLe.preferredWidth = config.BackBtnWidth;
                    backLe.preferredHeight = config.BackBtnHeight;
                    backLe.flexibleWidth = 0;
                }
                var backLabel = result.BackBtn.GetComponentInChildren<Text>();
                if (backLabel != null) backLabel.fontSize = config.BackBtnFontSize;
                result.BackBtn.OnClick = onBack;
            }

            // Title text — fills remaining space
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(headerGo.transform, false);
            titleGo.AddComponent<RectTransform>();
            titleGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            result.TitleText = titleGo.AddComponent<Text>();
            result.TitleText.text = title;
            result.TitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            result.TitleText.fontSize = config.TitleFontSize;
            result.TitleText.color = config.TitleColor;
            result.TitleText.alignment = TextAnchor.MiddleCenter;

            // Entrance animation
            if (animate)
                page.StartCoroutine(MFUIAnim.SlideIn(result.Header, new Vector2(0, config.Height), 0.3f));

            return result;
        }

        // ── Content area ──

        /// <summary>
        /// Create a content area that accounts for header/nav bar offsets and horizontal padding.
        /// </summary>
        public static RectTransform BuildContentArea(Transform parent, ContentConfig config,
            string name = "Content")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(config.HorizontalPad, config.BottomOffset);
            rect.offsetMax = new Vector2(-config.HorizontalPad, -config.TopOffset);
            return rect;
        }

        // ── Scroll content ──

        /// <summary>
        /// Build a scrollable content area with viewport, vertical layout, and content size fitter.
        /// Returns the content Transform to parent items into.
        /// </summary>
        public static Transform BuildScrollContent(Transform parent, float spacing = 6f)
        {
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(parent, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.vertical = true;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.clear;
            scroll.viewport = vpRect;

            var content = new GameObject("Items");
            content.transform.SetParent(viewport.transform, false);
            var cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            var cLayout = content.AddComponent<VerticalLayoutGroup>();
            cLayout.spacing = spacing;
            cLayout.padding = new RectOffset(0, 0, 4, 4);
            cLayout.childForceExpandWidth = true;
            cLayout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = cRect;

            return content.transform;
        }

        // ── Primitives ──

        /// <summary>
        /// Create a text label with a layout element for height.
        /// </summary>
        public static Text MakeLabel(Transform parent, string text, int fontSize, Color color,
            float height, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            return txt;
        }

        /// <summary>
        /// Create a horizontal divider line.
        /// </summary>
        public static GameObject MakeDivider(Transform parent, Color color, float height = 1f)
        {
            var go = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            go.AddComponent<Image>().color = color;
            return go;
        }

        /// <summary>
        /// Create an action button with a tinted background and centered label.
        /// </summary>
        public static Button MakeActionButton(Transform parent, string label,
            Color accentColor, int fontSize, Action onClick, float? width = null)
        {
            var btnGo = new GameObject($"Btn_{label}");
            btnGo.transform.SetParent(parent, false);
            var btnBg = btnGo.AddComponent<Image>();
            btnBg.color = new Color(accentColor.r * 0.25f, accentColor.g * 0.25f,
                accentColor.b * 0.25f, 0.92f);
            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var le = btnGo.AddComponent<LayoutElement>();
            if (width.HasValue)
                le.preferredWidth = width.Value;
            else
                le.flexibleWidth = 1;

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(btnGo.transform, false);
            var lblRect = lblGo.AddComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.offsetMin = new Vector2(4, 0);
            lblRect.offsetMax = new Vector2(-4, 0);
            var lblText = lblGo.AddComponent<Text>();
            lblText.text = label;
            lblText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lblText.fontSize = fontSize;
            lblText.color = accentColor;
            lblText.alignment = TextAnchor.MiddleCenter;

            return btn;
        }

        /// <summary>
        /// Add press-scale feedback to a GameObject (scale down on press, restore on release).
        /// </summary>
        public static void AddPressFeedback(GameObject go, float pressScale = 0.95f)
        {
            var trigger = go.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null) trigger = go.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var down = new UnityEngine.EventSystems.EventTrigger.Entry
                { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
            down.callback.AddListener(_ => go.transform.localScale = Vector3.one * pressScale);
            trigger.triggers.Add(down);

            var up = new UnityEngine.EventSystems.EventTrigger.Entry
                { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
            up.callback.AddListener(_ => go.transform.localScale = Vector3.one);
            trigger.triggers.Add(up);
        }
    }
}

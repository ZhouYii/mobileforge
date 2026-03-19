using System;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// TOS-themed UI building helpers. Delegates to MFPageHelper with TosTheme values.
    /// Existing API preserved — all code-built pages compile unchanged.
    /// </summary>
    public static class TosPageHelper
    {
        /// <summary>Result of BuildHeader containing the created UI elements.</summary>
        public struct HeaderResult
        {
            public RectTransform Header;
            public HorizontalLayoutGroup HBox;
            public MFButton BackBtn;
            public Text TitleText;
        }

        private static MFPageHelper.HeaderConfig DefaultHeaderConfig(Color titleColor) => new()
        {
            Height = TosTheme.HeaderHeight,
            BgColor = TosTheme.HeaderBg,
            TitleColor = titleColor,
            BackBtnBgColor = TosTheme.BackBtnBg,
            BackBtnTextColor = TosTheme.BackBtnText,
            BackBtnWidth = TosTheme.BackBtnWidth,
            BackBtnHeight = TosTheme.BackBtnHeight,
            BackBtnFontSize = TosTheme.BackBtnFontSize,
            TitleFontSize = TosTheme.FontHeader,
            Spacing = 6f,
            Padding = new RectOffset(6, 6, 4, 4),
        };

        private static MFPageHelper.ContentConfig DefaultContentConfig(
            float extraTopPad, float extraBottomPad, bool hasNavBar) => new()
        {
            TopOffset = TosTheme.ContentTopOffset + extraTopPad,
            BottomOffset = (hasNavBar ? TosTheme.ContentBottomNav : 0f) + extraBottomPad,
            HorizontalPad = TosTheme.ContentPadH,
        };

        /// <summary>
        /// Build a standardized page header with back button, title, and optional right widget.
        /// Returns the header RectTransform. Automatically adds slide-in animation.
        /// </summary>
        public static HeaderResult BuildHeader(MonoBehaviour page, Transform parent,
            string title, Color titleColor, Action onBack, bool animate = true)
        {
            var mfResult = MFPageHelper.BuildHeader(page, parent, title,
                DefaultHeaderConfig(titleColor), onBack, animate);

            return new HeaderResult
            {
                Header = mfResult.Header,
                HBox = mfResult.HBox,
                BackBtn = mfResult.BackBtn,
                TitleText = mfResult.TitleText,
            };
        }

        /// <summary>
        /// Create a standardized content area that accounts for header and nav bar.
        /// </summary>
        public static RectTransform BuildContentArea(Transform parent, string name = "Content",
            float extraTopPad = 0f, float extraBottomPad = 0f, bool hasNavBar = true)
        {
            return MFPageHelper.BuildContentArea(parent,
                DefaultContentConfig(extraTopPad, extraBottomPad, hasNavBar), name);
        }

        /// <summary>
        /// Build a standardized scrollable content area with viewport and vertical layout.
        /// Returns the content Transform to add items to.
        /// </summary>
        public static Transform BuildScrollContent(Transform parent, float spacing = 6f)
        {
            return MFPageHelper.BuildScrollContent(parent, spacing);
        }

        /// <summary>
        /// Create a simple text label with standard font and layout element.
        /// </summary>
        public static Text MakeLabel(Transform parent, string text, int fontSize, Color color,
            float height, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            return MFPageHelper.MakeLabel(parent, text, fontSize, color, height, alignment);
        }

        /// <summary>
        /// Create a horizontal divider line.
        /// </summary>
        public static GameObject MakeDivider(Transform parent, float height = 1f)
        {
            return MFPageHelper.MakeDivider(parent, new Color(0.2f, 0.2f, 0.25f, 0.6f), height);
        }

        /// <summary>
        /// Create an action button with consistent styling (used in inventory, shop, etc.).
        /// </summary>
        public static Button MakeActionButton(Transform parent, string label, Color accentColor,
            Action onClick, float? width = null)
        {
            return MFPageHelper.MakeActionButton(parent, label, accentColor,
                TosTheme.FontSmall, onClick, width);
        }

        /// <summary>
        /// Add press-scale feedback to a GameObject (0.95x on down, 1.0x on up).
        /// </summary>
        public static void AddPressFeedback(GameObject go, float pressScale = 0.95f)
        {
            MFPageHelper.AddPressFeedback(go, pressScale);
        }
    }
}

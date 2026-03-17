using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Title/main menu page. Shows game title and navigation buttons.
    /// </summary>
    public class TitlePage : PageBase<TitleScreen>
    {
        protected override void OnBind(TitleScreen screen)
        {
            // Dark background
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // Title label at top
            var titleRegion = CreateRegion("TitleArea", MFAnchor.Top,
                new MFPadding(0, 60, 0, 0));
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(titleRegion, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            titleRect.sizeDelta = new Vector2(0, 70);
            var titleText = titleGO.AddComponent<Text>();
            titleText.text = screen.Title;
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 48;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;

            // Navigation buttons in a vertical layout
            var buttonArea = CreateRegion("Buttons", MFAnchor.Center,
                new MFPadding(40, 0, 40, 80));
            var vbox = CreateVBox("ButtonList", buttonArea, spacing: 16f);

            foreach (var entry in screen.NavEntries)
            {
                string screenId = entry.ScreenId;
                var btn = SpawnPrimitive<MFButton>(vbox);
                btn.gameObject.name = $"btn_{screenId}";
                btn.SetLabel(entry.Label);
                btn.OnClick = () => screen.OnNavSelected(screenId);
            }
        }
    }
}

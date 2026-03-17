using UnityEngine;
using UnityEngine.UI;
using MobileForge.Presentation;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Generic fallback page for unimplemented screens.
    /// Shows the screen ID and a back button.
    /// </summary>
    public class PlaceholderPage : PageBase<IScreen>
    {
        private string _screenId;
        private UIRouter _router;

        public void SetScreenId(string screenId) => _screenId = screenId;
        public void SetRouter(UIRouter router) => _router = router;

        protected override void OnBind(IScreen screen)
        {
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // Screen name label
            var labelGO = new GameObject("ScreenLabel");
            labelGO.transform.SetParent(transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.4f);
            labelRect.anchorMax = new Vector2(1, 0.6f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var txt = labelGO.AddComponent<Text>();
            string displayName = _screenId != null
                ? _screenId.Replace("_", " ").ToUpper() : "UNKNOWN";
            txt.text = displayName;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 36;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            // Back button
            var btnArea = CreateRegion("Actions", MFAnchor.Bottom,
                new MFPadding(60, 0, 60, 60));
            btnArea.sizeDelta = new Vector2(0, 70);
            var backBtn = SpawnPrimitive<MFButton>(btnArea);
            backBtn.gameObject.name = "btn_back";
            backBtn.SetLabel("Back");
            backBtn.OnClick = () => _router?.Pop();
        }
    }
}

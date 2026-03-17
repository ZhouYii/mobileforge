using UnityEngine;
using UnityEngine.UI;
using MobileForge.UIComponents;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.TestScenes
{
    /// <summary>
    /// Drives the Primitive Gallery scene — instantiates every primitive in multiple states
    /// for visual QA. No game code, no screens, no data loading.
    ///
    /// Usage: create a Canvas in a scene, attach this to a child, press Play.
    /// </summary>
    public class PrimitiveGalleryDriver : MonoBehaviour
    {
        [SerializeField] private MFPrimitiveLibrary _library;

        void Start()
        {
            if (_library != null)
                MFPrimitiveLibrary.SetInstance(_library);

            // Configure CanvasScaler
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(540, 960);
                    scaler.matchWidthOrHeight = 0.5f;
                }
            }

            // Main vertical layout
            var mainLayout = gameObject.AddComponent<VerticalLayoutGroup>();
            mainLayout.spacing = 20;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = false;
            mainLayout.childControlWidth = true;
            mainLayout.childControlHeight = true;
            mainLayout.padding = new RectOffset(20, 20, 20, 20);

            // Scroll wrapper
            var scrollGO = new GameObject("Scroll");
            scrollGO.transform.SetParent(transform.parent, false);
            var scrollRect = scrollGO.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;
            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.content = GetComponent<RectTransform>();

            // ── Section: Buttons ──
            CreateSectionLabel("Buttons");

            var buttonRow = CreateHBox("ButtonRow");
            var btn1 = MFPrimitiveLibrary.Spawn<MFButton>(buttonRow);
            btn1.SetLabel("Enabled");
            btn1.SetEnabled(true);
            btn1.OnClick = () => Debug.Log("Button 1 clicked!");

            var btn2 = MFPrimitiveLibrary.Spawn<MFButton>(buttonRow);
            btn2.SetLabel("Disabled");
            btn2.SetEnabled(false);

            var btn3 = MFPrimitiveLibrary.Spawn<MFButton>(buttonRow);
            btn3.SetLabel("Colored");
            btn3.SetColor(new Color(0.2f, 0.6f, 0.9f), Color.white);

            var btn4 = MFPrimitiveLibrary.Spawn<MFButton>(buttonRow);
            btn4.SetLabel("Gold");
            btn4.SetColor(new Color(0.9f, 0.7f, 0.2f), Color.black);

            // ── Section: Progress Bars ──
            CreateSectionLabel("Progress Bars");

            var bar1 = MFPrimitiveLibrary.Spawn<MFProgressBar>(transform);
            bar1.SetProgress(75, 100);
            bar1.SetColor(Color.green);

            var bar2 = MFPrimitiveLibrary.Spawn<MFProgressBar>(transform);
            bar2.SetProgress(30, 100);
            bar2.SetColor(Color.red);

            var bar3 = MFPrimitiveLibrary.Spawn<MFProgressBar>(transform);
            bar3.SetProgress(100, 100);
            bar3.SetColor(Color.cyan);

            // ── Section: Monster Cards ──
            CreateSectionLabel("Monster Cards");

            var cardRow = CreateHBox("CardRow");
            var card1 = MFPrimitiveLibrary.Spawn<MFMonsterCard>(cardRow);
            card1.SetData("Water Dragon", 1, 5, 99);
            card1.OnTapped = () => Debug.Log("Card 1 tapped");

            var card2 = MFPrimitiveLibrary.Spawn<MFMonsterCard>(cardRow);
            card2.SetData("Fire Phoenix", 2, 4, 50);

            var card3 = MFPrimitiveLibrary.Spawn<MFMonsterCard>(cardRow);
            card3.SetData("Earth Golem", 3, 3, 25);

            var card4 = MFPrimitiveLibrary.Spawn<MFMonsterCard>(cardRow);
            card4.SetData("Light Angel", 4, 6, 1);

            // ── Section: Currency Display ──
            CreateSectionLabel("Currency Display");

            var currency = MFPrimitiveLibrary.Spawn<MFCurrencyDisplay>(transform);
            currency.SetImmediate(12345);

            // ── Section: Gem Board ──
            CreateSectionLabel("Gem Board (5x6)");

            var boardContainer = new GameObject("BoardContainer");
            boardContainer.transform.SetParent(transform, false);
            var bcRect = boardContainer.AddComponent<RectTransform>();
            boardContainer.AddComponent<LayoutElement>().preferredHeight = 350;

            var board = MFPrimitiveLibrary.Spawn<MFGemBoard>(boardContainer.transform);
            var elements = new int[]
            {
                1, 2, 3, 4, 5, 6,
                2, 3, 4, 5, 6, 1,
                3, 4, 5, 6, 1, 2,
                4, 5, 6, 1, 2, 3,
                5, 6, 1, 2, 3, 4
            };
            board.SetElements(elements, 5, 6);
            board.OnGemSwapped = (from, to) => Debug.Log($"Gem swapped: {from} -> {to}");

            // ── Section: Overlay ──
            CreateSectionLabel("Overlay (tap to toggle)");

            var overlayContainer = new GameObject("OverlayDemo");
            overlayContainer.transform.SetParent(transform, false);
            overlayContainer.AddComponent<RectTransform>();
            overlayContainer.AddComponent<LayoutElement>().preferredHeight = 100;
            var overlayBg = overlayContainer.AddComponent<Image>();
            overlayBg.color = new Color(0.3f, 0.3f, 0.3f);

            var overlayInst = MFPrimitiveLibrary.Spawn<MFOverlay>(overlayContainer.transform);
            overlayInst.Show(0.5f);
            bool visible = true;
            overlayInst.OnTapped = () =>
            {
                if (visible) overlayInst.Hide(0.3f);
                else overlayInst.Show(0.3f);
                visible = !visible;
            };
        }

        private void CreateSectionLabel(string title)
        {
            var go = new GameObject($"Section_{title}");
            go.transform.SetParent(transform, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredHeight = 30;
            var txt = go.AddComponent<Text>();
            txt.text = $"--- {title} ---";
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 20;
            txt.color = Color.yellow;
            txt.alignment = TextAnchor.MiddleLeft;
        }

        private Transform CreateHBox(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<RectTransform>();
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            go.AddComponent<LayoutElement>().preferredHeight = 130;
            return go.transform;
        }
    }
}

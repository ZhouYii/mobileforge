using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MobileForge.Presentation;
using MobileForge.UIComponents;
using MobileForge.UIComponents.Pages;
using MobileForge.UIComponents.TestScenes.Mocks;

namespace MobileForge.UIComponents.TestScenes
{
    /// <summary>
    /// Drives the Page Preview scene — instantiates individual pages with mock screen data.
    /// A dropdown selects which page to preview. Pages bind to mock screens with sample data.
    ///
    /// Usage: create a Canvas with this MonoBehaviour, add a Dropdown child, press Play.
    /// </summary>
    public class PagePreviewDriver : MonoBehaviour
    {
        [SerializeField] private MFPrimitiveLibrary _library;
        [SerializeField] private Dropdown _pageDropdown;

        private UIComponentRegistry _registry;
        private Dictionary<string, object> _mocks;
        private IUIComponent _currentComponent;
        private Transform _previewRoot;

        void Start()
        {
            if (_library != null)
                MFPrimitiveLibrary.SetInstance(_library);

            // Configure canvas
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

            // Create preview root
            var rootGO = new GameObject("PreviewRoot");
            rootGO.transform.SetParent(transform, false);
            var rootRect = rootGO.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = new Vector2(0, 0);
            rootRect.offsetMax = new Vector2(0, -40); // Leave room for dropdown
            _previewRoot = rootGO.transform;

            // Build mocks and registry
            _mocks = MockScreenFactory.CreateAll();
            _registry = new UIComponentRegistry();
            RegisterPreviewPages();

            // Setup dropdown
            if (_pageDropdown == null)
            {
                // Create a dropdown if not assigned
                var ddGO = new GameObject("PageDropdown");
                ddGO.transform.SetParent(transform, false);
                var ddRect = ddGO.AddComponent<RectTransform>();
                ddRect.anchorMin = new Vector2(0, 1);
                ddRect.anchorMax = Vector2.one;
                ddRect.pivot = new Vector2(0.5f, 1);
                ddRect.sizeDelta = new Vector2(0, 40);
                ddGO.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);
                _pageDropdown = ddGO.AddComponent<Dropdown>();

                // Label
                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(ddGO.transform, false);
                var lr = labelGO.AddComponent<RectTransform>();
                lr.anchorMin = Vector2.zero;
                lr.anchorMax = Vector2.one;
                lr.offsetMin = new Vector2(10, 0);
                lr.offsetMax = new Vector2(-30, 0);
                var label = labelGO.AddComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                label.fontSize = 18;
                label.color = Color.white;
                label.alignment = TextAnchor.MiddleLeft;
                _pageDropdown.captionText = label;

                // Template (minimal)
                var templateGO = new GameObject("Template");
                templateGO.transform.SetParent(ddGO.transform, false);
                templateGO.SetActive(false);
                var tRect = templateGO.AddComponent<RectTransform>();
                tRect.anchorMin = new Vector2(0, 0);
                tRect.anchorMax = new Vector2(1, 0);
                tRect.pivot = new Vector2(0.5f, 1);
                tRect.sizeDelta = new Vector2(0, 200);
                templateGO.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);
                templateGO.AddComponent<ScrollRect>();

                var viewport = new GameObject("Viewport");
                viewport.transform.SetParent(templateGO.transform, false);
                viewport.AddComponent<RectTransform>().anchorMax = Vector2.one;
                viewport.AddComponent<Mask>().showMaskGraphic = false;
                viewport.AddComponent<Image>().color = Color.clear;

                var content = new GameObject("Content");
                content.transform.SetParent(viewport.transform, false);
                content.AddComponent<RectTransform>();

                var itemGO = new GameObject("Item");
                itemGO.transform.SetParent(content.transform, false);
                itemGO.AddComponent<RectTransform>();
                var toggle = itemGO.AddComponent<Toggle>();

                var itemLabelGO = new GameObject("Item Label");
                itemLabelGO.transform.SetParent(itemGO.transform, false);
                itemLabelGO.AddComponent<RectTransform>();
                var itemLabel = itemLabelGO.AddComponent<Text>();
                itemLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                itemLabel.fontSize = 16;
                itemLabel.color = Color.white;

                _pageDropdown.template = tRect;
                _pageDropdown.itemText = itemLabel;
            }

            // Populate dropdown
            _pageDropdown.ClearOptions();
            var options = new List<string>(_mocks.Keys);
            _pageDropdown.AddOptions(options);
            _pageDropdown.onValueChanged.AddListener(OnPageSelected);

            // Show first page
            if (options.Count > 0)
                OnPageSelected(0);
        }

        private void OnPageSelected(int index)
        {
            var options = new List<string>(_mocks.Keys);
            if (index < 0 || index >= options.Count) return;

            string pageId = options[index];

            _currentComponent?.Unmount();
            _currentComponent = null;

            var component = _registry.Create(pageId);
            if (component == null) return;

            var mockScreen = _mocks[pageId];
            component.BindUntyped(mockScreen);
            component.Mount(_previewRoot);
            _currentComponent = component;
        }

        private void RegisterPreviewPages()
        {
            _registry.Register("title", () =>
            {
                var go = new GameObject("TitlePage");
                return go.AddComponent<TitlePage>();
            });

            _registry.Register("battle", () =>
            {
                var go = new GameObject("BattlePage");
                return go.AddComponent<BattlePage>();
            });

            _registry.Register("gacha", () =>
            {
                var go = new GameObject("GachaPage");
                return go.AddComponent<GachaPage>();
            });

            _registry.Register("shop", () =>
            {
                var go = new GameObject("ShopPage");
                return go.AddComponent<ShopPage>();
            });

            _registry.Register("result", () =>
            {
                var go = new GameObject("ResultPage");
                var page = go.AddComponent<ResultPage>();
                // Pass the mock's router so Continue button doesn't NRE
                var mockResult = _mocks["result"] as Mocks.MockResultScreen;
                if (mockResult != null)
                    page.SetRouter(mockResult.MockRouter);
                return page;
            });
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using MobileForge.Presentation;
using MobileForge.Infrastructure;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// Animated currency counter display.
    /// Wraps the framework's CurrencyBar pure C# model for count-up/down animation.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MFCurrencyDisplay : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Text _valueLabel;

        private CurrencyBar _currencyBar;
        private bool _initialized;

        void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            _currencyBar = new CurrencyBar();
            _currencyBar.OnDisplayChanged = OnDisplayChanged;

            if (_valueLabel == null)
                _valueLabel = GetComponentInChildren<Text>();

            if (_valueLabel == null)
            {
                // Build fallback UI
                var layout = gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 40;
                layout.preferredWidth = 150;

                var hbox = gameObject.AddComponent<HorizontalLayoutGroup>();
                hbox.spacing = 8;
                hbox.childForceExpandWidth = false;
                hbox.childForceExpandHeight = true;
                hbox.childControlWidth = true;
                hbox.childControlHeight = true;
                hbox.padding = new RectOffset(8, 8, 4, 4);

                // Icon placeholder
                var iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(transform, false);
                var iconLayout = iconGO.AddComponent<LayoutElement>();
                iconLayout.preferredWidth = 32;
                iconLayout.preferredHeight = 32;
                _icon = iconGO.AddComponent<Image>();
                _icon.color = new Color(1f, 0.85f, 0.2f); // Gold

                // Value label
                var labelGO = new GameObject("Value");
                labelGO.transform.SetParent(transform, false);
                labelGO.AddComponent<RectTransform>();
                var labelLayout = labelGO.AddComponent<LayoutElement>();
                labelLayout.flexibleWidth = 1;
                _valueLabel = labelGO.AddComponent<Text>();
                _valueLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                _valueLabel.fontSize = 22;
                _valueLabel.color = Color.white;
                _valueLabel.alignment = TextAnchor.MiddleLeft;
            }
        }

        /// <summary>
        /// Bind to a currency type and event bus for automatic updates.
        /// </summary>
        public void Bind(string currencyType, EventBus eventBus)
        {
            EnsureInitialized();
            _currencyBar.Bind(currencyType, eventBus);
        }

        /// <summary>
        /// Set the displayed value directly (with animation).
        /// </summary>
        public void SetValue(int value)
        {
            EnsureInitialized();
            _currencyBar.SetValue(value);
        }

        /// <summary>
        /// Set value immediately without animation.
        /// </summary>
        public void SetImmediate(int value)
        {
            EnsureInitialized();
            _currencyBar.SetImmediate(value);
        }

        void Update()
        {
            _currencyBar?.Update(Time.deltaTime);
        }

        void OnDestroy()
        {
            _currencyBar?.Unbind();
        }

        private void OnDisplayChanged(long newValue)
        {
            if (_valueLabel != null)
                _valueLabel.text = newValue.ToString("N0");
        }
    }
}

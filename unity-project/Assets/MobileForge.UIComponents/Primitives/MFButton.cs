using System;
using UnityEngine;
using UnityEngine.UI;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// Styled button primitive. Supports label, icon, enabled state, and visual styles.
    /// Integrates with the framework's ButtonFeedback for press effects.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MFButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;

        public Action OnClick;

        private bool _initialized;

        void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            // Auto-find components if not assigned via Inspector
            if (_button == null) _button = GetComponentInChildren<Button>();
            if (_label == null) _label = GetComponentInChildren<Text>();
            if (_background == null) _background = GetComponent<Image>();

            // Build fallback UI if nothing exists
            if (_button == null)
            {
                if (_background == null)
                    _background = gameObject.AddComponent<Image>();
                _background.color = new Color(0.25f, 0.25f, 0.35f, 1f);
                _button = gameObject.AddComponent<Button>();

                // Default size
                var rect = GetComponent<RectTransform>();
                var layout = gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 60;
                layout.flexibleWidth = 1;

                // Label child
                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(transform, false);
                var labelRect = labelGO.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                _label = labelGO.AddComponent<Text>();
                _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _label.fontSize = 24;
                _label.color = Color.white;
                _label.alignment = TextAnchor.MiddleCenter;
            }

            _button.onClick.AddListener(() => OnClick?.Invoke());
        }

        public void SetLabel(string text)
        {
            EnsureInitialized();
            if (_label != null) _label.text = text;
        }

        public void SetIcon(Sprite sprite)
        {
            EnsureInitialized();
            if (_icon != null)
            {
                _icon.sprite = sprite;
                _icon.enabled = sprite != null;
            }
        }

        public void SetEnabled(bool enabled)
        {
            EnsureInitialized();
            if (_button != null) _button.interactable = enabled;
            if (_background != null)
                _background.color = enabled
                    ? new Color(0.25f, 0.25f, 0.35f, 1f)
                    : new Color(0.2f, 0.2f, 0.2f, 0.6f);
            if (_label != null)
                _label.color = enabled ? Color.white : Color.gray;
        }

        public void SetColor(Color bgColor, Color textColor)
        {
            EnsureInitialized();
            if (_background != null) _background.color = bgColor;
            if (_label != null) _label.color = textColor;
        }

        /// <summary>
        /// Test helper — programmatically invoke the click.
        /// </summary>
        public void SimulateClick()
        {
            if (_button != null && _button.interactable)
                OnClick?.Invoke();
        }
    }
}

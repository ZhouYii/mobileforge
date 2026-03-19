using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// Modal dialog popup with title, message, and configurable buttons.
    /// Can be used standalone or through PopupStack.
    /// </summary>
    public class MFDialog : MonoBehaviour
    {
        public Action OnDismissed;

        private Image _overlay;
        private RectTransform _panel;
        private Text _titleText;
        private Text _messageText;
        private HorizontalLayoutGroup _buttonRow;
        private CanvasGroup _canvasGroup;
        private readonly List<Button> _buttons = new();

        void Awake()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            var rt = gameObject.GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;

            // Dark overlay
            var overlayGo = new GameObject("Overlay");
            overlayGo.transform.SetParent(transform, false);
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            _overlay = overlayGo.AddComponent<Image>();
            _overlay.color = new Color(0, 0, 0, 0.6f);
            var overlayBtn = overlayGo.AddComponent<Button>();
            overlayBtn.onClick.AddListener(() => Dismiss());

            // Panel
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(transform, false);
            _panel = panelGo.AddComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.1f, 0.3f);
            _panel.anchorMax = new Vector2(0.9f, 0.7f);
            _panel.offsetMin = Vector2.zero;
            _panel.offsetMax = Vector2.zero;
            var panelBg = panelGo.AddComponent<Image>();
            panelBg.color = new Color(0.12f, 0.12f, 0.16f, 0.97f);

            var panelLayout = panelGo.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(16, 16, 12, 12);
            panelLayout.spacing = 10;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            _titleText = titleGo.AddComponent<Text>();
            _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _titleText.fontSize = 22;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = new Color(1f, 0.85f, 0f); // Gold
            var titleLe = titleGo.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 30;

            // Message
            var msgGo = new GameObject("Message");
            msgGo.transform.SetParent(panelGo.transform, false);
            _messageText = msgGo.AddComponent<Text>();
            _messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _messageText.fontSize = 14;
            _messageText.alignment = TextAnchor.MiddleCenter;
            _messageText.color = Color.white;
            var msgLe = msgGo.AddComponent<LayoutElement>();
            msgLe.flexibleHeight = 1;
            msgLe.minHeight = 40;

            // Button row
            var btnRowGo = new GameObject("Buttons");
            btnRowGo.transform.SetParent(panelGo.transform, false);
            _buttonRow = btnRowGo.AddComponent<HorizontalLayoutGroup>();
            _buttonRow.spacing = 8;
            _buttonRow.childAlignment = TextAnchor.MiddleCenter;
            _buttonRow.childForceExpandWidth = true;
            _buttonRow.childForceExpandHeight = false;
            var btnRowLe = btnRowGo.AddComponent<LayoutElement>();
            btnRowLe.preferredHeight = 44;
        }

        public void SetTitle(string title) => _titleText.text = title;
        public void SetMessage(string message) => _messageText.text = message;

        public void SetTitleColor(Color color) => _titleText.color = color;

        /// <summary>
        /// Add a button to the dialog.
        /// </summary>
        public Button AddButton(string label, Action onClick, Color? bgColor = null, Color? textColor = null)
        {
            var btnGo = new GameObject($"Btn_{label}");
            btnGo.transform.SetParent(_buttonRow.transform, false);

            var bg = btnGo.AddComponent<Image>();
            bg.color = bgColor ?? new Color(0.2f, 0.4f, 0.7f);

            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            _buttons.Add(btn);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var text = labelGo.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = textColor ?? Color.white;

            var le = btnGo.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            le.flexibleWidth = 1;

            return btn;
        }

        /// <summary>
        /// Show dialog with fade-in.
        /// </summary>
        public void Show(float duration = 0.3f)
        {
            gameObject.SetActive(true);
            StartCoroutine(FadeIn(duration));
        }

        /// <summary>
        /// Dismiss dialog with fade-out.
        /// </summary>
        public void Dismiss(float duration = 0.2f)
        {
            StartCoroutine(FadeOut(duration));
        }

        private IEnumerator FadeIn(float duration)
        {
            _panel.localScale = Vector3.one * 0.8f;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                _canvasGroup.alpha = p;
                _panel.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, p);
                yield return null;
            }
            _canvasGroup.alpha = 1;
            _panel.localScale = Vector3.one;
        }

        private IEnumerator FadeOut(float duration)
        {
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = 1 - Mathf.Clamp01(t / duration);
                yield return null;
            }
            OnDismissed?.Invoke();
            Destroy(gameObject);
        }

        /// <summary>
        /// Factory: create a simple confirm dialog.
        /// </summary>
        public static MFDialog CreateConfirm(Transform parent, string title, string message, string okLabel = "OK", Action onOk = null)
        {
            var go = new GameObject("MFDialog");
            go.transform.SetParent(parent, false);
            var dialog = go.AddComponent<MFDialog>();
            dialog.SetTitle(title);
            dialog.SetMessage(message);
            dialog.AddButton(okLabel, () =>
            {
                onOk?.Invoke();
                dialog.Dismiss();
            }, new Color(0.2f, 0.6f, 0.3f));
            return dialog;
        }
    }
}

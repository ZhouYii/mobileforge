using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// Full-screen dark backdrop overlay. Used as a base layer for modal-style pages.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MFOverlay : MonoBehaviour
    {
        [SerializeField] private Image _backdrop;
        [SerializeField] private Color _color = new(0, 0, 0, 0.7f);

        public Action OnTapped;

        private bool _initialized;

        void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            if (_backdrop == null)
                _backdrop = GetComponent<Image>();

            if (_backdrop == null)
            {
                _backdrop = gameObject.AddComponent<Image>();
                _backdrop.color = new Color(0, 0, 0, 0);

                // Stretch to fill
                var rect = GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            // Add button for tap detection
            var btn = gameObject.GetComponent<Button>();
            if (btn == null) btn = gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => OnTapped?.Invoke());
        }

        public void Show(float fadeDuration = 0.3f)
        {
            EnsureInitialized();
            if (fadeDuration <= 0)
            {
                _backdrop.color = _color;
                return;
            }
            StartCoroutine(FadeCoroutine(0, _color.a, fadeDuration));
        }

        public void Hide(float fadeDuration = 0.3f)
        {
            EnsureInitialized();
            if (fadeDuration <= 0)
            {
                _backdrop.color = new Color(_color.r, _color.g, _color.b, 0);
                return;
            }
            StartCoroutine(FadeCoroutine(_backdrop.color.a, 0, fadeDuration));
        }

        public void SetOpacity(float alpha)
        {
            EnsureInitialized();
            _color = new Color(_color.r, _color.g, _color.b, alpha);
            _backdrop.color = _color;
        }

        private IEnumerator FadeCoroutine(float from, float to, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(from, to, t);
                _backdrop.color = new Color(_color.r, _color.g, _color.b, alpha);
                yield return null;
            }
            _backdrop.color = new Color(_color.r, _color.g, _color.b, to);
        }
    }
}

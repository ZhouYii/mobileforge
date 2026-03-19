using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MobileForge.UIComponents
{
    /// <summary>
    /// Simple fade-to-black screen transition overlay.
    /// Sits above pages, fades in before page swap, fades out after.
    /// </summary>
    public class MFScreenTransition : MonoBehaviour
    {
        private Image _fadeOverlay;
        private CanvasGroup _canvasGroup;
        private bool _isTransitioning;

        public float FadeDuration { get; set; } = 0.25f;

        void Awake()
        {
            var rt = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _fadeOverlay = gameObject.AddComponent<Image>();
            _fadeOverlay.color = new Color(0.03f, 0.03f, 0.05f, 1f);
            _fadeOverlay.raycastTarget = false;

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        /// <summary>
        /// Execute a transition: fade in, run the swap action, fade out.
        /// </summary>
        public void DoTransition(Action swapAction)
        {
            if (_isTransitioning) return;
            StartCoroutine(TransitionCoroutine(swapAction));
        }

        private IEnumerator TransitionCoroutine(Action swapAction)
        {
            _isTransitioning = true;
            _canvasGroup.blocksRaycasts = true;

            // Fade in (to black)
            float t = 0;
            while (t < FadeDuration)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(t / FadeDuration);
                yield return null;
            }
            _canvasGroup.alpha = 1;

            // Execute the page swap
            swapAction?.Invoke();
            yield return null; // Wait a frame for the new page to render

            // Fade out (from black)
            t = 0;
            while (t < FadeDuration)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = 1f - Mathf.Clamp01(t / FadeDuration);
                yield return null;
            }
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            _isTransitioning = false;
        }

        /// <summary>
        /// Quick fade out without swap (for initial page load).
        /// </summary>
        public void FadeOut()
        {
            StartCoroutine(FadeOutCoroutine());
        }

        private IEnumerator FadeOutCoroutine()
        {
            _canvasGroup.alpha = 1;
            float t = 0;
            while (t < FadeDuration)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = 1f - Mathf.Clamp01(t / FadeDuration);
                yield return null;
            }
            _canvasGroup.alpha = 0;
        }
    }
}

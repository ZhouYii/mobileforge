using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MobileForge.Presentation;

namespace MobileForge.UIComponents
{
    /// <summary>
    /// MonoBehaviour bridge for the pure C# ToastLayer.
    /// Renders toast notifications at the top of the screen.
    /// </summary>
    public class MFToastHost : MonoBehaviour
    {
        private ToastLayer _toastLayer;
        private Transform _root;
        private VerticalLayoutGroup _container;
        private readonly Dictionary<int, GameObject> _toastObjects = new();

        public ToastLayer ToastLayer => _toastLayer;

        public void Setup(Transform root)
        {
            _root = root;
            _toastLayer = new ToastLayer();

            // Create toast container at top of screen
            var containerGo = new GameObject("ToastContainer");
            containerGo.transform.SetParent(_root, false);
            var rt = containerGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.85f);
            rt.anchorMax = new Vector2(0.9f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _container = containerGo.AddComponent<VerticalLayoutGroup>();
            _container.spacing = 4;
            _container.childAlignment = TextAnchor.UpperCenter;
            _container.childForceExpandWidth = true;
            _container.childForceExpandHeight = false;

            // Wire events
            _toastLayer.OnToastShown += OnToastShown;
            _toastLayer.OnToastRemoved += OnToastRemoved;
        }

        void Update()
        {
            _toastLayer?.Update(Time.deltaTime);
        }

        /// <summary>
        /// Show a toast message.
        /// </summary>
        public int ShowToast(string text, float duration = 2f)
        {
            return _toastLayer.ShowToast(text, duration);
        }

        private void OnToastShown(string text, float duration, int toastId)
        {
            var go = new GameObject($"Toast_{toastId}");
            go.transform.SetParent(_container.transform, false);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 32;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var txtRect = textGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(8, 0);
            txtRect.offsetMax = new Vector2(-8, 0);
            var txt = textGo.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 13;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            _toastObjects[toastId] = go;

            // Fade in
            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0;
            StartCoroutine(FadeToast(cg, 0, 1, 0.2f));
        }

        private void OnToastRemoved(int toastId)
        {
            if (_toastObjects.TryGetValue(toastId, out var go) && go != null)
            {
                var cg = go.GetComponent<CanvasGroup>();
                if (cg != null)
                    StartCoroutine(FadeAndDestroy(cg, go));
                else
                    Destroy(go);
            }
            _toastObjects.Remove(toastId);
        }

        private IEnumerator FadeToast(CanvasGroup cg, float from, float to, float duration)
        {
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                if (cg != null) cg.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            if (cg != null) cg.alpha = to;
        }

        private IEnumerator FadeAndDestroy(CanvasGroup cg, GameObject go)
        {
            yield return FadeToast(cg, 1, 0, 0.2f);
            if (go != null) Destroy(go);
        }

        void OnDestroy()
        {
            if (_toastLayer != null)
            {
                _toastLayer.OnToastShown -= OnToastShown;
                _toastLayer.OnToastRemoved -= OnToastRemoved;
            }
        }
    }
}

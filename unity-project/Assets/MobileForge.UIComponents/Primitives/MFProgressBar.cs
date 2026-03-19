using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// Animated progress/HP bar with fill image, background, and optional label.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MFProgressBar : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _fill;
        [SerializeField] private Text _label;

        private float _current;
        private float _max = 1;
        private bool _animated = true;
        private Coroutine _animCoroutine;
        private bool _initialized;

        public float FillRatio => _max > 0 ? Mathf.Clamp01(_current / _max) : 0;

        void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            if (_background == null) _background = GetComponent<Image>();

            if (_background == null)
            {
                // Build fallback UI
                _background = gameObject.AddComponent<Image>();
                _background.color = new Color(0.2f, 0.2f, 0.2f, 1f);

                var layout = gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 30;
                layout.flexibleWidth = 1;

                // Fill child
                var fillGO = new GameObject("Fill");
                fillGO.transform.SetParent(transform, false);
                var fillRect = fillGO.AddComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = new Vector2(2, 2);
                fillRect.offsetMax = new Vector2(-2, -2);
                fillRect.pivot = new Vector2(0, 0.5f);
                _fill = fillGO.AddComponent<Image>();
                _fill.color = Color.green;

                // Label child
                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(transform, false);
                var labelRect = labelGO.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                _label = labelGO.AddComponent<Text>();
                _label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                _label.fontSize = 16;
                _label.color = Color.white;
                _label.alignment = TextAnchor.MiddleCenter;
            }

            if (_fill == null)
                _fill = transform.Find("Fill")?.GetComponent<Image>();
            if (_label == null)
                _label = transform.Find("Label")?.GetComponent<Text>();
        }

        public void SetProgress(float current, float max)
        {
            EnsureInitialized();
            _max = Mathf.Max(max, 0.001f);

            float targetRatio = Mathf.Clamp01(current / _max);

            if (_animated && gameObject.activeInHierarchy && Application.isPlaying)
            {
                if (_animCoroutine != null) StopCoroutine(_animCoroutine);
                _animCoroutine = StartCoroutine(AnimateFill(targetRatio, 0.3f));
            }
            else
            {
                ApplyFill(targetRatio);
            }

            _current = current;

            if (_label != null)
                _label.text = $"{Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
        }

        public void SetColor(Color color)
        {
            EnsureInitialized();
            if (_fill != null) _fill.color = color;
        }

        public void SetAnimated(bool animated)
        {
            _animated = animated;
        }

        private void ApplyFill(float ratio)
        {
            if (_fill == null) return;
            var rect = _fill.rectTransform;
            rect.anchorMax = new Vector2(ratio, rect.anchorMax.y);
        }

        private IEnumerator AnimateFill(float targetRatio, float duration)
        {
            if (_fill == null) yield break;
            float startRatio = _fill.rectTransform.anchorMax.x;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ratio = Mathf.Lerp(startRatio, targetRatio, t * t); // ease-in-quad
                ApplyFill(ratio);
                yield return null;
            }
            ApplyFill(targetRatio);
            _animCoroutine = null;
        }
    }
}

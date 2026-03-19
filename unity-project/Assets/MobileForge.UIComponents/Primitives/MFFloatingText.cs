using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// Animated floating text for damage numbers, healing, combo display, etc.
    /// Spawns text that rises and fades out.
    /// </summary>
    public class MFFloatingText : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;

        void Awake()
        {
            var rt = gameObject.GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>
        /// Spawn a floating text at the given local position within this container.
        /// </summary>
        public void Spawn(string text, Vector2 localPosition, Color color, float duration = 0.46f, int fontSize = 18, bool shake = false)
        {
            var go = new GameObject("FloatText");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = localPosition;
            rt.sizeDelta = new Vector2(200, 40);

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.7f);
            shadow.effectDistance = new Vector2(1, -1);

            var cg = go.AddComponent<CanvasGroup>();
            StartCoroutine(AnimateFloat(rt, cg, duration, shake));
        }

        /// <summary>
        /// Spawn damage text with element coloring and optional crit effect.
        /// </summary>
        public void SpawnDamage(int damage, Vector2 localPosition, int element, bool isCrit = false)
        {
            var color = TowerOfSaviors.TosTheme.ElementColor(element);
            int size = isCrit ? 24 : 18;
            string text = isCrit ? $"{damage}!" : damage.ToString();
            Spawn(text, localPosition, color, TowerOfSaviors.TosTheme.AnimDamage, size, isCrit);
        }

        /// <summary>
        /// Spawn healing text (green, rises slower).
        /// </summary>
        public void SpawnHeal(int amount, Vector2 localPosition)
        {
            Spawn($"+{amount}", localPosition, new Color(0.3f, 1f, 0.3f), 0.5f, 16);
        }

        /// <summary>
        /// Spawn combo display text.
        /// </summary>
        public void SpawnCombo(int comboCount, Vector2 localPosition)
        {
            var color = TowerOfSaviors.TosTheme.ComboColor(comboCount);
            int size = comboCount >= 7 ? 28 : (comboCount >= 5 ? 24 : 20);

            var go = new GameObject("ComboText");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = localPosition;
            rt.sizeDelta = new Vector2(250, 50);

            var txt = go.AddComponent<Text>();
            txt.text = $"{comboCount} Combo!";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = size;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(2, -2);

            var cg = go.AddComponent<CanvasGroup>();

            // Pop-in with EaseOutBack, then hold, then fade
            StartCoroutine(ComboAnimation(rt, cg, TowerOfSaviors.TosTheme.AnimCombo));
        }

        private IEnumerator ComboAnimation(RectTransform rt, CanvasGroup cg, float popDuration)
        {
            // Pop in (EaseOutBack)
            rt.localScale = Vector3.one * 0.3f;
            float t = 0;
            while (t < popDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / popDuration);
                float s = EaseOutBack(p);
                rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.3f, 1f, s);
                yield return null;
            }
            rt.localScale = Vector3.one;

            // Hold for 0.5s
            yield return new WaitForSeconds(0.5f);

            // Fade out
            t = 0;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                cg.alpha = 1f - Mathf.Clamp01(t / 0.3f);
                yield return null;
            }
            Destroy(rt.gameObject);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }

        private IEnumerator AnimateFloat(RectTransform rt, CanvasGroup cg, float duration, bool shake)
        {
            Vector2 startPos = rt.anchoredPosition;
            float riseHeight = 70f;
            float t = 0;

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);

                // Parabolic arc trajectory (rise then fall like a thrown object)
                // y = -4h * p * (p - 1) where h is max height
                float arc = -4f * riseHeight * p * (p - 1f);
                // Slight horizontal drift
                float drift = p * 15f * (shake ? Mathf.Sign(Mathf.Sin(t * 20f)) : 1f);
                Vector2 pos = startPos + new Vector2(drift, arc);

                if (shake && p < 0.3f)
                {
                    pos.x += Mathf.Sin(t * 40f) * 5f * (1f - p / 0.3f);
                }

                rt.anchoredPosition = pos;

                // Fade out in last 30%
                if (p > 0.7f)
                    cg.alpha = 1f - (p - 0.7f) / 0.3f;

                // Scale pop at start (EaseOutBack feel)
                if (p < 0.12f)
                {
                    float scale = 1f + 0.4f * (1f - p / 0.12f);
                    rt.localScale = Vector3.one * scale;
                }
                else if (p < 0.2f)
                {
                    float settle = Mathf.Lerp(1.05f, 1f, (p - 0.12f) / 0.08f);
                    rt.localScale = Vector3.one * settle;
                }
                else
                {
                    rt.localScale = Vector3.one;
                }

                yield return null;
            }

            Destroy(rt.gameObject);
        }
    }
}

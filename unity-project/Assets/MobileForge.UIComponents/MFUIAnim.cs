using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MobileForge.UIComponents
{
    /// <summary>
    /// Simple UI animation utilities. Provides coroutine-based fade, stagger, and pop-in effects.
    /// Matches Godot's MFUIAnim patterns (stagger_fade_in, pop_in, fade_in).
    /// </summary>
    public static class MFUIAnim
    {
        /// <summary>
        /// Fade in a CanvasGroup from 0 to 1.
        /// </summary>
        public static IEnumerator FadeIn(CanvasGroup cg, float duration = 0.3f)
        {
            if (cg == null) yield break;
            cg.alpha = 0;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Clamp01(t / duration);
                yield return null;
            }
            cg.alpha = 1;
        }

        /// <summary>
        /// Staggered fade-in for a list of transforms (each needs a CanvasGroup).
        /// </summary>
        /// <summary>
        /// Staggered fade-in for a list of CanvasGroups.
        /// Must be started via a MonoBehaviour's StartCoroutine.
        /// </summary>
        public static IEnumerator StaggerFadeIn(List<CanvasGroup> items, float baseDuration = 0.3f, float stagger = 0.08f)
        {
            // Set all invisible first
            foreach (var cg in items)
                if (cg != null) cg.alpha = 0;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    // Start fade on the MonoBehaviour attached to the same GameObject
                    var mb = items[i].GetComponent<MonoBehaviour>();
                    if (mb != null)
                        mb.StartCoroutine(FadeIn(items[i], baseDuration));
                    else
                        items[i].alpha = 1; // fallback: instant show
                }
                if (i < items.Count - 1)
                    yield return new WaitForSeconds(stagger);
            }
        }

        /// <summary>
        /// Pop-in animation: scale from small to normal with overshoot.
        /// </summary>
        public static IEnumerator PopIn(Transform target, float duration = 0.3f, float startScale = 0.5f, float overshoot = 1.12f)
        {
            if (target == null) yield break;
            target.localScale = Vector3.one * startScale;
            float t = 0;
            float halfDuration = duration * 0.6f;

            // Phase 1: scale to overshoot
            while (t < halfDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / halfDuration);
                float s = Mathf.Lerp(startScale, overshoot, EaseOutBack(p));
                target.localScale = Vector3.one * s;
                yield return null;
            }

            // Phase 2: settle to 1.0
            t = 0;
            float settleDuration = duration * 0.4f;
            while (t < settleDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / settleDuration);
                float s = Mathf.Lerp(overshoot, 1f, p);
                target.localScale = Vector3.one * s;
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        /// <summary>
        /// Slide in from a direction.
        /// </summary>
        public static IEnumerator SlideIn(RectTransform rt, Vector2 fromOffset, float duration = 0.4f)
        {
            if (rt == null) yield break;
            Vector2 target = rt.anchoredPosition;
            rt.anchoredPosition = target + fromOffset;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                rt.anchoredPosition = Vector2.Lerp(target + fromOffset, target, EaseOutCubic(p));
                yield return null;
            }
            rt.anchoredPosition = target;
        }

        /// <summary>
        /// Shake animation for defeat/error effects.
        /// </summary>
        public static IEnumerator Shake(RectTransform rt, float duration = 0.5f, float amplitude = 10f)
        {
            if (rt == null) yield break;
            Vector2 origin = rt.anchoredPosition;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float decay = 1f - Mathf.Clamp01(t / duration);
                float offset = Mathf.Sin(t * 30f) * amplitude * decay;
                rt.anchoredPosition = origin + new Vector2(offset, 0);
                yield return null;
            }
            rt.anchoredPosition = origin;
        }

        /// <summary>
        /// Delayed fade-in — waits, then fades.
        /// </summary>
        public static IEnumerator DelayedFadeIn(CanvasGroup cg, float delay, float duration = 0.3f)
        {
            if (cg == null) yield break;
            cg.alpha = 0;
            yield return new WaitForSeconds(delay);
            yield return FadeIn(cg, duration);
        }

        /// <summary>
        /// Scale pulse (repeating). Call StopCoroutine to stop.
        /// </summary>
        public static IEnumerator Pulse(Transform target, float minScale = 1f, float maxScale = 1.15f, float period = 0.6f)
        {
            if (target == null) yield break;
            while (true)
            {
                float t = 0;
                while (t < period)
                {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / period);
                    float s = Mathf.Lerp(minScale, maxScale, Mathf.Sin(p * Mathf.PI));
                    target.localScale = Vector3.one * s;
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Fade out a CanvasGroup from 1 to 0.
        /// </summary>
        public static IEnumerator FadeOut(CanvasGroup cg, float duration = 0.3f)
        {
            if (cg == null) yield break;
            cg.alpha = 1;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                cg.alpha = 1f - Mathf.Clamp01(t / duration);
                yield return null;
            }
            cg.alpha = 0;
        }

        /// <summary>
        /// Slide out to a direction.
        /// </summary>
        public static IEnumerator SlideOut(RectTransform rt, Vector2 toOffset, float duration = 0.3f)
        {
            if (rt == null) yield break;
            Vector2 start = rt.anchoredPosition;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                rt.anchoredPosition = Vector2.Lerp(start, start + toOffset, EaseInCubic(p));
                yield return null;
            }
            rt.anchoredPosition = start + toOffset;
        }

        /// <summary>
        /// Scale bounce — quick scale up then settle.
        /// </summary>
        public static IEnumerator ScaleBounce(Transform target, float peakScale = 1.2f, float duration = 0.25f)
        {
            if (target == null) yield break;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float s = 1f + (peakScale - 1f) * Mathf.Sin(p * Mathf.PI);
                target.localScale = Vector3.one * s;
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        /// <summary>
        /// Color flash — tint an Image briefly then return to original.
        /// </summary>
        public static IEnumerator ColorFlash(UnityEngine.UI.Image image, Color flashColor, float duration = 0.3f)
        {
            if (image == null) yield break;
            Color original = image.color;
            image.color = flashColor;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                image.color = Color.Lerp(flashColor, original, p);
                yield return null;
            }
            image.color = original;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }

        private static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3);
        }

        private static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        /// <summary>
        /// Smooth ease-in-out for balanced animations.
        /// </summary>
        public static float EaseInOutCubic(float t)
        {
            return t < 0.5f
                ? 4f * t * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 3) / 2f;
        }
    }
}

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public static class TweenHelper
{
    public static IEnumerator PunchScale(Transform target, Vector3 punch, float duration)
    {
        Vector3 original = target.localScale;
        float half = duration * 0.5f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(Mathf.Clamp01(elapsed / half));
            target.localScale = original + punch * t;
            yield return null;
        }

        elapsed = 0f;
        Vector3 peaked = original + punch;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(Mathf.Clamp01(elapsed / half));
            target.localScale = Vector3.Lerp(peaked, original, t);
            yield return null;
        }

        target.localScale = original;
    }

    public static IEnumerator ScaleTo(Transform target, Vector3 endScale, float duration)
    {
        Vector3 start = target.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutBack(Mathf.Clamp01(elapsed / duration));
            target.localScale = Vector3.LerpUnclamped(start, endScale, t);
            yield return null;
        }

        target.localScale = endScale;
    }

    public static IEnumerator FadeTo(CanvasGroup cg, float endAlpha, float duration)
    {
        float start = cg.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(Mathf.Clamp01(elapsed / duration));
            cg.alpha = Mathf.Lerp(start, endAlpha, t);
            yield return null;
        }

        cg.alpha = endAlpha;
    }

    public static IEnumerator ColorTo(Graphic graphic, Color endColor, float duration)
    {
        Color start = graphic.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(Mathf.Clamp01(elapsed / duration));
            graphic.color = Color.Lerp(start, endColor, t);
            yield return null;
        }

        graphic.color = endColor;
    }

    public static IEnumerator MoveTo(RectTransform rt, Vector2 endPos, float duration)
    {
        Vector2 start = rt.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(Mathf.Clamp01(elapsed / duration));
            rt.anchoredPosition = Vector2.Lerp(start, endPos, t);
            yield return null;
        }

        rt.anchoredPosition = endPos;
    }

    public static IEnumerator DelayedAction(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    public static IEnumerator ShakePosition(RectTransform rt, float magnitude, float duration)
    {
        Vector2 original = rt.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - Mathf.Clamp01(elapsed / duration);
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude * decay;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude * decay;
            rt.anchoredPosition = original + new Vector2(x, y);
            yield return null;
        }

        rt.anchoredPosition = original;
    }

    // Easing functions
    public static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    public static float EaseOutElastic(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float c4 = (2f * Mathf.PI) / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }
}

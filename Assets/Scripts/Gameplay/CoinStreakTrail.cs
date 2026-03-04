using UnityEngine;
using UnityEngine.UI;

public class CoinStreakTrail : MonoBehaviour
{
    private RectTransform rt;
    private Image image;
    private Vector2 p0, p1, p2, p3;
    private float startDelay;
    private float travelDuration;
    private float elapsed;
    private bool moving;
    private Color baseColor;
    private Vector2 prevPos;
    private float streakLength;
    private bool visible;

    public void Setup(RectTransform rectTransform, Image img, Color color, float length)
    {
        rt = rectTransform;
        image = img;
        baseColor = color;
        streakLength = length;
    }

    public void Initialize(Vector2 start, Vector2 end, float delay, float duration, float arcHeight)
    {
        p0 = start;
        p3 = end;

        Vector2 midPoint = (start + end) * 0.5f;
        float dist = Vector2.Distance(start, end);
        Vector2 up = Vector2.up * arcHeight * dist;

        p1 = start + (midPoint - start) * 0.3f + up;
        p2 = end + (midPoint - end) * 0.3f + up * 0.5f;

        startDelay = delay;
        travelDuration = duration;
        elapsed = 0f;
        moving = false;
        visible = false;
        prevPos = start;

        rt.anchoredPosition = start;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
        image.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
    }

    public bool Tick(float dt)
    {
        if (!moving)
        {
            startDelay -= dt;
            if (startDelay <= 0f)
                moving = true;
            return false;
        }

        elapsed += dt;
        float rawT = Mathf.Clamp01(elapsed / travelDuration);
        float t = EaseInOutCubic(rawT);

        Vector2 newPos = EvaluateBezier(t);
        rt.anchoredPosition = newPos;

        // Rotate to face movement direction
        Vector2 dir = newPos - prevPos;
        if (dir.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0, 0, angle);
            // Only become visible once we have a valid direction
            if (!visible)
            {
                visible = true;
                image.color = baseColor;
            }
        }
        prevPos = newPos;

        // Shrink length as it approaches target
        float scale = Mathf.Lerp(1f, 0.4f, rawT);
        rt.localScale = new Vector3(scale, scale, 1f);

        // Fade out in last 30%
        if (rawT > 0.7f)
        {
            float fadeT = (rawT - 0.7f) / 0.3f;
            var c = baseColor;
            c.a = 1f - fadeT;
            image.color = c;
        }

        return rawT >= 1f;
    }

    private Vector2 EvaluateBezier(float t)
    {
        float u = 1f - t;
        float uu = u * u;
        float uuu = uu * u;
        float tt = t * t;
        float ttt = tt * t;

        return uuu * p0
             + 3f * uu * t * p1
             + 3f * u * tt * p2
             + ttt * p3;
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}

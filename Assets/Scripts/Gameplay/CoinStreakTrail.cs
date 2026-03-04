using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Procedural ribbon trail that follows a cubic Bezier curve.
/// Extends MaskableGraphic to generate a single connected quad strip mesh —
/// the UI equivalent of TrailRenderer for Canvas/2D.
/// </summary>
public class CoinStreakTrail : MaskableGraphic
{
    private Vector2 p0, p1, p2, p3;
    private float startDelay;
    private float travelDuration;
    private float elapsed;
    private bool moving;
    private float headT;
    private float tailT;
    private float trailSpan;
    private float streakWidth;
    private int sampleCount;
    private Color baseColor;
    private bool draining;
    private float drainElapsed;
    private float drainDuration;
    private float tailTAtDrainStart;

    public float HeadT => headT;
    public float TailT => tailT;
    public bool IsDraining => draining;

    public void Setup(Color color, float width, int samples, float span)
    {
        baseColor = color;
        streakWidth = width;
        sampleCount = Mathf.Max(2, samples);
        trailSpan = span;
        raycastTarget = false;
    }

    public void SetColor(Color color)
    {
        baseColor = color;
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
        headT = 0f;
        tailT = 0f;
        draining = false;
        drainElapsed = 0f;

        SetVerticesDirty();
    }

    /// <summary>
    /// Copy Bezier and timing from another trail so both follow the exact same path.
    /// </summary>
    public void CopyPathFrom(CoinStreakTrail source)
    {
        p0 = source.p0;
        p1 = source.p1;
        p2 = source.p2;
        p3 = source.p3;
        startDelay = source.startDelay;
        travelDuration = source.travelDuration;
        elapsed = 0f;
        moving = false;
        headT = 0f;
        tailT = 0f;
        draining = false;
        drainElapsed = 0f;

        SetVerticesDirty();
    }

    /// <summary>
    /// Sync head/tail from the driver trail instead of computing independently.
    /// </summary>
    public void SyncFrom(CoinStreakTrail driver)
    {
        headT = driver.headT;
        tailT = driver.tailT;
        moving = driver.moving;
        draining = driver.draining;
        SetVerticesDirty();
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

        if (!draining)
        {
            float rawT = Mathf.Clamp01(elapsed / travelDuration);
            headT = EaseInOutCubic(rawT);
            tailT = Mathf.Max(0f, headT - trailSpan);

            if (rawT >= 1f)
            {
                headT = 1f;
                draining = true;
                tailTAtDrainStart = tailT;
                drainElapsed = 0f;
                drainDuration = travelDuration * 0.3f;
            }
        }
        else
        {
            drainElapsed += dt;
            float drainT = Mathf.Clamp01(drainElapsed / drainDuration);
            tailT = Mathf.Lerp(tailTAtDrainStart, 1f, drainT);

            if (drainT >= 1f)
            {
                SetVerticesDirty();
                return true;
            }
        }

        SetVerticesDirty();
        return false;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float span = headT - tailT;
        if (span <= 0.001f) return;

        int samples = sampleCount;
        float halfWidth = streakWidth * 0.5f;

        for (int i = 0; i <= samples; i++)
        {
            float frac = (float)i / samples; // 0 at tail, 1 at head
            float t = Mathf.Lerp(tailT, headT, frac);

            Vector2 pos = EvaluateBezier(t);
            Vector2 tangent = EvaluateTangent(t);

            // Perpendicular: rotate tangent 90 degrees CCW
            Vector2 perp = new Vector2(-tangent.y, tangent.x);
            if (perp.sqrMagnitude > 0.0001f)
                perp.Normalize();
            else
                perp = Vector2.up;

            // Width taper: eye/football shape — 0 at both ends, widest in middle
            float taper = Mathf.Sin(frac * Mathf.PI);
            float w = halfWidth * taper;

            Vector2 left = pos + perp * w;
            Vector2 right = pos - perp * w;

            vh.AddVert(new Vector3(left.x, left.y, 0f), baseColor, new Vector2(0f, frac));
            vh.AddVert(new Vector3(right.x, right.y, 0f), baseColor, new Vector2(1f, frac));
        }

        for (int i = 0; i < samples; i++)
        {
            int bl = i * 2;
            int br = i * 2 + 1;
            int tl = (i + 1) * 2;
            int tr = (i + 1) * 2 + 1;

            vh.AddTriangle(bl, tl, tr);
            vh.AddTriangle(bl, tr, br);
        }
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

    /// <summary>
    /// First derivative of cubic Bezier — tangent direction at parameter t.
    /// </summary>
    private Vector2 EvaluateTangent(float t)
    {
        float u = 1f - t;
        return 3f * u * u * (p1 - p0)
             + 6f * u * t * (p2 - p1)
             + 3f * t * t * (p3 - p2);
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}

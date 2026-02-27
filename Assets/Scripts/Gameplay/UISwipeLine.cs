using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISwipeLine : MonoBehaviour
{
    [Tooltip("Parent RectTransform where line segments are spawned")]
    [SerializeField] private RectTransform lineContainer;
    [Tooltip("Tint color applied to all line segments")]
    [SerializeField] private Color lineColor = new Color(0.4f, 0.7f, 1f, 1f);
    [Tooltip("Height of each line segment in pixels")]
    [SerializeField] private float lineThickness = 12f;
    [Tooltip("Material with UI/Glow shader for additive glow blending")]
    [SerializeField] private Material glowMaterial;
    [Tooltip("Number of line segments pre-created in the object pool")]
    [SerializeField] private int poolSize = 10;
    [Header("Glow Texture")]
    [Tooltip("Power curve for glow texture falloff (higher = tighter bright core)")]
    [SerializeField] private float glowFalloff = 2f;
    [Tooltip("Brightness multiplier baked into the glow texture core")]
    [SerializeField] private float glowBrightness = 1.5f;

    private List<RectTransform> segments = new List<RectTransform>();
    private int activeCount;
    private Sprite lineGlowSprite;

    private void Awake()
    {
        lineGlowSprite = GenerateLineGlowSprite();

        for (int i = 0; i < poolSize; i++)
        {
            var seg = CreateSegment();
            seg.gameObject.SetActive(false);
            segments.Add(seg);
        }
    }

    /// <summary>
    /// Generate a vertical gradient glow texture: bright center, fades to transparent edges.
    /// Same technique as StreakTrailManager.GenerateGlowTexture().
    /// </summary>
    private Sprite GenerateLineGlowSprite()
    {
        int height = 64;
        int width = 4;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float halfHeight = height * 0.5f;

        for (int y = 0; y < height; y++)
        {
            float distFromCenter = Mathf.Abs(y - halfHeight) / halfHeight;
            float alpha = Mathf.Pow(1f - distFromCenter, glowFalloff);
            float brightness = Mathf.Min(alpha * glowBrightness, 1f);
            Color pixel = new Color(brightness, brightness, brightness, alpha);

            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, pixel);
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0f, 0.5f));
    }

    private RectTransform CreateSegment()
    {
        GameObject go = new GameObject("LineSegment", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(lineContainer, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);

        Image img = go.GetComponent<Image>();
        img.color = lineColor;
        img.raycastTarget = false;
        if (lineGlowSprite != null)
            img.sprite = lineGlowSprite;
        if (glowMaterial != null)
            img.material = glowMaterial;

        return rt;
    }

    private RectTransform GetSegment(int index)
    {
        while (index >= segments.Count)
        {
            var seg = CreateSegment();
            seg.gameObject.SetActive(false);
            segments.Add(seg);
        }
        return segments[index];
    }

    public void UpdateLine(List<RectTransform> tilePositions, Vector2 pointerLocalPos)
    {
        int segIndex = 0;

        // Draw segments between consecutive tile centers
        for (int i = 0; i < tilePositions.Count - 1; i++)
        {
            SetSegment(segIndex, tilePositions[i].anchoredPosition, tilePositions[i + 1].anchoredPosition);
            segIndex++;
        }

        // Draw segment from last tile to current pointer position
        if (tilePositions.Count > 0)
        {
            SetSegment(segIndex, tilePositions[tilePositions.Count - 1].anchoredPosition, pointerLocalPos);
            segIndex++;
        }

        // Hide unused segments
        for (int i = segIndex; i < activeCount; i++)
            segments[i].gameObject.SetActive(false);
        activeCount = segIndex;
    }

    private void SetSegment(int index, Vector2 from, Vector2 to)
    {
        RectTransform seg = GetSegment(index);
        seg.gameObject.SetActive(true);

        Vector2 diff = to - from;
        float distance = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        seg.anchoredPosition = from;
        seg.sizeDelta = new Vector2(distance, lineThickness);
        seg.localRotation = Quaternion.Euler(0, 0, angle);
    }

    public void ClearLine()
    {
        for (int i = 0; i < activeCount; i++)
            segments[i].gameObject.SetActive(false);
        activeCount = 0;
    }
}

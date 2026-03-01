using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISwipeLine : MonoBehaviour
{
    [Tooltip("Parent RectTransform where line segments are spawned")]
    [SerializeField] private RectTransform lineContainer;
    [Tooltip("Fill color of the swipe line")]
    [SerializeField] private Color lineColor = new Color(0.4f, 0.8f, 1f, 0.9f);
    [Tooltip("Outline color for contrast against any background")]
    [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.6f);
    [Tooltip("Width of the inner line in pixels")]
    [SerializeField] private float lineThickness = 10f;
    [Tooltip("Extra width added for the outline (each side)")]
    [SerializeField] private float outlineWidth = 3f;
    [Tooltip("Number of line segments pre-created in the object pool")]
    [SerializeField] private int poolSize = 10;

    private List<LineSegment> segments = new List<LineSegment>();
    private int activeCount;

    private struct LineSegment
    {
        public RectTransform outline;
        public RectTransform fill;
    }

    private void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var seg = CreateSegment();
            seg.outline.gameObject.SetActive(false);
            seg.fill.gameObject.SetActive(false);
            segments.Add(seg);
        }
    }

    private LineSegment CreateSegment()
    {
        // Outline (behind)
        GameObject outlineGO = new GameObject("LineOutline", typeof(RectTransform), typeof(Image));
        outlineGO.transform.SetParent(lineContainer, false);
        RectTransform outlineRT = outlineGO.GetComponent<RectTransform>();
        outlineRT.pivot = new Vector2(0f, 0.5f);
        outlineRT.anchorMin = new Vector2(0.5f, 0.5f);
        outlineRT.anchorMax = new Vector2(0.5f, 0.5f);
        Image outlineImg = outlineGO.GetComponent<Image>();
        outlineImg.color = outlineColor;
        outlineImg.raycastTarget = false;

        // Fill (in front)
        GameObject fillGO = new GameObject("LineFill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(lineContainer, false);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.pivot = new Vector2(0f, 0.5f);
        fillRT.anchorMin = new Vector2(0.5f, 0.5f);
        fillRT.anchorMax = new Vector2(0.5f, 0.5f);
        Image fillImg = fillGO.GetComponent<Image>();
        fillImg.color = lineColor;
        fillImg.raycastTarget = false;

        return new LineSegment { outline = outlineRT, fill = fillRT };
    }

    private LineSegment GetSegment(int index)
    {
        while (index >= segments.Count)
        {
            var seg = CreateSegment();
            seg.outline.gameObject.SetActive(false);
            seg.fill.gameObject.SetActive(false);
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
        {
            segments[i].outline.gameObject.SetActive(false);
            segments[i].fill.gameObject.SetActive(false);
        }
        activeCount = segIndex;
    }

    private void SetSegment(int index, Vector2 from, Vector2 to)
    {
        LineSegment seg = GetSegment(index);
        seg.outline.gameObject.SetActive(true);
        seg.fill.gameObject.SetActive(true);

        Vector2 diff = to - from;
        float distance = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0, 0, angle);

        // Outline: thicker, behind
        seg.outline.anchoredPosition = from;
        seg.outline.sizeDelta = new Vector2(distance, lineThickness + outlineWidth * 2f);
        seg.outline.localRotation = rot;

        // Fill: thinner, in front
        seg.fill.anchoredPosition = from;
        seg.fill.sizeDelta = new Vector2(distance, lineThickness);
        seg.fill.localRotation = rot;
    }

    public void ClearLine()
    {
        for (int i = 0; i < activeCount; i++)
        {
            segments[i].outline.gameObject.SetActive(false);
            segments[i].fill.gameObject.SetActive(false);
        }
        activeCount = 0;
    }
}

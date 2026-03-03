using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class LayoutDebugOverlay : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Show outlines in Game view")]
    public bool showOverlay = true;
    [Tooltip("Automatically hide when entering Play mode")]
    public bool hideInPlayMode = true;
    [Tooltip("Border thickness in pixels")]
    public float borderWidth = 2f;

    [Header("Targets")]
    public RectTransform[] targets;
    public Color[] colors;

    private GameObject overlayRoot;
    private bool wasShowing;

    private static readonly Color[] defaultColors = new Color[]
    {
        new Color(1f, 0.3f, 0.3f, 0.8f),   // red
        new Color(0.3f, 1f, 0.3f, 0.8f),    // green
        new Color(0.3f, 0.5f, 1f, 0.8f),    // blue
        new Color(1f, 1f, 0.3f, 0.8f),      // yellow
        new Color(1f, 0.5f, 0f, 0.8f),      // orange
        new Color(0.8f, 0.3f, 1f, 0.8f),    // purple
    };

    private const string OverlayName = "__DebugOverlay__";

    private void LateUpdate()
    {
        bool shouldShow = showOverlay && !(hideInPlayMode && Application.isPlaying);

        // Reconnect to orphaned overlay (reference lost after domain reload)
        if (overlayRoot == null)
            FindExistingOverlay();

        if (shouldShow && overlayRoot == null)
            BuildOverlay();
        else if (!shouldShow && overlayRoot != null)
            DestroyOverlay();

        if (shouldShow && overlayRoot != null)
            UpdatePositions();
    }

    private void FindExistingOverlay()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            var child = canvas.transform.GetChild(i);
            if (child.name == OverlayName)
            {
                overlayRoot = child.gameObject;
                return;
            }
        }
    }

    private void BuildOverlay()
    {
        if (targets == null || targets.Length == 0) return;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        overlayRoot = new GameObject(OverlayName);
        overlayRoot.transform.SetParent(canvas.transform, false);
        overlayRoot.transform.SetAsLastSibling();
        var rootRT = overlayRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        // Disable raycasting on the whole overlay
        var cg = overlayRoot.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;
            Color c = (colors != null && i < colors.Length) ? colors[i] : defaultColors[i % defaultColors.Length];
            CreateBorderRect(targets[i].name, overlayRoot.transform, c);
        }
    }

    private void CreateBorderRect(string label, Transform parent, Color color)
    {
        // We create 4 thin images for each border side + a label
        string[] sides = { "Top", "Bottom", "Left", "Right" };
        for (int s = 0; s < 4; s++)
        {
            var go = new GameObject($"Border_{label}_{sides[s]}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        // Label
        var labelGO = new GameObject($"Label_{label}");
        labelGO.transform.SetParent(parent, false);
        var labelRT = labelGO.AddComponent<RectTransform>();
        var text = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 14;
        text.color = color;
        text.alignment = TMPro.TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        text.overflowMode = TMPro.TextOverflowModes.Overflow;
    }

    private void UpdatePositions()
    {
        if (targets == null) return;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var canvasRT = canvas.GetComponent<RectTransform>();

        int childIdx = 0;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) { childIdx += 5; continue; }

            // Get target corners in canvas space
            Vector3[] worldCorners = new Vector3[4];
            targets[i].GetWorldCorners(worldCorners);

            Vector2 min, max;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRT, RectTransformUtility.WorldToScreenPoint(null, worldCorners[0]), null, out min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRT, RectTransformUtility.WorldToScreenPoint(null, worldCorners[2]), null, out max);

            float w = borderWidth;

            // Top border
            SetRect(overlayRoot.transform.GetChild(childIdx + 0), min.x, max.y - w, max.x, max.y);
            // Bottom border
            SetRect(overlayRoot.transform.GetChild(childIdx + 1), min.x, min.y, max.x, min.y + w);
            // Left border
            SetRect(overlayRoot.transform.GetChild(childIdx + 2), min.x, min.y, min.x + w, max.y);
            // Right border
            SetRect(overlayRoot.transform.GetChild(childIdx + 3), max.x - w, min.y, max.x, max.y);
            // Label
            var labelRT = overlayRoot.transform.GetChild(childIdx + 4).GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0.5f, 0.5f);
            labelRT.anchorMax = new Vector2(0.5f, 0.5f);
            labelRT.anchoredPosition = new Vector2((min.x + max.x) * 0.5f, max.y + 12f);
            labelRT.sizeDelta = new Vector2(200, 20);

            childIdx += 5;
        }
    }

    private void SetRect(Transform child, float xMin, float yMin, float xMax, float yMax)
    {
        var rt = child.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
        rt.sizeDelta = new Vector2(xMax - xMin, yMax - yMin);
    }

    private void DestroyOverlay()
    {
        if (overlayRoot != null)
        {
            if (Application.isPlaying)
                Destroy(overlayRoot);
            else
                DestroyImmediate(overlayRoot);
            overlayRoot = null;
        }
    }

    private void OnDisable()
    {
        DestroyOverlay();
    }

    private void OnDestroy()
    {
        DestroyOverlay();
    }
}

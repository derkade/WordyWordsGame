using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Generates a procedural tiled texture with horizontal lines that match
/// the line height of a TMP_Text component, creating a ruled-paper effect.
/// Attach to a GameObject with an Image component placed behind the text.
/// </summary>
[RequireComponent(typeof(Image))]
public class LinedTextBackground : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The TMP_Text whose line height determines line spacing")]
    [SerializeField] private TMP_Text targetText;

    [Header("Line Appearance")]
    [Tooltip("Color of the horizontal rule lines")]
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.15f);
    [Tooltip("Thickness of each line in pixels (1-2 recommended)")]
    [SerializeField] private int lineThickness = 1;
    [Tooltip("Vertical offset to nudge lines into alignment with text baselines")]
    [SerializeField] private float verticalOffset = 0f;

    private Image image;
    private Texture2D linedTexture;
    private float lastLineHeight;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        GenerateTexture();
    }

    /// <summary>
    /// Computes the TMP line height and generates a tiled texture strip.
    /// </summary>
    public void GenerateTexture()
    {
        if (targetText == null || image == null) return;

        float lineHeight = CalculateLineHeight();
        if (lineHeight < 4f) return;

        int lineHeightPx = Mathf.RoundToInt(lineHeight);

        // Avoid regenerating if line height hasn't changed
        if (lineHeightPx == Mathf.RoundToInt(lastLineHeight) && linedTexture != null)
            return;
        lastLineHeight = lineHeight;

        int texWidth = 4;
        int texHeight = lineHeightPx;

        if (linedTexture != null)
            Destroy(linedTexture);

        linedTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
        linedTexture.wrapMode = TextureWrapMode.Repeat;
        linedTexture.filterMode = FilterMode.Point;

        // Fill transparent
        Color clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < texHeight; y++)
            for (int x = 0; x < texWidth; x++)
                linedTexture.SetPixel(x, y, clear);

        // Draw line at bottom of tile (y=0 to y=lineThickness-1)
        // When tiled top-down, this places a rule between each row
        for (int t = 0; t < lineThickness && t < texHeight; t++)
            for (int x = 0; x < texWidth; x++)
                linedTexture.SetPixel(x, t, lineColor);

        linedTexture.Apply();

        Sprite sprite = Sprite.Create(
            linedTexture,
            new Rect(0, 0, texWidth, texHeight),
            new Vector2(0.5f, 1f),
            100f
        );

        image.sprite = sprite;
        image.type = Image.Type.Tiled;
        image.pixelsPerUnitMultiplier = 1f;

        // Apply vertical offset
        if (verticalOffset != 0f)
        {
            RectTransform rt = image.rectTransform;
            Vector2 pos = rt.anchoredPosition;
            pos.y = verticalOffset;
            rt.anchoredPosition = pos;
        }
    }

    private float CalculateLineHeight()
    {
        // Temporarily set two-line text to measure actual line-to-line distance
        // (includes lineSpacing, which lineInfo[0].lineHeight does not)
        string original = targetText.text;

        targetText.text = "A\nA";
        targetText.ForceMeshUpdate();

        var textInfo = targetText.textInfo;
        if (textInfo.lineCount >= 2)
        {
            // Measure actual distance between first and second line origins
            float distance = textInfo.lineInfo[0].ascender - textInfo.lineInfo[1].ascender;
            targetText.text = original;
            return Mathf.Abs(distance);
        }

        targetText.text = original;

        // Fallback: compute from font metrics
        var font = targetText.font;
        if (font != null)
        {
            var face = font.faceInfo;
            float baseLineHeight = targetText.fontSize * face.lineHeight / face.pointSize;
            float spacingMultiplier = 1f + targetText.lineSpacing / 100f;
            return baseLineHeight * spacingMultiplier;
        }

        return targetText.fontSize * 1.2f;
    }

    private void OnDestroy()
    {
        if (linedTexture != null)
            Destroy(linedTexture);
    }
}

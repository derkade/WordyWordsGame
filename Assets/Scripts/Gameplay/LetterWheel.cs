using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LetterWheel : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Parent RectTransform where letter tiles are spawned in a circle")]
    [SerializeField] private RectTransform wheelContainer;
    [Tooltip("Prefab for each letter tile (needs LetterTile component)")]
    [SerializeField] private GameObject letterTilePrefab;
    [Tooltip("Large circle Image behind the letter tiles")]
    [SerializeField] private Image wheelBackground;
    [Tooltip("Shuffle button placed at the center of the wheel")]
    [SerializeField] private Button shuffleButton;

    [Header("Layout")]
    [Tooltip("Distance from wheel center to letter positions in pixels")]
    [SerializeField] private float wheelRadius = 85f;
    [Tooltip("Minimum tile size in pixels")]
    [SerializeField] private float tileMinSize = 60f;
    [Tooltip("Maximum tile size in pixels")]
    [SerializeField] private float tileMaxSize = 110f;
    [Tooltip("Multiplier for tile size based on arc gap (smaller = more spacing between letters)")]
    [Range(0.4f, 1f)]
    [SerializeField] private float tileSizeFactor = 0.75f;
    [Tooltip("How much padding the background circle adds beyond the tile edges (1.0 = tiles touch edge, higher = more padding)")]
    [Range(0.5f, 2f)]
    [SerializeField] private float backgroundPadding = 1.3f;
    [Tooltip("Font size as a fraction of tile size")]
    [Range(0.3f, 0.9f)]
    [SerializeField] private float fontSizeRatio = 0.6f;

    [Header("Wheel Border")]
    [Tooltip("Border thickness for the wheel circle")]
    [SerializeField] private float wheelBorderWidth = 2f;
    [Tooltip("Border color for the wheel circle")]
    [SerializeField] private Color wheelBorderColor = Color.black;

    [Header("Wheel Drop Shadow")]
    [Tooltip("Shadow color for the wheel circle")]
    [SerializeField] private Color wheelShadowColor = new Color(0f, 0f, 0f, 0.3f);
    [Tooltip("Shadow offset in pixels (negative Y = down)")]
    [SerializeField] private Vector2 wheelShadowOffset = new Vector2(0f, -3f);
    [Tooltip("Shadow blur radius in pixels")]
    [SerializeField] private float wheelShadowBlur = 5f;
    [Tooltip("Extra padding around shape for shadow rendering")]
    [SerializeField] private float wheelShadowExpand = 6f;

    [Header("Wheel Inner Bevel")]
    [Tooltip("How deep the bevel extends from the edge in pixels")]
    [SerializeField] private float wheelBevelSize = 14f;
    [Tooltip("Intensity of the highlight/shadow bevel effect")]
    [Range(0f, 0.5f)]
    [SerializeField] private float wheelBevelStrength = 0.2f;

    [Header("Shuffle Button")]
    [Tooltip("Size of the shuffle button in pixels")]
    [SerializeField] private float shuffleButtonSize = 60f;
    [Tooltip("Tint color of the shuffle icon in the wheel center")]
    [SerializeField] private Color shuffleIconColor = new Color(0.35f, 0.35f, 0.4f, 0.6f);

    private List<LetterTile> tiles = new List<LetterTile>();
    private string currentLetters;
    private Material wheelMaterial;

    /// <summary>Expanded size of the wheel background (including shadow expand), set after BuildWheel.</summary>
    public float WheelBackgroundSize { get; private set; }

    private void Start()
    {
        // Apply SDF rounded rect as circle for wheel background
        if (wheelBackground != null)
        {
            wheelBackground.sprite = null;
            wheelBackground.type = Image.Type.Simple;
            wheelBackground.transform.SetAsFirstSibling();

            var shader = Shader.Find("UI/RoundedRect");
            if (shader != null)
            {
                wheelMaterial = new Material(shader);
                wheelBackground.material = wheelMaterial;
            }
        }

        if (shuffleButton != null)
        {
            shuffleButton.onClick.AddListener(ShuffleTiles);

            // Apply shuffle icon sprite to button
            var btnImage = shuffleButton.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.sprite = GenerateShuffleSprite(64);
                btnImage.color = shuffleIconColor;
            }

            // Hide the text child — icon is sufficient
            var btnText = shuffleButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                btnText.enabled = false;
        }
    }

    public void BuildWheel(string letters)
    {
        ClearWheel();
        currentLetters = letters.ToUpper();

        int count = currentLetters.Length;
        float angleStep = 360f / count;
        // Start from top (-90 deg) so first letter is at top
        float startAngle = 90f;

        // Compute tile size: use arc gap between tiles, capped to a max
        float arcGap = 2f * Mathf.PI * wheelRadius / count;
        float tileSize = Mathf.Clamp(arcGap * tileSizeFactor, tileMinSize, tileMaxSize);

        // Size the wheel background circle to tightly frame the tiles
        if (wheelBackground != null)
        {
            float bgSize = wheelRadius * 2f + tileSize * backgroundPadding;
            float shadowExp = Mathf.Max(wheelShadowExpand, 0f);
            float expandedSize = bgSize + shadowExp * 2f;
            wheelBackground.rectTransform.sizeDelta = new Vector2(expandedSize, expandedSize);
            WheelBackgroundSize = expandedSize;

            if (wheelMaterial != null)
            {
                wheelMaterial.SetVector("_RectSize", new Vector4(expandedSize, expandedSize, 0, 0));
                wheelMaterial.SetFloat("_Radius", expandedSize * 0.5f);
                wheelMaterial.SetFloat("_BorderWidth", wheelBorderWidth);
                wheelMaterial.SetColor("_BorderColor", wheelBorderColor);
                wheelMaterial.SetColor("_ShadowColor", wheelShadowColor);
                wheelMaterial.SetVector("_ShadowOffset", new Vector4(wheelShadowOffset.x, wheelShadowOffset.y, 0, 0));
                wheelMaterial.SetFloat("_ShadowBlur", wheelShadowBlur);
                wheelMaterial.SetFloat("_ShadowExpand", shadowExp);
                wheelMaterial.SetFloat("_BevelSize", wheelBevelSize);
                wheelMaterial.SetFloat("_BevelStrength", wheelBevelStrength);
            }
        }

        // Position and size shuffle button at center
        if (shuffleButton != null)
        {
            var btnRT = shuffleButton.GetComponent<RectTransform>();
            btnRT.anchoredPosition = Vector2.zero;
            btnRT.sizeDelta = new Vector2(shuffleButtonSize, shuffleButtonSize);
        }

        for (int i = 0; i < count; i++)
        {
            GameObject tileGO = Instantiate(letterTilePrefab, wheelContainer);
            tileGO.name = $"Tile_{currentLetters[i]}_{i}";

            RectTransform rt = tileGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(tileSize, tileSize);

            float angle = startAngle - i * angleStep;
            float rad = angle * Mathf.Deg2Rad;
            rt.anchoredPosition = new Vector2(
                Mathf.Cos(rad) * wheelRadius,
                Mathf.Sin(rad) * wheelRadius
            );

            // Scale font to match tile size
            TMP_Text txt = tileGO.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.fontSize = tileSize * fontSizeRatio;

            LetterTile tile = tileGO.GetComponent<LetterTile>();
            tile.SetLetter(currentLetters[i]);
            tile.WheelIndex = i;

            tiles.Add(tile);
        }
    }

    private bool isShuffling;

    public void ShuffleTiles()
    {
        if (tiles.Count <= 1 || isShuffling) return;
        StartCoroutine(AnimatedShuffle());
    }

    private IEnumerator AnimatedShuffle()
    {
        isShuffling = true;

        // Fisher-Yates shuffle to get new letter order
        string previous = currentLetters;
        char[] chars;
        int attempts = 0;
        do
        {
            chars = previous.ToCharArray();
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            attempts++;
        } while (new string(chars) == previous && attempts < 10);

        // Build a mapping: for each new slot, which old tile goes there?
        // Old tile i has letter previous[i], new slot j needs chars[j]
        // Find where each tile needs to move
        var oldPositions = new Vector2[tiles.Count];
        for (int i = 0; i < tiles.Count; i++)
            oldPositions[i] = tiles[i].GetComponent<RectTransform>().anchoredPosition;

        // Map: newIndex -> oldIndex (which tile moves to which slot)
        string newStr = new string(chars);
        var used = new bool[tiles.Count];
        var moveMap = new int[tiles.Count]; // moveMap[newSlot] = oldTileIndex
        for (int n = 0; n < chars.Length; n++)
        {
            for (int o = 0; o < previous.Length; o++)
            {
                if (!used[o] && previous[o] == chars[n])
                {
                    moveMap[n] = o;
                    used[o] = true;
                    break;
                }
            }
        }

        // Animate each tile from its current position to its new position
        float duration = 0.5f;
        float elapsed = 0f;

        // Cache start positions per tile (indexed by old tile index)
        var startPos = new Vector2[tiles.Count];
        var endPos = new Vector2[tiles.Count];
        for (int n = 0; n < tiles.Count; n++)
        {
            int oldIdx = moveMap[n];
            startPos[oldIdx] = oldPositions[oldIdx];
            endPos[oldIdx] = oldPositions[n]; // target is the new slot position
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = TweenHelper.EaseOutBack(Mathf.Clamp01(elapsed / duration));
            for (int i = 0; i < tiles.Count; i++)
            {
                var rt = tiles[i].GetComponent<RectTransform>();
                rt.anchoredPosition = Vector2.LerpUnclamped(startPos[i], endPos[i], t);
            }
            yield return null;
        }

        // Snap to final positions and update letters
        currentLetters = newStr;
        // Reorder tiles list to match new positions
        var newTiles = new List<LetterTile>(tiles.Count);
        for (int n = 0; n < tiles.Count; n++)
            newTiles.Add(tiles[moveMap[n]]);

        for (int i = 0; i < newTiles.Count; i++)
        {
            var rt = newTiles[i].GetComponent<RectTransform>();
            rt.anchoredPosition = oldPositions[i];
            newTiles[i].SetLetter(chars[i]);
            newTiles[i].WheelIndex = i;
        }

        tiles = newTiles;
        isShuffling = false;
    }

    public void DeselectAll()
    {
        foreach (var tile in tiles)
            tile.SetSelected(false);
    }

    public void ClearWheel()
    {
        foreach (var tile in tiles)
        {
            if (tile != null)
                Destroy(tile.gameObject);
        }
        tiles.Clear();
    }

    public List<LetterTile> Tiles => tiles;

    private static Sprite GenerateShuffleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        // Clear to transparent
        var clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

        float lineWidth = size * 0.1f;
        float margin = size * 0.2f;
        float arrowSize = size * 0.15f;

        // Draw two crossing diagonal lines
        DrawLine(tex, size, margin, margin, size - margin, size - margin, lineWidth);
        DrawLine(tex, size, margin, size - margin, size - margin, margin, lineWidth);

        // Arrowheads at the right ends
        // Top-right arrow (for line going bottom-left to top-right)
        DrawLine(tex, size, size - margin, size - margin, size - margin - arrowSize, size - margin, lineWidth);
        DrawLine(tex, size, size - margin, size - margin, size - margin, size - margin - arrowSize, lineWidth);

        // Bottom-right arrow (for line going top-left to bottom-right)
        DrawLine(tex, size, size - margin, margin, size - margin - arrowSize, margin, lineWidth);
        DrawLine(tex, size, size - margin, margin, size - margin, margin + arrowSize, lineWidth);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static void DrawLine(Texture2D tex, int size, float x0, float y0, float x1, float y1, float width)
    {
        float halfW = width * 0.5f;
        int minX = Mathf.Max(0, (int)(Mathf.Min(x0, x1) - halfW - 1));
        int maxX = Mathf.Min(size - 1, (int)(Mathf.Max(x0, x1) + halfW + 1));
        int minY = Mathf.Max(0, (int)(Mathf.Min(y0, y1) - halfW - 1));
        int maxY = Mathf.Min(size - 1, (int)(Mathf.Max(y0, y1) + halfW + 1));

        float dx = x1 - x0, dy = y1 - y0;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 0.001f) return;

        for (int py = minY; py <= maxY; py++)
        {
            for (int px = minX; px <= maxX; px++)
            {
                // Distance from point to line segment
                float t = Mathf.Clamp01(((px - x0) * dx + (py - y0) * dy) / (len * len));
                float cx = x0 + t * dx, cy = y0 + t * dy;
                float dist = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                float alpha = Mathf.Clamp01(halfW - dist + 0.5f);
                if (alpha > 0)
                {
                    Color existing = tex.GetPixel(px, py);
                    float blended = Mathf.Max(existing.a, alpha);
                    tex.SetPixel(px, py, new Color(1f, 1f, 1f, blended));
                }
            }
        }
    }

}

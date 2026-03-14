using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Full-screen splash overlay that lives in WordyWords scene.
/// Shows "WORDY / WORDS" in grid-style tiles with a loading bar.
/// Call Hide() to fade out and destroy.
/// </summary>
public class SplashScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CrosswordGrid crosswordGrid;
    [SerializeField] private TMP_FontAsset tileFont;

    [Header("Timing")]
    [SerializeField] private float minimumDisplayTime = 2f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Tile Layout")]
    [SerializeField] private float tileSize = 110f;
    [SerializeField] private float tileSpacing = 14f;
    [SerializeField] private float tileStaggerAmount = 20f;
    [SerializeField] private float tileRotationAmount = 6f;

    [Header("Colors")]
    [SerializeField] private Color bgColorTop = new Color(0.08f, 0.08f, 0.18f, 1f);
    [SerializeField] private Color bgColorBottom = new Color(0.22f, 0.28f, 0.50f, 1f);
    [SerializeField] private Color tileLetterColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private Color barBgColor = new Color(0.15f, 0.18f, 0.30f, 1f);
    [SerializeField] private Color barFillColor = new Color(1f, 0.75f, 0.15f, 1f);
    [SerializeField] private Color loadingTextColor = new Color(0.7f, 0.75f, 0.85f, 1f);

    [Header("Stars")]
    [SerializeField] private int starCount = 40;
    [SerializeField] private float starMaxY = 0.7f;

    private GameObject splashRoot;
    private Image barFillImage;
    private RectTransform barFillRT;
    private TextMeshProUGUI percentText;
    private CanvasGroup canvasGroup;
    private float startTime;
    private bool isReady;

    private void Awake()
    {
        startTime = Time.time;
        BuildUI();
    }

    /// <summary>
    /// Called by GameManager when the level is fully loaded and ready to play.
    /// </summary>
    public void Hide()
    {
        isReady = true;
    }

    private void BuildUI()
    {
        // Overlay canvas on top of everything
        splashRoot = new GameObject("SplashOverlay");
        var canvas = splashRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = splashRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        splashRoot.AddComponent<GraphicRaycaster>();
        canvasGroup = splashRoot.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;

        var canvasRT = splashRoot.GetComponent<RectTransform>();

        // Gradient background (dark top → lighter bottom)
        var bgGO = CreateChild("Background", canvasRT);
        StretchFill(bgGO.GetComponent<RectTransform>());
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = GenerateGradientSprite(bgColorTop, bgColorBottom, 256);
        bgImg.type = Image.Type.Sliced;
        bgImg.color = Color.white;

        // Stars scattered in the upper portion
        var starsGO = CreateChild("Stars", canvasRT);
        StretchFill(starsGO.GetComponent<RectTransform>());
        var starSprite = GenerateSoftCircle(16);
        var rng = new System.Random(42); // fixed seed for consistency
        for (int s = 0; s < starCount; s++)
        {
            var starGO = CreateChild($"Star_{s}", starsGO.GetComponent<RectTransform>());
            var starRT = starGO.GetComponent<RectTransform>();
            starRT.anchorMin = new Vector2((float)rng.NextDouble(), 1f - (float)rng.NextDouble() * starMaxY);
            starRT.anchorMax = starRT.anchorMin;
            starRT.anchoredPosition = Vector2.zero;
            float size = 6f + (float)rng.NextDouble() * 10f;
            starRT.sizeDelta = new Vector2(size, size);
            var starImg = starGO.AddComponent<Image>();
            starImg.sprite = starSprite;
            starImg.raycastTarget = false;
            float brightness = 0.5f + (float)rng.NextDouble() * 0.5f;
            starImg.color = new Color(brightness, brightness, brightness, 0.4f + (float)rng.NextDouble() * 0.6f);
        }

        // Get tile material from CrosswordGrid (same shader + properties)
        Material tileMat = null;
        Color tileColor = new Color(1f, 1f, 1f, 0.8f);
        if (crosswordGrid != null)
        {
            tileMat = crosswordGrid.CreateMaterialForSize(tileSize);
            tileColor = crosswordGrid.GetCellDefaultColor();
        }

        // Two rows of letter tiles: "WORDY" / "WORDS"
        // Per-tile vertical offsets and rotations for a playful staggered look
        float[][] yOffsets = {
            new[] { 0.6f, -0.4f, 0.9f, -0.7f, 0.3f },   // WORDY
            new[] { -0.5f, 0.7f, -0.3f, 0.8f, -0.6f }    // WORDS
        };
        float[][] rotations = {
            new[] { -0.5f, 0.7f, -0.8f, 0.4f, -0.6f },   // WORDY
            new[] { 0.6f, -0.4f, 0.7f, -0.5f, 0.3f }     // WORDS
        };

        string[] rows = { "WORDY", "WORDS" };
        float shadowExp = crosswordGrid != null ? 6f : 0f; // match grid's shadowExpand
        float expandedTile = tileSize + shadowExp * 2f;

        for (int r = 0; r < rows.Length; r++)
        {
            string word = rows[r];
            float totalW = word.Length * expandedTile + (word.Length - 1) * tileSpacing;
            float startX = -totalW / 2f + expandedTile / 2f;
            float yAnchor = 0.64f - r * 0.13f;

            var rowGO = CreateChild($"Row_{r}", canvasRT);
            var rowRT = rowGO.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0.5f, yAnchor);
            rowRT.anchorMax = new Vector2(0.5f, yAnchor);
            rowRT.anchoredPosition = Vector2.zero;
            rowRT.sizeDelta = new Vector2(totalW, expandedTile + tileStaggerAmount * 2f);

            for (int i = 0; i < word.Length; i++)
            {
                float x = startX + i * (expandedTile + tileSpacing);
                float yOff = yOffsets[r][i] * tileStaggerAmount;
                float rot = rotations[r][i] * tileRotationAmount;

                var tileGO = CreateChild($"Tile_{word[i]}", rowRT);
                var tileRT = tileGO.GetComponent<RectTransform>();
                tileRT.anchorMin = new Vector2(0.5f, 0.5f);
                tileRT.anchorMax = new Vector2(0.5f, 0.5f);
                tileRT.anchoredPosition = new Vector2(x, yOff);
                tileRT.sizeDelta = new Vector2(expandedTile, expandedTile);
                tileRT.localRotation = Quaternion.Euler(0, 0, rot);

                var img = tileGO.AddComponent<Image>();
                img.color = tileColor;
                img.raycastTarget = false;
                if (tileMat != null) img.material = tileMat;

                // Letter
                var letterGO = CreateChild("Letter", tileRT);
                StretchFill(letterGO.GetComponent<RectTransform>());
                var tmp = letterGO.AddComponent<TextMeshProUGUI>();
                tmp.text = word[i].ToString();
                tmp.fontSize = 56;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = tileLetterColor;
                if (tileFont != null) tmp.font = tileFont;
            }
        }

        // Loading bar
        float barW = 600f, barH = 50f;
        float barPad = 4f;
        float barShadowExp = 6f;
        float barExpandedW = barW + barShadowExp * 2f;
        float barExpandedH = barH + barShadowExp * 2f;

        var barContainer = CreateChild("BarContainer", canvasRT);
        var barContainerRT = barContainer.GetComponent<RectTransform>();
        barContainerRT.anchorMin = new Vector2(0.5f, 0.30f);
        barContainerRT.anchorMax = new Vector2(0.5f, 0.30f);
        barContainerRT.anchoredPosition = Vector2.zero;
        barContainerRT.sizeDelta = new Vector2(barExpandedW, barExpandedH);

        // Mask to clip yellow fill to the tube's rounded shape (rendered first = behind)
        var barMaskGO = CreateChild("BarMask", barContainerRT);
        var barMaskRT = barMaskGO.GetComponent<RectTransform>();
        barMaskRT.anchorMin = Vector2.zero;
        barMaskRT.anchorMax = Vector2.one;
        float maskInset = barPad + barShadowExp;
        barMaskRT.offsetMin = new Vector2(maskInset, maskInset);
        barMaskRT.offsetMax = new Vector2(-maskInset, -maskInset);
        var maskImg = barMaskGO.AddComponent<Image>();
        maskImg.sprite = GenerateRoundedSprite(21);
        maskImg.type = Image.Type.Sliced;
        maskImg.color = Color.white;
        maskImg.raycastTarget = false;
        barMaskGO.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;

        // Yellow fill inside mask (grows via anchorMax, clipped to rounded edges)
        var barFill = CreateChild("BarFill", barMaskRT);
        barFillRT = barFill.GetComponent<RectTransform>();
        barFillRT.anchorMin = Vector2.zero;
        barFillRT.anchorMax = new Vector2(0f, 1f);
        barFillRT.offsetMin = Vector2.zero;
        barFillRT.offsetMax = Vector2.zero;
        barFillImage = barFill.AddComponent<Image>();
        barFillImage.color = barFillColor;

        // Glossy tube on top (rendered last = in front, so gloss/bevel sits over the fill)
        Material barMat = null;
        if (tileMat != null)
        {
            barMat = new Material(tileMat);
            barMat.SetVector("_RectSize", new Vector4(barExpandedW, barExpandedH, 0, 0));
            barMat.SetFloat("_Radius", barH * 0.5f);
            barMat.SetFloat("_ShadowExpand", barShadowExp);
            barMat.SetFloat("_GlossStrength", 1.5f);
        }
        var barBg = CreateChild("BarGloss", barContainerRT);
        StretchFill(barBg.GetComponent<RectTransform>());
        var barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(barBgColor.r, barBgColor.g, barBgColor.b, 0.4f);
        barBgImg.raycastTarget = false;
        if (barMat != null) barBgImg.material = barMat;

        var pctGO = CreateChild("PercentText", barContainerRT);
        StretchFill(pctGO.GetComponent<RectTransform>());
        percentText = pctGO.AddComponent<TextMeshProUGUI>();
        percentText.text = "0%";
        percentText.fontSize = 28;
        percentText.alignment = TextAlignmentOptions.Center;
        percentText.color = Color.white;
        if (tileFont != null) percentText.font = tileFont;

        // "Loading..." text
        var loadGO = CreateChild("LoadingText", canvasRT);
        var loadRT = loadGO.GetComponent<RectTransform>();
        loadRT.anchorMin = new Vector2(0.5f, 0.24f);
        loadRT.anchorMax = new Vector2(0.5f, 0.24f);
        loadRT.anchoredPosition = Vector2.zero;
        loadRT.sizeDelta = new Vector2(400, 50);
        var loadTMP = loadGO.AddComponent<TextMeshProUGUI>();
        loadTMP.text = "Loading...";
        loadTMP.fontSize = 32;
        loadTMP.alignment = TextAlignmentOptions.Center;
        loadTMP.color = loadingTextColor;
        if (tileFont != null) loadTMP.font = tileFont;

        StartCoroutine(AnimateAndHide());
    }

    private IEnumerator AnimateAndHide()
    {
        // Animate loading bar while waiting for game to be ready
        float fakeTarget = 0f;
        float fill = 0f;

        while (!isReady || (Time.time - startTime) < minimumDisplayTime)
        {
            // Fake progress: ramp toward 0.85 over time, then stall until ready
            float elapsed = Time.time - startTime;
            fakeTarget = Mathf.Clamp(elapsed / minimumDisplayTime * 0.85f, 0f, 0.85f);
            fill = Mathf.MoveTowards(fill, fakeTarget, Time.deltaTime * 0.6f);

            SetBarFill(fill);
            percentText.text = $"{Mathf.RoundToInt(fill * 100)}%";
            yield return null;
        }

        // Fill to 100%
        float fillTime = 0f;
        float startFill = fill;
        while (fillTime < 0.3f)
        {
            fillTime += Time.deltaTime;
            float t = fillTime / 0.3f;
            float v = Mathf.Lerp(startFill, 1f, t);
            SetBarFill(v);
            percentText.text = $"{Mathf.RoundToInt(v * 100)}%";
            yield return null;
        }
        SetBarFill(1f);
        percentText.text = "100%";
        yield return new WaitForSeconds(0.3f);

        // Fade out
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeOutDuration)
        {
            fadeElapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - fadeElapsed / fadeOutDuration;
            yield return null;
        }

        Destroy(splashRoot);
        Destroy(gameObject);
    }

    private void SetBarFill(float fill)
    {
        if (barFillRT == null) return;
        barFillRT.anchorMax = new Vector2(fill, 1f);
    }

    private GameObject CreateChild(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private Sprite GenerateGradientSprite(Color top, Color bottom, int height)
    {
        var tex = new Texture2D(1, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[height];
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / (height - 1); // y=0 is bottom in texture space
            Color c = Color.Lerp(bottom, top, t);
            pixels[y] = c;
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, height), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(0, 1, 0, 1));
    }

    private Sprite GenerateSoftCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                float a = Mathf.Clamp01(1f - dist / center);
                a = a * a; // softer falloff
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite GenerateRoundedSprite(int radius)
    {
        int size = radius * 2 + 4;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cx = x < radius ? radius : (x >= size - radius ? size - radius - 1 : x);
                float cy = y < radius ? radius : (y >= size - radius ? size - radius - 1 : y);
                float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        int border = radius + 1;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }
}

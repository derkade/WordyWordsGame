using UnityEngine;
using UnityEngine.UI;

public class ParallaxBackground : MonoBehaviour
{
    public enum VerticalAnchor { Stretch, Bottom, Top, FillHeight }
    public enum ScrollMode { Linear, OvalDrift }

    [System.Serializable]
    public class ParallaxLayer
    {
        [Tooltip("Sprite asset for this layer")]
        public Sprite sprite;
        [Tooltip("Alternate sprite for the B copy (tiles A,B,A,B instead of A,A,A,A). Leave empty for normal tiling.")]
        public Sprite alternateSprite;
        [Tooltip("Scroll speed in screen-widths per second (Linear) or orbit speed in revolutions per second (OvalDrift)")]
        public float scrollSpeed;
        [Tooltip("Alpha transparency (1 = opaque, lower values dim the layer)")]
        [Range(0f, 1f)]
        public float alpha = 1f;
        [Tooltip("Stretch fills screen (may distort). Bottom/Top preserves height ratio. FillHeight scales uniformly to fill height (pixel-perfect).")]
        public VerticalAnchor verticalAnchor = VerticalAnchor.Stretch;
        [Tooltip("Linear scrolls left. OvalDrift follows an elliptical path.")]
        public ScrollMode scrollMode = ScrollMode.Linear;
        [Tooltip("Horizontal radius of oval drift (pixels)")]
        public float ovalRadiusX = 50f;
        [Tooltip("Vertical radius of oval drift (pixels)")]
        public float ovalRadiusY = 30f;
        [Tooltip("OvalDrift: tile in a grid (small repeating textures). Uncheck for single large images like frames.")]
        public bool ovalTileGrid = true;
    }

    [System.Serializable]
    public class ParallaxTheme
    {
        public string name;
        [Tooltip("Uncheck to exclude this theme from random selection")]
        public bool enabled = true;
        [Tooltip("Revealed grid cell tint for this theme (keep dark/muted)")]
        public Color revealedCellColor = new Color(0.3f, 0.3f, 0.5f, 1f);
        public ParallaxLayer[] layers;
    }

    [Header("Themes (randomly selected each level)")]
    [SerializeField] private ParallaxTheme[] themes;

    [Header("Parallax Layers (active set — back to front)")]
    [SerializeField] private ParallaxLayer[] layers;

    [Header("Settings")]
    [SerializeField] private float globalSpeedMultiplier = 1f;
    [SerializeField] private bool scrollEnabled = true;
    [SerializeField] private float referenceHeight = 1080f;

    private int lastThemeIndex = -1;

    /// <summary>Revealed cell color from the currently active theme.</summary>
    public Color ActiveRevealedCellColor =>
        (themes != null && lastThemeIndex >= 0 && lastThemeIndex < themes.Length)
            ? themes[lastThemeIndex].revealedCellColor
            : new Color(0.3f, 0.3f, 0.5f, 1f);

    private struct LayerInfo
    {
        public RectTransform containerRT;
        public RectTransform[] imageRTs;
        public Image[] images;
        public float tileWidth;     // current width of one tile in pixels
        public int tileCount;       // tiles per pattern (1 = no alternate, 2 = A/B)
        public float spriteAspect;  // sprite width/height (0 = standard mode)
        public float ovalAngle;     // current angle for OvalDrift
        public float direction;     // +1 or -1, randomized per build
        public float tileHeight;    // for OvalDrift grid tiling
        public int gridCols;        // columns in OvalDrift grid
        public int gridRows;        // rows in OvalDrift grid
    }

    private LayerInfo[] layerInfo;
    private float lastParentHeight;
    private float sharedOvalAngle;
    private float scrollDirection;  // +1 or -1, randomized each level

    private void Awake()
    {
        if (themes != null && themes.Length > 0)
            ApplyRandomTheme();
        else
            BuildLayers();
    }

    private void Update()
    {
        if (!scrollEnabled || layerInfo == null) return;

        // Check if parent height changed (aspect ratio switch) — rebuild FillHeight tiles
        RectTransform parentRT = (RectTransform)transform;
        float parentHeight = parentRT.rect.height;
        if (!Mathf.Approximately(parentHeight, lastParentHeight) && parentHeight > 0)
        {
            lastParentHeight = parentHeight;
            float parentWidth = parentRT.rect.width;
            for (int i = 0; i < layerInfo.Length; i++)
            {
                if (layerInfo[i].spriteAspect > 0 && layers[i].scrollMode != ScrollMode.OvalDrift)
                    RecalcFillHeight(ref layerInfo[i], parentWidth, parentHeight);
            }
        }

        // Advance shared oval angle (one "camera" orbit)
        sharedOvalAngle += globalSpeedMultiplier * Mathf.PI * 2f * Time.deltaTime * 0.025f;
        if (sharedOvalAngle > Mathf.PI * 2f)
            sharedOvalAngle -= Mathf.PI * 2f;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layerInfo[i].containerRT == null) continue;

            // Live-update alpha
            Color c = new Color(1f, 1f, 1f, layers[i].alpha);
            var imgs = layerInfo[i].images;
            if (imgs != null)
                for (int j = 0; j < imgs.Length; j++)
                    if (imgs[j] != null) imgs[j].color = c;

            float speed = layers[i].scrollSpeed * globalSpeedMultiplier;
            if (Mathf.Approximately(speed, 0f)) continue;

            RectTransform crt = layerInfo[i].containerRT;

            if (layers[i].scrollMode == ScrollMode.OvalDrift)
            {
                // Classic parallax: shared camera angle, scrollSpeed is depth factor
                // Layers with higher speed move more (closer to camera)
                float x = layers[i].ovalRadiusX * speed * Mathf.Cos(sharedOvalAngle) * scrollDirection;
                float y = layers[i].ovalRadiusY * speed * Mathf.Sin(sharedOvalAngle);
                crt.anchoredPosition = new Vector2(x, y);
            }
            else
            {
                Vector2 pos = crt.anchoredPosition;
                float parentWidth2 = ((RectTransform)crt.parent).rect.width;

                pos.x -= speed * parentWidth2 * Time.deltaTime * scrollDirection;

                float wrapDist;
                if (layerInfo[i].tileWidth > 0)
                    wrapDist = layerInfo[i].tileWidth * layerInfo[i].tileCount;
                else
                    wrapDist = parentWidth2;
                if (pos.x <= -wrapDist)
                    pos.x += wrapDist;
                else if (pos.x >= wrapDist)
                    pos.x -= wrapDist;

                crt.anchoredPosition = pos;
            }
        }
    }

    private void RecalcFillHeight(ref LayerInfo info, float parentWidth, float parentHeight)
    {
        float oldTileW = info.tileWidth;
        float tileW = parentHeight * info.spriteAspect;
        info.tileWidth = tileW;

        if (info.imageRTs == null) return;
        for (int c = 0; c < info.imageRTs.Length; c++)
        {
            if (info.imageRTs[c] == null) continue;
            info.imageRTs[c].anchoredPosition = new Vector2(c * tileW, 0f);
            info.imageRTs[c].sizeDelta = new Vector2(tileW, 0f);
        }

        // Scale scroll position proportionally so the visual position stays consistent
        if (info.containerRT != null && oldTileW > 0f)
        {
            Vector2 pos = info.containerRT.anchoredPosition;
            pos.x *= tileW / oldTileW;
            info.containerRT.anchoredPosition = pos;
        }
    }

    public void BuildLayers()
    {
        if (layerInfo != null)
        {
            for (int i = 0; i < layerInfo.Length; i++)
            {
                if (layerInfo[i].containerRT != null)
                    Destroy(layerInfo[i].containerRT.gameObject);
            }
        }

        if (layers == null || layers.Length == 0)
        {
            layerInfo = new LayerInfo[0];
            return;
        }

        Canvas.ForceUpdateCanvases();

        scrollDirection = Random.value < 0.5f ? -1f : 1f;
        layerInfo = new LayerInfo[layers.Length];
        RectTransform parentRT = GetComponent<RectTransform>();
        float parentWidth = parentRT.rect.width;
        float parentHeight = parentRT.rect.height;
        lastParentHeight = parentHeight;

        for (int i = 0; i < layers.Length; i++)
        {
            var layer = layers[i];
            if (layer.sprite == null) continue;

            float heightRatio = layer.sprite.texture.height / referenceHeight;

            var containerGO = new GameObject($"ParallaxContainer_{i}");
            containerGO.transform.SetParent(parentRT, false);
            RectTransform containerRT = containerGO.AddComponent<RectTransform>();

            if (layer.scrollMode == ScrollMode.OvalDrift)
            {
                // OvalDrift sets its own anchors below — skip the standard anchor setup
            }
            else
            {
                switch (layer.verticalAnchor)
                {
                    case VerticalAnchor.Stretch:
                    case VerticalAnchor.FillHeight:
                        containerRT.anchorMin = Vector2.zero;
                        containerRT.anchorMax = Vector2.one;
                        break;
                    case VerticalAnchor.Bottom:
                        containerRT.anchorMin = new Vector2(0f, 0f);
                        containerRT.anchorMax = new Vector2(1f, heightRatio);
                        break;
                    case VerticalAnchor.Top:
                        containerRT.anchorMin = new Vector2(0f, 1f - heightRatio);
                        containerRT.anchorMax = new Vector2(1f, 1f);
                        break;
                }
                containerRT.offsetMin = Vector2.zero;
                containerRT.offsetMax = Vector2.zero;
            }

            if (layer.scrollMode == ScrollMode.OvalDrift)
            {
                float spriteAspect = (float)layer.sprite.texture.width / layer.sprite.texture.height;
                float tileH = parentHeight;
                float tileW = tileH * spriteAspect;

                // Container: centered, explicit size
                containerRT.anchorMin = new Vector2(0.5f, 0.5f);
                containerRT.anchorMax = new Vector2(0.5f, 0.5f);
                containerRT.anchoredPosition = Vector2.zero;

                if (layer.ovalTileGrid)
                {
                    // Grid tiling for small repeating textures
                    float padX = layer.ovalRadiusX + tileW;
                    float padY = layer.ovalRadiusY + tileH * 0.5f;
                    float totalW = parentWidth + padX * 2f;
                    float totalH = parentHeight + padY * 2f;

                    int cols = Mathf.CeilToInt(totalW / tileW) + 1;
                    int rows = Mathf.CeilToInt(totalH / tileH) + 1;

                    containerRT.sizeDelta = new Vector2(cols * tileW, rows * tileH);

                    float startX = -(cols * tileW) / 2f;
                    float startY = -(rows * tileH) / 2f;
                    int total = cols * rows;

                    var imgs = new Image[total];
                    var rts = new RectTransform[total];
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c2 = 0; c2 < cols; c2++)
                        {
                            int idx = r * cols + c2;
                            CreateGridTile($"Layer_{i}_{r}_{c2}", containerRT, layer, layer.sprite,
                                startX + c2 * tileW, startY + r * tileH, tileW, tileH,
                                out rts[idx], out imgs[idx]);
                        }
                    }

                    layerInfo[i] = new LayerInfo
                    {
                        containerRT = containerRT,
                        imageRTs = rts,
                        images = imgs,
                        tileWidth = tileW,
                        tileHeight = tileH,
                        tileCount = 1,
                        spriteAspect = spriteAspect,
                        gridCols = cols,
                        gridRows = rows
                    };
                }
                else
                {
                    // Single image: stretch to cover screen + drift padding
                    float imgW = Mathf.Max(tileW, parentWidth) + layer.ovalRadiusX * 2f;
                    float imgH = parentHeight + layer.ovalRadiusY * 2f;
                    containerRT.sizeDelta = new Vector2(imgW, imgH);

                    var imgs = new Image[1];
                    var rts = new RectTransform[1];
                    CreateGridTile($"Layer_{i}_single", containerRT, layer, layer.sprite,
                        -imgW / 2f, -imgH / 2f, imgW, imgH,
                        out rts[0], out imgs[0]);

                    layerInfo[i] = new LayerInfo
                    {
                        containerRT = containerRT,
                        imageRTs = rts,
                        images = imgs,
                        tileWidth = imgW,
                        tileHeight = imgH,
                        tileCount = 1,
                        spriteAspect = spriteAspect
                    };
                }
            }
            else if (layer.verticalAnchor == VerticalAnchor.FillHeight)
            {
                float spriteAspect = (float)layer.sprite.texture.width / layer.sprite.texture.height;
                float tileW = parentHeight * spriteAspect;

                bool hasAlt = layer.alternateSprite != null;
                int tileCount = hasAlt ? 2 : 1;
                float patternWidth = tileW * tileCount;
                int patternsNeeded = Mathf.CeilToInt(parentWidth / patternWidth) + 1;
                // Extra pattern on the left so scrolling right is covered
                int totalCopies = (patternsNeeded + 1) * tileCount;

                var imgs = new Image[totalCopies];
                var rts = new RectTransform[totalCopies];
                // Start one pattern to the left of origin
                float startX = -patternWidth;
                for (int c = 0; c < totalCopies; c++)
                {
                    bool useAlt = hasAlt && (c % 2 == 1);
                    Sprite s = useAlt ? layer.alternateSprite : layer.sprite;
                    CreateFillHeightImage($"Layer_{i}_{c}_{s.name}", containerRT, layer, s,
                        startX + c * tileW, tileW, out rts[c], out imgs[c]);
                }

                layerInfo[i] = new LayerInfo
                {
                    containerRT = containerRT,
                    imageRTs = rts,
                    images = imgs,
                    tileWidth = tileW,
                    tileCount = tileCount,
                    spriteAspect = spriteAspect
                };
            }
            else
            {
                Sprite spriteB = layer.alternateSprite != null ? layer.alternateSprite : layer.sprite;
                var imgs = new Image[3];
                var rts = new RectTransform[3];
                // One copy to the left for right-scrolling coverage
                rts[0] = CreateAnchoredImage($"Layer_{i}L_{spriteB.name}", containerRT, layer, -1f, spriteB, 1f);
                imgs[0] = rts[0].GetComponent<Image>();
                rts[1] = CreateAnchoredImage($"Layer_{i}A_{layer.sprite.name}", containerRT, layer, 0f, layer.sprite, 1f);
                imgs[1] = rts[1].GetComponent<Image>();
                rts[2] = CreateAnchoredImage($"Layer_{i}B_{spriteB.name}", containerRT, layer, 1f, spriteB, 1f);
                imgs[2] = rts[2].GetComponent<Image>();

                layerInfo[i] = new LayerInfo
                {
                    containerRT = containerRT,
                    imageRTs = rts,
                    images = imgs,
                    tileWidth = 0f,
                    tileCount = 1,
                    spriteAspect = 0f
                };
            }
        }
    }

    // FillHeight images: anchored to stretch vertically, explicit pixel width, positioned by anchoredPosition
    private void CreateFillHeightImage(string name, RectTransform parent, ParallaxLayer layer, Sprite sprite,
        float xPos, float width, out RectTransform rt, out Image img)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        rt = go.AddComponent<RectTransform>();
        // Stretch vertically, pin to left edge
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(xPos, 0f);
        rt.sizeDelta = new Vector2(width, 0f); // height from anchors, explicit width

        img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.color = new Color(1f, 1f, 1f, layer.alpha);
        img.raycastTarget = false;
    }

    // OvalDrift grid tile: explicit position and size, no anchoring
    private void CreateGridTile(string name, RectTransform parent, ParallaxLayer layer, Sprite sprite,
        float xPos, float yPos, float width, float height, out RectTransform rt, out Image img)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(xPos, yPos);
        rt.sizeDelta = new Vector2(width, height);

        img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.color = new Color(1f, 1f, 1f, layer.alpha);
        img.raycastTarget = false;
    }

    // Standard anchor-based images (Stretch/Bottom/Top modes)
    private RectTransform CreateAnchoredImage(string name, RectTransform parent, ParallaxLayer layer, float xOffset, Sprite sprite, float widthFraction)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xOffset, 0f);
        rt.anchorMax = new Vector2(xOffset + widthFraction, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.color = new Color(1f, 1f, 1f, layer.alpha);
        img.raycastTarget = false;

        return rt;
    }

    public void ApplyRandomTheme()
    {
        if (themes == null || themes.Length == 0) return;

        var enabled = new System.Collections.Generic.List<int>();
        for (int i = 0; i < themes.Length; i++)
        {
            if (themes[i].enabled)
                enabled.Add(i);
        }
        if (enabled.Count == 0) return;

        int index;
        if (enabled.Count == 1)
        {
            index = enabled[0];
        }
        else
        {
            do { index = enabled[Random.Range(0, enabled.Count)]; }
            while (index == lastThemeIndex);
        }

        lastThemeIndex = index;
        SetLayers(themes[index].layers);
    }

    public void SetLayers(ParallaxLayer[] newLayers)
    {
        layers = newLayers;
        BuildLayers();
    }

    public void SetScrollEnabled(bool enabled)
    {
        scrollEnabled = enabled;
    }
}

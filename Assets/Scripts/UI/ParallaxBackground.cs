using UnityEngine;
using UnityEngine.UI;

public class ParallaxBackground : MonoBehaviour
{
    public enum VerticalAnchor { Stretch, Bottom, Top }

    [System.Serializable]
    public class ParallaxLayer
    {
        [Tooltip("Sprite asset for this layer")]
        public Sprite sprite;
        [Tooltip("Scroll speed in screen-widths per second (0.005 ≈ 3.3 min per full scroll)")]
        public float scrollSpeed;
        [Tooltip("Alpha transparency (1 = opaque, lower values dim the layer)")]
        [Range(0f, 1f)]
        public float alpha = 1f;
        [Tooltip("Vertical anchor: Stretch fills the screen, Bottom/Top preserves native height ratio")]
        public VerticalAnchor verticalAnchor = VerticalAnchor.Stretch;
    }

    [System.Serializable]
    public class ParallaxTheme
    {
        public string name;
        [Tooltip("Uncheck to exclude this theme from random selection")]
        public bool enabled = true;
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

    // Each layer has two Image copies (A and B) placed side by side for seamless wrapping
    private struct LayerPair
    {
        public RectTransform containerRT;
        public Image imageA;
        public Image imageB;
    }

    private LayerPair[] layerPairs;

    private void Awake()
    {
        if (themes != null && themes.Length > 0)
            ApplyRandomTheme();
        else
            BuildLayers();
    }

    private void Update()
    {
        if (!scrollEnabled || layerPairs == null) return;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layerPairs[i].containerRT == null) continue;

            // Live-update alpha so Inspector changes take effect immediately
            Color c = new Color(1f, 1f, 1f, layers[i].alpha);
            if (layerPairs[i].imageA != null) layerPairs[i].imageA.color = c;
            if (layerPairs[i].imageB != null) layerPairs[i].imageB.color = c;

            float speed = layers[i].scrollSpeed * globalSpeedMultiplier;
            if (Mathf.Approximately(speed, 0f)) continue;

            RectTransform crt = layerPairs[i].containerRT;
            Vector2 pos = crt.anchoredPosition;
            float parentWidth = ((RectTransform)crt.parent).rect.width;

            // Scroll left
            pos.x -= speed * parentWidth * Time.deltaTime;

            // Wrap: when scrolled one full image width, reset
            if (pos.x <= -parentWidth)
                pos.x += parentWidth;

            crt.anchoredPosition = pos;
        }
    }

    public void BuildLayers()
    {
        // Clean up existing
        if (layerPairs != null)
        {
            for (int i = 0; i < layerPairs.Length; i++)
            {
                if (layerPairs[i].containerRT != null)
                    Destroy(layerPairs[i].containerRT.gameObject);
            }
        }

        if (layers == null || layers.Length == 0)
        {
            layerPairs = new LayerPair[0];
            return;
        }

        layerPairs = new LayerPair[layers.Length];
        RectTransform parentRT = GetComponent<RectTransform>();

        for (int i = 0; i < layers.Length; i++)
        {
            var layer = layers[i];
            if (layer.sprite == null) continue;

            float heightRatio = layer.sprite.texture.height / referenceHeight;

            // Create a container that holds two copies side by side
            // Container is 2x parent width so both copies fit
            var containerGO = new GameObject($"ParallaxContainer_{i}");
            containerGO.transform.SetParent(parentRT, false);
            RectTransform containerRT = containerGO.AddComponent<RectTransform>();

            // Set vertical anchors based on layer type
            switch (layer.verticalAnchor)
            {
                case VerticalAnchor.Stretch:
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

            // Create image A (fills left half of doubled container area)
            var imgA = CreateLayerImage($"Layer_{i}A_{layer.sprite.name}", containerRT, layer, 0f);
            // Create image B (fills right half, adjacent to A)
            var imgB = CreateLayerImage($"Layer_{i}B_{layer.sprite.name}", containerRT, layer, 1f);

            layerPairs[i] = new LayerPair
            {
                containerRT = containerRT,
                imageA = imgA,
                imageB = imgB
            };
        }
    }

    private Image CreateLayerImage(string name, RectTransform parent, ParallaxLayer layer, float xOffset)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        // Each image is exactly parent-width wide, anchored to fill vertically
        // xOffset=0 for the first copy, xOffset=1 for the second (placed to the right)
        rt.anchorMin = new Vector2(xOffset, 0f);
        rt.anchorMax = new Vector2(xOffset + 1f, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.sprite = layer.sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.color = new Color(1f, 1f, 1f, layer.alpha);
        img.raycastTarget = false;

        return img;
    }

    public void ApplyRandomTheme()
    {
        if (themes == null || themes.Length == 0) return;

        // Collect enabled theme indices
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

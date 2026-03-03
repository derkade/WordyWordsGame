using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class RebuildWordBankPanel
{
    [MenuItem("Tools/Rebuild Word Bank Panel")]
    public static void Rebuild()
    {
        // Load default TMP font via AssetDatabase
        TMP_FontAsset defaultFont = null;
        var guids = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset");
        if (guids.Length > 0)
            defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (defaultFont == null)
            defaultFont = TMP_Settings.defaultFontAsset;
        Debug.Log($"TMP Font: {(defaultFont != null ? defaultFont.name : "NULL")} (found {guids.Length} assets)");

        // Find WordBankPanel
        GameObject panelGO = null;
        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null) { Debug.LogError("GameCanvas not found!"); return; }
        foreach (Transform child in canvas.transform)
        {
            if (child.name == "WordBankPanel") { panelGO = child.gameObject; break; }
        }
        if (panelGO == null) { Debug.LogError("WordBankPanel not found!"); return; }

        // Delete all children
        while (panelGO.transform.childCount > 0)
            Object.DestroyImmediate(panelGO.transform.GetChild(0).gameObject);

        // Panel is full-screen dim backdrop
        var panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        var panelImg = panelGO.GetComponent<Image>();
        if (panelImg == null) panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.5f);
        panelImg.raycastTarget = true;

        // === Outer Frame (gray, rounded) ===
        var outerGO = CreateUI("OuterFrame", panelGO.transform);
        var outerRT = outerGO.GetComponent<RectTransform>();
        outerRT.anchorMin = new Vector2(0.5f, 0.5f);
        outerRT.anchorMax = new Vector2(0.5f, 0.5f);
        outerRT.sizeDelta = new Vector2(520, 720);
        outerRT.anchoredPosition = new Vector2(0, 20);
        var outerImg = outerGO.AddComponent<Image>();
        outerImg.color = new Color(0.35f, 0.35f, 0.4f, 0.85f);
        outerImg.raycastTarget = false;

        // Apply rounded sprite to outer
        ApplyRoundedSprite(outerImg, 24);

        // === Inner Panel (black, rounded) ===
        var innerGO = CreateUI("InnerPanel", outerGO.transform);
        var innerRT = innerGO.GetComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(16, 16);
        innerRT.offsetMax = new Vector2(-16, -16);
        var innerImg = innerGO.AddComponent<Image>();
        innerImg.color = new Color(0.08f, 0.08f, 0.1f, 1f);
        innerImg.raycastTarget = false;
        ApplyRoundedSprite(innerImg, 24);

        // === Title ===
        var titleGO = CreateUI("WordBankTitle", innerGO.transform);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -12);
        titleRT.sizeDelta = new Vector2(-40, 50);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) titleTMP.font = defaultFont;
        titleTMP.text = "WORD BANK";
        titleTMP.fontSize = 32;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = Color.white;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.raycastTarget = false;

        // === Divider line under title ===
        var divGO = CreateUI("Divider", innerGO.transform);
        var divRT = divGO.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0, 1);
        divRT.anchorMax = new Vector2(1, 1);
        divRT.pivot = new Vector2(0.5f, 1);
        divRT.anchoredPosition = new Vector2(0, -65);
        divRT.sizeDelta = new Vector2(-40, 2);
        var divImg = divGO.AddComponent<Image>();
        divImg.color = new Color(0.3f, 0.3f, 0.35f, 1f);
        divImg.raycastTarget = false;

        // === Scroll View for word list ===
        var scrollGO = CreateUI("WordScroll", innerGO.transform);
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(10, 10);
        scrollRT.offsetMax = new Vector2(-10, -75);
        var scrollImg = scrollGO.AddComponent<Image>();
        scrollImg.color = Color.clear;
        scrollImg.raycastTarget = true;
        scrollGO.AddComponent<RectMask2D>();
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 30;

        // Content
        var contentGO = CreateUI("Content", scrollGO.transform);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);
        var contentSizer = contentGO.AddComponent<ContentSizeFitter>();
        contentSizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        // Word text (clickable TMP links)
        var textGO = CreateUI("WordBankText", contentGO.transform);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, 1);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.pivot = new Vector2(0.5f, 1);
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = new Vector2(-20, 0);
        var textTMP = textGO.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) textTMP.font = defaultFont;
        textTMP.text = "No bonus words found yet.";
        textTMP.fontSize = 26;
        textTMP.color = Color.white;
        textTMP.alignment = TextAlignmentOptions.TopLeft;
        textTMP.raycastTarget = true; // needed for link clicks
        textTMP.textWrappingMode = TMPro.TextWrappingModes.Normal;
        textTMP.margin = new Vector4(15, 10, 15, 10);
        textTMP.lineSpacing = 15;
        // ContentSizeFitter on text so it auto-sizes height from content
        var textSizer = textGO.AddComponent<ContentSizeFitter>();
        textSizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Add WordBankClickHandler
        var clickHandler = textGO.AddComponent<WordBankClickHandler>();

        // Lined background: child of textGO (same coordinate space for alignment),
        // top-anchored with large fixed height so lines extend to fill the viewport.
        // The scroll view's RectMask2D clips any overflow below.
        var linedBgGO = CreateUI("LinedBackground", textGO.transform);
        var linedBgRT = linedBgGO.GetComponent<RectTransform>();
        linedBgRT.anchorMin = new Vector2(0, 1);
        linedBgRT.anchorMax = new Vector2(1, 1);
        linedBgRT.pivot = new Vector2(0.5f, 1);
        linedBgRT.anchoredPosition = Vector2.zero;
        linedBgRT.sizeDelta = new Vector2(0, 1200);
        linedBgGO.transform.SetAsFirstSibling();
        var linedBgImg = linedBgGO.AddComponent<Image>();
        linedBgImg.color = Color.white;
        linedBgImg.raycastTarget = false;
        var linedBg = linedBgGO.AddComponent<LinedTextBackground>();
        var linedSO = new SerializedObject(linedBg);
        linedSO.FindProperty("targetText").objectReferenceValue = textTMP;
        linedSO.FindProperty("lineColor").colorValue = new Color(1f, 1f, 1f, 0.12f);
        linedSO.FindProperty("lineThickness").intValue = 1;
        linedSO.FindProperty("verticalOffset").floatValue = 4f;
        linedSO.ApplyModifiedProperties();

        // === Close Button (X, top right of outer frame) ===
        var closeGO = CreateUI("WordBankCloseButton", outerGO.transform);
        var closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.pivot = new Vector2(0.5f, 0.5f);
        closeRT.anchoredPosition = new Vector2(10, 10);
        closeRT.sizeDelta = new Vector2(48, 48);
        var closeImg = closeGO.AddComponent<Image>();
        closeImg.color = new Color(0.4f, 0.4f, 0.4f, 0.9f);
        ApplyRoundedSprite(closeImg, 20);
        var closeBtn = closeGO.AddComponent<Button>();
        var colors = closeBtn.colors;
        colors.highlightedColor = new Color(0.6f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        closeBtn.colors = colors;

        var xGO = CreateUI("XText", closeGO.transform);
        var xRT = xGO.GetComponent<RectTransform>();
        xRT.anchorMin = Vector2.zero;
        xRT.anchorMax = Vector2.one;
        xRT.offsetMin = Vector2.zero;
        xRT.offsetMax = Vector2.zero;
        var xTMP = xGO.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) xTMP.font = defaultFont;
        xTMP.text = "X";
        xTMP.fontSize = 28;
        xTMP.color = Color.white;
        xTMP.alignment = TextAlignmentOptions.Center;
        xTMP.raycastTarget = false;

        // === Wire up GameManager references ===
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            var so = new SerializedObject(gm);
            so.FindProperty("wordBankPanel").objectReferenceValue = panelGO;
            so.FindProperty("wordBankCG").objectReferenceValue = panelGO.GetComponent<CanvasGroup>();
            so.FindProperty("wordBankText").objectReferenceValue = textTMP;
            so.FindProperty("wordBankCloseButton").objectReferenceValue = closeBtn;
            so.FindProperty("wordBankClickHandler").objectReferenceValue = clickHandler;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(gm);
        }

        EditorUtility.SetDirty(panelGO);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("Word Bank Panel rebuilt successfully!");
    }

    private static void ApplyRoundedSprite(Image img, int radius)
    {
        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color white = Color.white;
        Color clear = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = 0f, dy = 0f;
                bool inCorner = false;

                if (x < radius && y < radius) { dx = radius - x; dy = radius - y; inCorner = true; }
                else if (x >= size - radius && y < radius) { dx = x - (size - radius - 1); dy = radius - y; inCorner = true; }
                else if (x < radius && y >= size - radius) { dx = radius - x; dy = y - (size - radius - 1); inCorner = true; }
                else if (x >= size - radius && y >= size - radius) { dx = x - (size - radius - 1); dy = y - (size - radius - 1); inCorner = true; }

                if (inCorner)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > radius)
                        tex.SetPixel(x, y, clear);
                    else if (dist > radius - 1.5f)
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 1f - (dist - (radius - 1.5f)) / 1.5f));
                    else
                        tex.SetPixel(x, y, white);
                }
                else
                {
                    tex.SetPixel(x, y, white);
                }
            }
        }
        tex.Apply();

        Vector4 border = new Vector4(radius, radius, radius, radius);
        img.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        img.type = Image.Type.Sliced;
    }

    private static GameObject CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        return go;
    }
}

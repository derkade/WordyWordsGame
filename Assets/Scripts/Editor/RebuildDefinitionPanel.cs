using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class RebuildDefinitionPanel
{
    [MenuItem("Tools/Rebuild Definition Panel")]
    public static void Rebuild()
    {
        // Find the DefinitionPanel
        var panelGO = GameObject.Find("GameCanvas/DefinitionPanel");
        if (panelGO == null)
        {
            // Try inactive
            var canvas = GameObject.Find("GameCanvas");
            if (canvas == null) { Debug.LogError("GameCanvas not found!"); return; }
            foreach (Transform child in canvas.transform)
            {
                if (child.name == "DefinitionPanel") { panelGO = child.gameObject; break; }
            }
        }
        if (panelGO == null) { Debug.LogError("DefinitionPanel not found!"); return; }

        // Load default TMP font via AssetDatabase
        TMP_FontAsset defaultFont = null;
        var guids = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset");
        if (guids.Length > 0)
            defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (defaultFont == null)
            defaultFont = TMP_Settings.defaultFontAsset;
        Debug.Log($"TMP Font: {(defaultFont != null ? defaultFont.name : "NULL")} (found {guids.Length} assets)");

        // Delete all children
        while (panelGO.transform.childCount > 0)
            Object.DestroyImmediate(panelGO.transform.GetChild(0).gameObject);

        // DefinitionPanel itself is the full-screen dim backdrop
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
        var outerGO = CreateUIObject("OuterFrame", panelGO.transform);
        var outerRT = outerGO.GetComponent<RectTransform>();
        outerRT.anchorMin = new Vector2(0.5f, 0.5f);
        outerRT.anchorMax = new Vector2(0.5f, 0.5f);
        outerRT.sizeDelta = new Vector2(520, 720);
        outerRT.anchoredPosition = new Vector2(0, 20);
        var outerImg = outerGO.AddComponent<Image>();
        outerImg.color = new Color(0.35f, 0.35f, 0.4f, 0.85f);
        outerImg.raycastTarget = false;

        // === Inner Panel (black, rounded) ===
        var innerGO = CreateUIObject("InnerPanel", outerGO.transform);
        var innerRT = innerGO.GetComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(16, 80); // bottom padding for nav
        innerRT.offsetMax = new Vector2(-16, -16); // top/side padding
        var innerImg = innerGO.AddComponent<Image>();
        innerImg.color = new Color(0.08f, 0.08f, 0.1f, 1f);
        innerImg.raycastTarget = false;

        // === Word Title ===
        var titleGO = CreateUIObject("WordTitle", innerGO.transform);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -12);
        titleRT.sizeDelta = new Vector2(-40, 50);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) titleTMP.font = defaultFont;
        titleTMP.text = "WORD";
        titleTMP.fontSize = 36;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = Color.white;
        titleTMP.alignment = TextAlignmentOptions.Left;
        titleTMP.raycastTarget = false;
        titleTMP.margin = new Vector4(20, 0, 0, 0);

        // === Phonetic Text ===
        var phoneticGO = CreateUIObject("PhoneticText", innerGO.transform);
        var phoneticRT = phoneticGO.GetComponent<RectTransform>();
        phoneticRT.anchorMin = new Vector2(0, 1);
        phoneticRT.anchorMax = new Vector2(1, 1);
        phoneticRT.pivot = new Vector2(0.5f, 1);
        phoneticRT.anchoredPosition = new Vector2(0, -60);
        phoneticRT.sizeDelta = new Vector2(-40, 30);
        var phoneticTMP = phoneticGO.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) phoneticTMP.font = defaultFont;
        phoneticTMP.text = "";
        phoneticTMP.fontSize = 20;
        phoneticTMP.fontStyle = FontStyles.Italic;
        phoneticTMP.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        phoneticTMP.alignment = TextAlignmentOptions.Left;
        phoneticTMP.raycastTarget = false;
        phoneticTMP.margin = new Vector4(20, 0, 0, 0);

        // === Scroll View for definitions ===
        var scrollGO = CreateUIObject("DefinitionScroll", innerGO.transform);
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(10, 10);
        scrollRT.offsetMax = new Vector2(-10, -90);
        var scrollImg = scrollGO.AddComponent<Image>();
        scrollImg.color = new Color(0, 0, 0, 0); // transparent mask
        var mask = scrollGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 30;

        // Content object for scroll
        var contentGO = CreateUIObject("Content", scrollGO.transform);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);
        var contentSizer = contentGO.AddComponent<ContentSizeFitter>();
        contentSizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        // Definition text inside content
        var defGO = CreateUIObject("DefinitionText", contentGO.transform);
        var defRT = defGO.GetComponent<RectTransform>();
        defRT.anchorMin = new Vector2(0, 1);
        defRT.anchorMax = new Vector2(1, 1);
        defRT.pivot = new Vector2(0.5f, 1);
        defRT.anchoredPosition = Vector2.zero;
        defRT.sizeDelta = new Vector2(-20, 0);
        var defTMP = defGO.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) defTMP.font = defaultFont;
        defTMP.text = "";
        defTMP.fontSize = 22;
        defTMP.color = Color.white;
        defTMP.alignment = TextAlignmentOptions.TopLeft;
        defTMP.raycastTarget = false;
        defTMP.margin = new Vector4(10, 0, 10, 0);
        defTMP.enableWordWrapping = true;

        // === Loading Text ===
        var loadingGO = CreateUIObject("LoadingText", innerGO.transform);
        var loadingRT = loadingGO.GetComponent<RectTransform>();
        loadingRT.anchorMin = new Vector2(0.5f, 0.5f);
        loadingRT.anchorMax = new Vector2(0.5f, 0.5f);
        loadingRT.sizeDelta = new Vector2(300, 50);
        loadingRT.anchoredPosition = Vector2.zero;
        var loadingTMP = loadingGO.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) loadingTMP.font = defaultFont;
        loadingTMP.text = "Loading...";
        loadingTMP.fontSize = 24;
        loadingTMP.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        loadingTMP.alignment = TextAlignmentOptions.Center;
        loadingTMP.fontStyle = FontStyles.Italic;
        loadingTMP.raycastTarget = false;

        // === Close Button (circle X, top right of outer frame) ===
        var closeGO = CreateUIObject("DefCloseButton", outerGO.transform);
        var closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.pivot = new Vector2(0.5f, 0.5f);
        closeRT.anchoredPosition = new Vector2(10, 10);
        closeRT.sizeDelta = new Vector2(48, 48);
        var closeImg = closeGO.AddComponent<Image>();
        closeImg.color = new Color(0.4f, 0.4f, 0.4f, 0.9f);
        // Reuse rounded rect sprite for circular button
        Sprite closeBtnSprite = GenerateRoundedRectSpriteStatic(64, 64, 32);
        closeImg.sprite = closeBtnSprite;
        closeImg.type = Image.Type.Sliced;
        var closeBtn = closeGO.AddComponent<Button>();
        var closeBtnColors = closeBtn.colors;
        closeBtnColors.highlightedColor = new Color(0.6f, 0.3f, 0.3f, 1f);
        closeBtnColors.pressedColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        closeBtn.colors = closeBtnColors;

        // X text on close button
        var xTextGO = CreateUIObject("XText", closeGO.transform);
        var xTextRT = xTextGO.GetComponent<RectTransform>();
        xTextRT.anchorMin = Vector2.zero;
        xTextRT.anchorMax = Vector2.one;
        xTextRT.offsetMin = Vector2.zero;
        xTextRT.offsetMax = Vector2.zero;
        var xTMP = xTextGO.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) xTMP.font = defaultFont;
        xTMP.text = "X";
        xTMP.fontSize = 28;
        xTMP.color = Color.white;
        xTMP.alignment = TextAlignmentOptions.Center;
        xTMP.raycastTarget = false;

        // === Navigation Bar (bottom of outer frame) ===
        var navGO = CreateUIObject("NavBar", outerGO.transform);
        var navRT = navGO.GetComponent<RectTransform>();
        navRT.anchorMin = new Vector2(0, 0);
        navRT.anchorMax = new Vector2(1, 0);
        navRT.pivot = new Vector2(0.5f, 0);
        navRT.anchoredPosition = new Vector2(0, 8);
        navRT.sizeDelta = new Vector2(0, 60);

        // Prev button
        var prevGO = CreateUIObject("PrevButton", navGO.transform);
        var prevRT = prevGO.GetComponent<RectTransform>();
        prevRT.anchorMin = new Vector2(0, 0);
        prevRT.anchorMax = new Vector2(0, 1);
        prevRT.pivot = new Vector2(0, 0.5f);
        prevRT.anchoredPosition = new Vector2(40, 0);
        prevRT.sizeDelta = new Vector2(60, 0);
        var prevImg = prevGO.AddComponent<Image>();
        prevImg.color = new Color(0, 0, 0, 0); // transparent hit area
        var prevBtn = prevGO.AddComponent<Button>();

        var prevTextGO = CreateUIObject("Text", prevGO.transform);
        var prevTextRT = prevTextGO.GetComponent<RectTransform>();
        prevTextRT.anchorMin = Vector2.zero;
        prevTextRT.anchorMax = Vector2.one;
        prevTextRT.offsetMin = Vector2.zero;
        prevTextRT.offsetMax = Vector2.zero;
        var prevTMP = prevTextGO.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) prevTMP.font = defaultFont;
        prevTMP.text = "<";
        prevTMP.fontSize = 36;
        prevTMP.color = Color.white;
        prevTMP.alignment = TextAlignmentOptions.Center;
        prevTMP.raycastTarget = false;

        // Count text
        var countGO = CreateUIObject("NavCount", navGO.transform);
        var countRT = countGO.GetComponent<RectTransform>();
        countRT.anchorMin = new Vector2(0.5f, 0);
        countRT.anchorMax = new Vector2(0.5f, 1);
        countRT.pivot = new Vector2(0.5f, 0.5f);
        countRT.anchoredPosition = Vector2.zero;
        countRT.sizeDelta = new Vector2(100, 0);
        var countTMP = countGO.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) countTMP.font = defaultFont;
        countTMP.text = "1/1";
        countTMP.fontSize = 28;
        countTMP.color = Color.white;
        countTMP.alignment = TextAlignmentOptions.Center;
        countTMP.raycastTarget = false;

        // Next button
        var nextGO = CreateUIObject("NextButton", navGO.transform);
        var nextRT = nextGO.GetComponent<RectTransform>();
        nextRT.anchorMin = new Vector2(1, 0);
        nextRT.anchorMax = new Vector2(1, 1);
        nextRT.pivot = new Vector2(1, 0.5f);
        nextRT.anchoredPosition = new Vector2(-40, 0);
        nextRT.sizeDelta = new Vector2(60, 0);
        var nextImg = nextGO.AddComponent<Image>();
        nextImg.color = new Color(0, 0, 0, 0);
        var nextBtn = nextGO.AddComponent<Button>();

        var nextTextGO = CreateUIObject("Text", nextGO.transform);
        var nextTextRT = nextTextGO.GetComponent<RectTransform>();
        nextTextRT.anchorMin = Vector2.zero;
        nextTextRT.anchorMax = Vector2.one;
        nextTextRT.offsetMin = Vector2.zero;
        nextTextRT.offsetMax = Vector2.zero;
        var nextTMP = nextTextGO.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) nextTMP.font = defaultFont;
        nextTMP.text = ">";
        nextTMP.fontSize = 36;
        nextTMP.color = Color.white;
        nextTMP.alignment = TextAlignmentOptions.Center;
        nextTMP.raycastTarget = false;

        // === Wire up the DefinitionPanel component ===
        var panel = panelGO.GetComponent<DefinitionPanel>();
        if (panel == null) panel = panelGO.AddComponent<DefinitionPanel>();

        var so = new SerializedObject(panel);
        so.FindProperty("canvasGroup").objectReferenceValue = panelGO.GetComponent<CanvasGroup>();
        so.FindProperty("wordTitle").objectReferenceValue = titleTMP;
        so.FindProperty("phoneticText").objectReferenceValue = phoneticTMP;
        so.FindProperty("definitionText").objectReferenceValue = defTMP;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
        so.FindProperty("loadingIndicator").objectReferenceValue = loadingGO;
        so.FindProperty("prevButton").objectReferenceValue = prevBtn;
        so.FindProperty("nextButton").objectReferenceValue = nextBtn;
        so.FindProperty("navCountText").objectReferenceValue = countTMP;
        so.FindProperty("navBar").objectReferenceValue = navGO;
        so.FindProperty("outerFrame").objectReferenceValue = outerImg;
        so.FindProperty("innerPanel").objectReferenceValue = innerImg;
        so.FindProperty("cornerRadius").floatValue = 24f;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(panelGO);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("Definition Panel rebuilt successfully!");
    }

    private static Sprite GenerateRoundedRectSpriteStatic(int width, int height, int radius)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color white = Color.white;
        Color clear = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = 0f, dy = 0f;
                bool inCorner = false;
                if (x < radius && y < radius) { dx = radius - x; dy = radius - y; inCorner = true; }
                else if (x >= width - radius && y < radius) { dx = x - (width - radius - 1); dy = radius - y; inCorner = true; }
                else if (x < radius && y >= height - radius) { dx = radius - x; dy = y - (height - radius - 1); inCorner = true; }
                else if (x >= width - radius && y >= height - radius) { dx = x - (width - radius - 1); dy = y - (height - radius - 1); inCorner = true; }
                if (inCorner)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > radius) tex.SetPixel(x, y, clear);
                    else if (dist > radius - 1.5f) tex.SetPixel(x, y, new Color(1f, 1f, 1f, 1f - (dist - (radius - 1.5f)) / 1.5f));
                    else tex.SetPixel(x, y, white);
                }
                else tex.SetPixel(x, y, white);
            }
        }
        tex.Apply();
        Vector4 border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        return go;
    }
}

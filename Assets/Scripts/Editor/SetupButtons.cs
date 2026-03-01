using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class SetupButtons
{
    [MenuItem("Tools/Setup Button Bar")]
    public static void Setup()
    {
        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null) { Debug.LogError("GameCanvas not found!"); return; }

        // Find ButtonBar
        Transform buttonBar = null;
        foreach (Transform child in canvas.transform)
        {
            if (child.name == "ButtonBar") { buttonBar = child; break; }
        }
        if (buttonBar == null) { Debug.LogError("ButtonBar not found!"); return; }

        // Generate rounded sprite
        Sprite roundedSprite = GenerateRoundedRectSprite(128, 20);

        // Find buttons
        Button hintBtn = null, wordBankBtn = null;
        foreach (Transform child in buttonBar)
        {
            if (child.name == "HintButton") hintBtn = child.GetComponent<Button>();
            if (child.name == "WordBankButton") wordBankBtn = child.GetComponent<Button>();
        }

        if (hintBtn != null) SetupButton(hintBtn.gameObject, roundedSprite, 180, 60);
        if (wordBankBtn != null) SetupButton(wordBankBtn.gameObject, roundedSprite, 180, 60);

        EditorUtility.SetDirty(buttonBar.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("Button Bar set up successfully!");
    }

    private static void SetupButton(GameObject btnGO, Sprite sprite, float width, float height)
    {
        // Set size via LayoutElement so HorizontalLayoutGroup respects it
        var layout = btnGO.GetComponent<LayoutElement>();
        if (layout == null) layout = btnGO.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        // Apply rounded sprite
        var img = btnGO.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
        }

        EditorUtility.SetDirty(btnGO);
    }

    private static Sprite GenerateRoundedRectSprite(int size, int radius)
    {
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
                    if (dist > radius) tex.SetPixel(x, y, clear);
                    else if (dist > radius - 1.5f) tex.SetPixel(x, y, new Color(1f, 1f, 1f, 1f - (dist - (radius - 1.5f)) / 1.5f));
                    else tex.SetPixel(x, y, white);
                }
                else tex.SetPixel(x, y, white);
            }
        }
        tex.Apply();
        Vector4 border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }
}

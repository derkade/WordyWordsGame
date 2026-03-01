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

        // Create SDF rounded rect material
        var shader = Shader.Find("UI/RoundedRect");
        if (shader == null) { Debug.LogError("UI/RoundedRect shader not found!"); return; }

        // Find buttons
        Button hintBtn = null, wordBankBtn = null;
        foreach (Transform child in buttonBar)
        {
            if (child.name == "HintButton") hintBtn = child.GetComponent<Button>();
            if (child.name == "WordBankButton") wordBankBtn = child.GetComponent<Button>();
        }

        float width = 180f, height = 60f;
        if (hintBtn != null) SetupButton(hintBtn.gameObject, shader, width, height);
        if (wordBankBtn != null) SetupButton(wordBankBtn.gameObject, shader, width, height);

        EditorUtility.SetDirty(buttonBar.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("Button Bar set up successfully!");
    }

    private static void SetupButton(GameObject btnGO, Shader shader, float width, float height)
    {
        // Set sizeDelta directly (layout group has childControl off)
        var rt = btnGO.GetComponent<RectTransform>();
        if (rt != null)
            rt.sizeDelta = new Vector2(width, height);

        // Apply SDF rounded rect material
        var img = btnGO.GetComponent<Image>();
        if (img != null)
        {
            // Clear any old sprite
            img.sprite = null;
            img.type = Image.Type.Simple;

            var mat = new Material(shader);
            mat.SetVector("_RectSize", new Vector4(width, height, 0, 0));
            mat.SetFloat("_Radius", 14f);
            img.material = mat;
        }

        EditorUtility.SetDirty(btnGO);
    }
}

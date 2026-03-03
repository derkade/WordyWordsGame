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

        // Find buttons
        Button hintBtn = null, wordBankBtn = null;
        foreach (Transform child in buttonBar)
        {
            if (child.name == "HintButton") hintBtn = child.GetComponent<Button>();
            if (child.name == "WordBankButton") wordBankBtn = child.GetComponent<Button>();
        }

        float width = 180f, height = 60f;
        if (hintBtn != null) SetupButton(hintBtn.gameObject, width, height);
        if (wordBankBtn != null) SetupButton(wordBankBtn.gameObject, width, height);

        EditorUtility.SetDirty(buttonBar.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("Button Bar set up successfully!");
    }

    private static void SetupButton(GameObject btnGO, float width, float height)
    {
        // Set sizeDelta directly (layout group has childControl off)
        var rt = btnGO.GetComponent<RectTransform>();
        if (rt != null)
            rt.sizeDelta = new Vector2(width, height);

        // Clear any old sprite/material
        var img = btnGO.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = null;
            img.type = Image.Type.Simple;
            img.material = null;
        }

        // Add SDFRoundedImage component (remove old one if exists)
        var existing = btnGO.GetComponent<SDFRoundedImage>();
        if (existing != null)
            Object.DestroyImmediate(existing);

        var comp = btnGO.AddComponent<SDFRoundedImage>();
        var so = new SerializedObject(comp);
        so.FindProperty("cornerRadius").floatValue = 14f;
        so.FindProperty("borderWidth").floatValue = 2f;
        so.FindProperty("borderColor").colorValue = Color.black;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(btnGO);
    }
}

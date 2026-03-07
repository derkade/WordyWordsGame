using UnityEngine;
using UnityEditor;

public static class SetupUnderwaterTheme
{
    [MenuItem("Tools/Add Underwater Background Theme")]
    public static void AddUnderwaterTheme()
    {
        string basePath = "Assets/Art/Backgrounds/Underwater/";
        string[] textures = { "far.png", "sand.png", "foreground-2.png", "foreground-1.png" };

        foreach (string tex in textures)
        {
            string path = basePath + tex;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogWarning($"Not found: {path}"); continue; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
            Debug.Log($"Reimported {tex} (Sprite, Bilinear, uncompressed, max 4096)");
        }

        var bg = Object.FindFirstObjectByType<ParallaxBackground>();
        if (bg == null) { Debug.LogError("ParallaxBackground not found!"); return; }

        var so = new SerializedObject(bg);
        var themesProp = so.FindProperty("themes");

        // Remove existing Underwater theme
        for (int i = 0; i < themesProp.arraySize; i++)
        {
            if (themesProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "Underwater")
            {
                themesProp.DeleteArrayElementAtIndex(i);
                break;
            }
        }

        int idx = themesProp.arraySize;
        themesProp.InsertArrayElementAtIndex(idx);
        var theme = themesProp.GetArrayElementAtIndex(idx);
        theme.FindPropertyRelative("name").stringValue = "Underwater";
        theme.FindPropertyRelative("enabled").boolValue = true;

        var layersProp = theme.FindPropertyRelative("layers");
        layersProp.arraySize = 3;

        // 3 layers: far (full bg stretch), sand (mid coral), foreground-1 + foreground-2 alternating
        string[] spritePaths =    { basePath + "far.png", basePath + "sand.png", basePath + "foreground-1.png" };
        string[] altSpritePaths = { null,                 null,                  basePath + "foreground-2.png" };
        float[] speeds =  { 0f, 0.003f, 0.008f };
        float[] alphas =  { 1f, 1f, 1f };
        int[] anchors =   { 0, 3, 3 }; // Stretch for solid bg, FillHeight for pixel art layers

        for (int i = 0; i < 3; i++)
        {
            var layer = layersProp.GetArrayElementAtIndex(i);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePaths[i]);
            if (sprite == null) { Debug.LogWarning($"Could not load: {spritePaths[i]}"); continue; }

            layer.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            layer.FindPropertyRelative("scrollSpeed").floatValue = speeds[i];
            layer.FindPropertyRelative("alpha").floatValue = alphas[i];
            layer.FindPropertyRelative("verticalAnchor").enumValueIndex = anchors[i];

            if (altSpritePaths[i] != null)
            {
                var altSprite = AssetDatabase.LoadAssetAtPath<Sprite>(altSpritePaths[i]);
                if (altSprite != null)
                    layer.FindPropertyRelative("alternateSprite").objectReferenceValue = altSprite;
            }
            else
            {
                layer.FindPropertyRelative("alternateSprite").objectReferenceValue = null;
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(bg);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("Underwater background theme added successfully!");
    }
}

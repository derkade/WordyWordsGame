using UnityEngine;
using UnityEditor;

public static class SetupUnderwaterDivingTheme
{
    [MenuItem("Tools/Add Underwater Diving Background Theme")]
    public static void AddUnderwaterDivingTheme()
    {
        string basePath = "Assets/Art/Backgrounds/UnderwaterDiving/";
        string[] textures = { "background.png", "midground.png", "foreground.png" };

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

        // Remove existing theme with same name
        for (int i = 0; i < themesProp.arraySize; i++)
        {
            if (themesProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "UnderwaterDiving")
            {
                themesProp.DeleteArrayElementAtIndex(i);
                break;
            }
        }

        int idx = themesProp.arraySize;
        themesProp.InsertArrayElementAtIndex(idx);
        var theme = themesProp.GetArrayElementAtIndex(idx);
        theme.FindPropertyRelative("name").stringValue = "UnderwaterDiving";
        theme.FindPropertyRelative("enabled").boolValue = true;
        theme.FindPropertyRelative("revealedCellColor").colorValue = new Color(0.15f, 0.25f, 0.30f, 1f);

        var layersProp = theme.FindPropertyRelative("layers");
        layersProp.arraySize = 3;

        // Layer 0: background.png — deep water, gentle oval drift (grid tiled)
        // Layer 1: midground.png — coral vines, larger oval for parallax (grid tiled)
        // Layer 2: foreground.png — assembled cave frame, single image (not tiled)
        string[] spritePaths = { basePath + "background.png", basePath + "midground.png", basePath + "foreground.png" };
        float[] speeds = { 0.3f, 0.6f, 1.0f };  // depth factor: bg barely moves, fg moves most
        float[] alphas = { 1f, 1f, 1f };
        float[] radiiX = { 140f, 140f, 140f };  // same base orbit for all
        float[] radiiY = { 90f, 90f, 90f };
        bool[] tileGrid = { true, true, false }; // foreground is single image, not tiled

        for (int i = 0; i < 3; i++)
        {
            var layer = layersProp.GetArrayElementAtIndex(i);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePaths[i]);
            if (sprite == null) { Debug.LogWarning($"Could not load: {spritePaths[i]}"); continue; }

            layer.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            layer.FindPropertyRelative("alternateSprite").objectReferenceValue = null;
            layer.FindPropertyRelative("scrollSpeed").floatValue = speeds[i];
            layer.FindPropertyRelative("alpha").floatValue = alphas[i];
            layer.FindPropertyRelative("verticalAnchor").enumValueIndex = 0;
            layer.FindPropertyRelative("scrollMode").enumValueIndex = 1; // OvalDrift
            layer.FindPropertyRelative("ovalRadiusX").floatValue = radiiX[i];
            layer.FindPropertyRelative("ovalRadiusY").floatValue = radiiY[i];
            layer.FindPropertyRelative("ovalTileGrid").boolValue = tileGrid[i];
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(bg);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("Underwater Diving background theme added successfully!");
    }
}

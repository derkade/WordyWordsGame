using UnityEngine;
using UnityEditor;

public static class SetupForestTheme
{
    [MenuItem("Tools/Add Forest Background Theme")]
    public static void AddForestTheme()
    {
        // Step 1: Set texture import settings to Sprite
        string basePath = "Assets/Art/Backgrounds/Forest/";
        string[] textures = {
            "01_Sky.png", "02_Forest.png", "03_Forest.png", "04_Forest.png",
            "05_Forest.png", "06_Particles.png", "07_Forest.png",
            "08_Particles.png", "09_Bushes.png", "10_Mist.png"
        };

        foreach (string tex in textures)
        {
            string path = basePath + tex;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Could not find texture importer for: {path}");
                continue;
            }

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                Debug.Log($"Set {tex} to Sprite type");
            }
        }

        // Step 2: Find ParallaxBackground and add theme
        var bg = Object.FindFirstObjectByType<ParallaxBackground>();
        if (bg == null)
        {
            Debug.LogError("ParallaxBackground not found in scene!");
            return;
        }

        // Access themes via SerializedObject
        var so = new SerializedObject(bg);
        var themesProp = so.FindProperty("themes");

        // Check if Forest theme already exists
        for (int i = 0; i < themesProp.arraySize; i++)
        {
            var nameProp = themesProp.GetArrayElementAtIndex(i).FindPropertyRelative("name");
            if (nameProp.stringValue == "Forest")
            {
                Debug.Log("Forest theme already exists, updating it.");
                themesProp.DeleteArrayElementAtIndex(i);
                break;
            }
        }

        // Add new theme element
        int idx = themesProp.arraySize;
        themesProp.InsertArrayElementAtIndex(idx);
        var theme = themesProp.GetArrayElementAtIndex(idx);
        theme.FindPropertyRelative("name").stringValue = "Forest";
        theme.FindPropertyRelative("enabled").boolValue = true;
        theme.FindPropertyRelative("revealedCellColor").colorValue = new Color(0.20f, 0.30f, 0.18f, 1f);

        var layersProp = theme.FindPropertyRelative("layers");
        layersProp.arraySize = 10;

        // Layer config: sprite path, scroll speed, alpha, vertical anchor (0=Stretch, 1=Bottom, 2=Top)
        string[] spritePaths = {
            basePath + "01_Sky.png",       // sky
            basePath + "02_Forest.png",    // distant haze
            basePath + "03_Forest.png",    // far tree trunks
            basePath + "04_Forest.png",    // mid tree trunks
            basePath + "05_Forest.png",    // closer trees
            basePath + "06_Particles.png", // floating particles (back)
            basePath + "07_Forest.png",    // nearest trees
            basePath + "08_Particles.png", // floating particles (front)
            basePath + "09_Bushes.png",    // foreground bushes
            basePath + "10_Mist.png"       // mist overlay
        };
        float[] speeds =  { 0f, 0.002f, 0.003f, 0.004f, 0.006f, 0.005f, 0.008f, 0.007f, 0.012f, 0.001f };
        float[] alphas =  { 1f, 1f, 1f, 1f, 1f, 0.6f, 1f, 0.5f, 1f, 0.08f };
        int[] anchors =   { 0, 1, 1, 1, 1, 0, 1, 0, 1, 0 }; // Stretch for sky/particles/mist, Bottom for trees/bushes

        for (int i = 0; i < 10; i++)
        {
            var layer = layersProp.GetArrayElementAtIndex(i);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePaths[i]);
            if (sprite == null)
            {
                Debug.LogWarning($"Could not load sprite at: {spritePaths[i]}");
                continue;
            }

            layer.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            layer.FindPropertyRelative("scrollSpeed").floatValue = speeds[i];
            layer.FindPropertyRelative("alpha").floatValue = alphas[i];
            layer.FindPropertyRelative("verticalAnchor").enumValueIndex = anchors[i];
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(bg);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("Forest background theme added successfully!");
    }
}

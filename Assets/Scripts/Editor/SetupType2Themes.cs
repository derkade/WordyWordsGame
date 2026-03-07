using UnityEngine;
using UnityEditor;

public static class SetupType2Themes
{
    [MenuItem("Tools/Add Type2 Background Themes")]
    public static void AddType2Themes()
    {
        // Ensure type2 textures are imported as Sprites
        string[][] type2Paths = {
            new[] {
                "Assets/Art/Backgrounds/Jungle/type2/bg.png",
                "Assets/Art/Backgrounds/Jungle/type2/clouds.png",
                "Assets/Art/Backgrounds/Jungle/type2/mountains.png"
            },
            new[] {
                "Assets/Art/Backgrounds/Desert/type2/bg.png",
                "Assets/Art/Backgrounds/Desert/type2/clouds.png",
                "Assets/Art/Backgrounds/Desert/type2/dune.png"
            },
            new[] {
                "Assets/Art/Backgrounds/IceAge/type2/bg.png",
                "Assets/Art/Backgrounds/IceAge/type2/clouds.png",
                "Assets/Art/Backgrounds/IceAge/type2/mountains.png"
            }
        };

        foreach (var paths in type2Paths)
        {
            foreach (string path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) { Debug.LogWarning($"Not found: {path}"); continue; }
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                    Debug.Log($"Set {path} to Sprite type");
                }
            }
        }

        var bg = Object.FindFirstObjectByType<ParallaxBackground>();
        if (bg == null) { Debug.LogError("ParallaxBackground not found!"); return; }

        var so = new SerializedObject(bg);
        var themesProp = so.FindProperty("themes");

        // Theme definitions: name, type2 sprites (sky, clouds, mountains), original sprites (middle_ground, foreground)
        var themeDefs = new[] {
            new {
                name = "Jungle2",
                sky = "Assets/Art/Backgrounds/Jungle/type2/bg.png",
                clouds = "Assets/Art/Backgrounds/Jungle/type2/clouds.png",
                mountains = "Assets/Art/Backgrounds/Jungle/type2/mountains.png",
                middleGround = "Assets/Art/Backgrounds/Jungle/middle_ground.png",
                foreground = "Assets/Art/Backgrounds/Jungle/foreground.png"
            },
            new {
                name = "Desert2",
                sky = "Assets/Art/Backgrounds/Desert/type2/bg.png",
                clouds = "Assets/Art/Backgrounds/Desert/type2/clouds.png",
                mountains = "Assets/Art/Backgrounds/Desert/type2/dune.png",
                middleGround = "Assets/Art/Backgrounds/Desert/middle_ground.png",
                foreground = "Assets/Art/Backgrounds/Desert/foreground.png"
            },
            new {
                name = "IceAge2",
                sky = "Assets/Art/Backgrounds/IceAge/type2/bg.png",
                clouds = "Assets/Art/Backgrounds/IceAge/type2/clouds.png",
                mountains = "Assets/Art/Backgrounds/IceAge/type2/mountains.png",
                middleGround = "Assets/Art/Backgrounds/IceAge/middle_ground.png",
                foreground = "Assets/Art/Backgrounds/IceAge/foreground.png"
            }
        };

        foreach (var def in themeDefs)
        {
            // Remove existing theme with same name
            for (int i = 0; i < themesProp.arraySize; i++)
            {
                if (themesProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == def.name)
                {
                    themesProp.DeleteArrayElementAtIndex(i);
                    break;
                }
            }

            int idx = themesProp.arraySize;
            themesProp.InsertArrayElementAtIndex(idx);
            var theme = themesProp.GetArrayElementAtIndex(idx);
            theme.FindPropertyRelative("name").stringValue = def.name;
            theme.FindPropertyRelative("enabled").boolValue = true;

            var layersProp = theme.FindPropertyRelative("layers");
            layersProp.arraySize = 5;

            // Same structure as originals: sky, mountains, clouds, middle_ground, foreground
            string[] sprites = { def.sky, def.mountains, def.clouds, def.middleGround, def.foreground };
            float[] speeds =   { 0f, 0.003f, 0.005f, 0.008f, 0.012f };
            // verticalAnchor: 0=Stretch, 1=Bottom, 2=Top
            int[] anchors =    { 0, 1, 2, 1, 1 };

            for (int i = 0; i < 5; i++)
            {
                var layer = layersProp.GetArrayElementAtIndex(i);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sprites[i]);
                if (sprite == null) { Debug.LogWarning($"Could not load: {sprites[i]}"); continue; }

                layer.FindPropertyRelative("sprite").objectReferenceValue = sprite;
                layer.FindPropertyRelative("scrollSpeed").floatValue = speeds[i];
                layer.FindPropertyRelative("alpha").floatValue = 1f;
                layer.FindPropertyRelative("verticalAnchor").enumValueIndex = anchors[i];
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(bg);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("Type2 themes (Jungle2, Desert2, IceAge2) added successfully!");
    }
}

using UnityEngine;
using UnityEditor;
using System.Reflection;

/// <summary>
/// One-shot editor utility: properly assigns Sprite references to the Winter theme
/// in ParallaxBackground. Run via menu: Tools > WordyWords > Setup Winter Theme.
/// Safe to delete after use.
/// </summary>
public static class SetupWinterTheme
{
    [MenuItem("Tools/WordyWords/Setup Winter Theme")]
    public static void Run()
    {
        var bg = Object.FindFirstObjectByType<ParallaxBackground>();
        if (bg == null)
        {
            Debug.LogError("SetupWinterTheme: No ParallaxBackground found in scene.");
            return;
        }

        // Access the private 'themes' field via reflection
        var themesField = typeof(ParallaxBackground).GetField("themes",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (themesField == null)
        {
            Debug.LogError("SetupWinterTheme: Could not find 'themes' field.");
            return;
        }

        var themes = themesField.GetValue(bg) as ParallaxBackground.ParallaxTheme[];
        if (themes == null)
        {
            Debug.LogError("SetupWinterTheme: themes array is null.");
            return;
        }

        // Find the Winter theme
        int winterIdx = -1;
        for (int i = 0; i < themes.Length; i++)
        {
            if (themes[i].name == "Winter")
            {
                winterIdx = i;
                break;
            }
        }

        if (winterIdx < 0)
        {
            Debug.LogError("SetupWinterTheme: No theme named 'Winter' found.");
            return;
        }

        string basePath = "Assets/Art/Backgrounds/Winter/";

        // Define layers: file, scrollSpeed, alpha, verticalAnchor, verticalOffset
        // Back to front: BG3 (sky) → clouds → BG2 → BG1 → MG3 → MG2 → MG1 → FG
        // Bottom anchor with verticalOffset shifts layers up to show more mountain peaks / tree tops
        // without distortion (Stretch) or full-height tiling (FillHeight)
        var layerDefs = new (string file, float speed, float alpha, ParallaxBackground.VerticalAnchor anchor, float vOffset)[]
        {
            ("layer7_BG3_4k.png",  0.000f, 1.0f, ParallaxBackground.VerticalAnchor.Stretch,     0.00f),
            ("BG_clouds_4k.png",   0.002f, 0.8f, ParallaxBackground.VerticalAnchor.Top,          0.00f),
            ("layer6_BG2_4k.png",  0.004f, 1.0f, ParallaxBackground.VerticalAnchor.Bottom,       0.15f),  // distant mountains up
            ("layer5_BG1_4k.png",  0.006f, 1.0f, ParallaxBackground.VerticalAnchor.Bottom,       0.12f),  // closer mountains up
            ("layer4_MG3_4k.png",  0.010f, 1.0f, ParallaxBackground.VerticalAnchor.Bottom,       0.08f),  // distant treeline up
            ("layer3_MG2_4k.png",  0.014f, 1.0f, ParallaxBackground.VerticalAnchor.Bottom,       0.05f),  // midground trees slight up
            ("layer2_MG1_4k.png",  0.019f, 1.0f, ParallaxBackground.VerticalAnchor.FillHeight,   0.00f),
            ("layer1_FG_4k.png",   0.024f, 1.0f, ParallaxBackground.VerticalAnchor.FillHeight,   0.00f),
        };

        var layers = new ParallaxBackground.ParallaxLayer[layerDefs.Length];
        for (int i = 0; i < layerDefs.Length; i++)
        {
            string assetPath = basePath + layerDefs[i].file;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Debug.LogError($"SetupWinterTheme: Could not load sprite at '{assetPath}'");
                return;
            }

            layers[i] = new ParallaxBackground.ParallaxLayer
            {
                sprite = sprite,
                alternateSprite = null,
                scrollSpeed = layerDefs[i].speed,
                alpha = layerDefs[i].alpha,
                verticalAnchor = layerDefs[i].anchor,
                verticalOffset = layerDefs[i].vOffset,
                scrollMode = ParallaxBackground.ScrollMode.Linear,
                ovalRadiusX = 50f,
                ovalRadiusY = 30f,
                ovalTileGrid = true,
            };
        }

        themes[winterIdx].layers = layers;
        themes[winterIdx].enabled = true;
        themes[winterIdx].revealedCellColor = new Color(0.25f, 0.3f, 0.4f, 1f);

        // Assign snow particle prefab
        var snowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Art/Backgrounds/Winter/VFX/FX SnowFlakes.prefab");
        if (snowPrefab != null)
            themes[winterIdx].particlePrefab = snowPrefab;
        else
            Debug.LogWarning("SetupWinterTheme: Could not find FX SnowFlakes prefab.");

        themesField.SetValue(bg, themes);

        EditorUtility.SetDirty(bg);
        Debug.Log($"SetupWinterTheme: Winter theme (index {winterIdx}) configured with {layers.Length} layers. Save scene to persist.");
    }
}

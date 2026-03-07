using UnityEngine;
using UnityEditor;
using TMPro;
using TMPro.EditorUtilities;
using UnityEngine.TextCore.LowLevel;

public static class CreateTMPFontAssets
{
    [MenuItem("Tools/WordyWords/Create TMP Font Assets")]
    public static void CreateAll()
    {
        string[] fontPaths = new[]
        {
            "Assets/Fonts/Fredoka-SemiBold.ttf",
            "Assets/Fonts/Poppins-Bold.ttf",
            "Assets/Fonts/Poppins-SemiBold.ttf",
            "Assets/Fonts/Poppins-Medium.ttf",
            "Assets/Fonts/Poppins-Regular.ttf"
        };

        foreach (string path in fontPaths)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (font == null)
            {
                Debug.LogWarning($"Font not found at {path}");
                continue;
            }

            string outputPath = path.Replace(".ttf", " SDF.asset");
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath) != null)
            {
                Debug.Log($"SDF asset already exists: {outputPath}");
                continue;
            }

            // Create SDF font asset with good defaults for mobile
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                90,          // sampling point size
                9,           // padding
                GlyphRenderMode.SDFAA,
                1024,        // atlas width
                1024         // atlas height
            );

            if (fontAsset == null)
            {
                Debug.LogError($"Failed to create SDF asset for {path}");
                continue;
            }

            AssetDatabase.CreateAsset(fontAsset, outputPath);

            // Save the atlas texture as a sub-asset
            if (fontAsset.atlasTexture != null)
            {
                fontAsset.atlasTexture.name = font.name + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }

            // Save material as sub-asset
            if (fontAsset.material != null)
            {
                fontAsset.material.name = font.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            Debug.Log($"Created SDF font asset: {outputPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("TMP Font Asset creation complete.");
    }
}

using UnityEngine;
using UnityEditor;
using TMPro;

public static class FontDilate
{
    [MenuItem("Tools/WordyWords/Fredoka Dilate +0.3")]
    public static void Dilate03()
    {
        SetFredokaDilate(0.3f);
    }

    [MenuItem("Tools/WordyWords/Fredoka Dilate +0.2")]
    public static void Dilate02()
    {
        SetFredokaDilate(0.2f);
    }

    [MenuItem("Tools/WordyWords/Fredoka Dilate +0.4")]
    public static void Dilate04()
    {
        SetFredokaDilate(0.4f);
    }

    [MenuItem("Tools/WordyWords/Fredoka Dilate 0 (reset)")]
    public static void DilateReset()
    {
        SetFredokaDilate(0f);
    }

    private static void SetFredokaDilate(float value)
    {
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fredoka-SemiBold SDF.asset");
        if (fontAsset == null || fontAsset.material == null)
        {
            Debug.LogError("Fredoka SDF font asset not found");
            return;
        }

        fontAsset.material.SetFloat("_FaceDilate", value);
        EditorUtility.SetDirty(fontAsset.material);
        AssetDatabase.SaveAssets();
        Debug.Log($"Fredoka _FaceDilate set to {value}");
    }
}

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class ApplyNewFonts
{
    [MenuItem("Tools/WordyWords/Apply New Fonts")]
    public static void Apply()
    {
        // Load font assets
        var fredoka = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fredoka-SemiBold SDF.asset");
        var poppinsBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Poppins-Bold SDF.asset");
        var poppinsSemi = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Poppins-SemiBold SDF.asset");
        var poppinsMed = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Poppins-Medium SDF.asset");

        if (fredoka == null || poppinsBold == null || poppinsSemi == null || poppinsMed == null)
        {
            Debug.LogError("Could not load one or more SDF font assets from Assets/Fonts/");
            return;
        }

        int count = 0;

        // --- Prefabs: GridCell and LetterTile get Fredoka ---
        string[] prefabPaths = {
            "Assets/Prefabs/GridCell.prefab",
            "Assets/Prefabs/LetterTilePrefab.prefab"
        };
        foreach (string path in prefabPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var tmp = prefab.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null && tmp.font != fredoka)
            {
                tmp.font = fredoka;
                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log($"Set {path} → Fredoka SemiBold");
                count++;
            }
        }

        // --- Scene TMP_Text components ---
        var allTMP = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        foreach (var tmp in allTMP)
        {
            string name = tmp.gameObject.name.ToLower();
            string parentName = tmp.transform.parent != null ? tmp.transform.parent.name.ToLower() : "";

            TMP_FontAsset targetFont;

            // Letter tiles and grid cells → Fredoka
            if (tmp.GetComponentInParent<LetterTile>() != null ||
                parentName.Contains("cell") ||
                name.Contains("cell"))
            {
                targetFont = fredoka;
            }
            // Level text, button labels → Poppins Bold
            else if (name.Contains("level") ||
                     name.Contains("title") ||
                     name.Contains("complete") ||
                     parentName.Contains("hint") ||
                     parentName.Contains("wordbank") ||
                     parentName.Contains("word bank") ||
                     parentName.Contains("next"))
            {
                targetFont = poppinsBold;
            }
            // Coin text, extra words count → Poppins SemiBold
            else if (name.Contains("coin") ||
                     name.Contains("score") ||
                     name.Contains("extra") ||
                     name.Contains("current"))
            {
                targetFont = poppinsSemi;
            }
            // Everything else (definition text, word bank body) → Poppins Medium
            else
            {
                targetFont = poppinsMed;
            }

            if (tmp.font != targetFont)
            {
                tmp.font = targetFont;
                EditorUtility.SetDirty(tmp);
                count++;
                Debug.Log($"Set {tmp.gameObject.name} → {targetFont.name}");
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Font swap complete. Changed {count} TMP components.");
    }
}

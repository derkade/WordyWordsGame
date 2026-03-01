using UnityEngine;
using UnityEditor;
using TMPro;

public static class SetupTextOutlines
{
    [MenuItem("Tools/Add Outline To HUD Text")]
    public static void AddOutlineToHUDText()
    {
        string[] paths = {
            "GameCanvas/HUD/LevelText",
            "GameCanvas/HUD/CoinText",
            "GameCanvas/WordDisplay/ExtraWordsCountText",
            "GameCanvas/WordDisplay/CurrentWordText"
        };

        float outlineWidth = 0.25f;
        Color32 outlineColor = new Color32(0, 0, 0, 255);

        foreach (string path in paths)
        {
            var go = GameObject.Find(path);
            if (go == null)
            {
                Debug.LogWarning($"Could not find: {path}");
                continue;
            }

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                Debug.LogWarning($"No TMP component on: {path}");
                continue;
            }

            // Enable outline via material properties
            tmp.fontMaterial.EnableKeyword("OUTLINE_ON");
            tmp.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
            tmp.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
            tmp.UpdateMeshPadding();
            tmp.SetMaterialDirty();

            EditorUtility.SetDirty(tmp);
            Debug.Log($"Outline applied to {path}");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("HUD text outlines configured.");
    }
}

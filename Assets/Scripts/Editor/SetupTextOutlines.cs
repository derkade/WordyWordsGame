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
            "GameCanvas/WordDisplay/CurrentWordText",
            "GameCanvas/LevelCompletePanel/CompleteTitleText"
        };

        float outlineWidth = 0.25f;
        Color32 outlineColor = new Color32(38, 38, 38, 180);

        foreach (string path in paths)
        {
            var go = GameObject.Find(path);
            // Try finding inactive objects via parent traversal
            if (go == null)
            {
                int lastSlash = path.LastIndexOf('/');
                if (lastSlash > 0)
                {
                    string parentPath = path.Substring(0, lastSlash);
                    string childName = path.Substring(lastSlash + 1);
                    var parent = GameObject.Find(parentPath);
                    if (parent != null)
                        go = parent.transform.Find(childName)?.gameObject;
                    // Try grandparent if parent is also inactive
                    if (go == null)
                    {
                        int secondSlash = parentPath.LastIndexOf('/');
                        if (secondSlash > 0)
                        {
                            var grandParent = GameObject.Find(parentPath.Substring(0, secondSlash));
                            if (grandParent != null)
                            {
                                var p = grandParent.transform.Find(parentPath.Substring(secondSlash + 1));
                                if (p != null)
                                    go = p.Find(childName)?.gameObject;
                            }
                        }
                    }
                }
            }
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

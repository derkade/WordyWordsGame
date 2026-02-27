using UnityEngine;
using UnityEditor;

public static class ReorderHierarchy
{
    [MenuItem("Tools/Move ButtonBar After WheelArea")]
    public static void MoveButtonBar()
    {
        var buttonBar = GameObject.Find("GameCanvas/ButtonBar");
        var wheelArea = GameObject.Find("GameCanvas/WheelArea");
        if (buttonBar != null && wheelArea != null)
        {
            int wheelIndex = wheelArea.transform.GetSiblingIndex();
            buttonBar.transform.SetSiblingIndex(wheelIndex + 1);
            Debug.Log($"ButtonBar moved to sibling index {wheelIndex + 1}");
            MarkDirty(buttonBar);
        }
        else
        {
            Debug.LogError("Could not find ButtonBar or WheelArea!");
        }
    }

    [MenuItem("Tools/Move LineContainer Behind Tiles")]
    public static void MoveLineContainerFirst()
    {
        var lineContainer = GameObject.Find("GameCanvas/WheelArea/LineContainer");
        if (lineContainer != null)
        {
            lineContainer.transform.SetSiblingIndex(0);
            Debug.Log("LineContainer moved to sibling index 0 (renders behind tiles)");
            MarkDirty(lineContainer);
        }
        else
        {
            Debug.LogError("Could not find LineContainer!");
        }
    }

    private static void MarkDirty(GameObject go)
    {
        EditorUtility.SetDirty(go);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}

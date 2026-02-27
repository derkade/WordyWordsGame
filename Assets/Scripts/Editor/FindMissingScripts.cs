using UnityEngine;
using UnityEditor;

public static class FindMissingScripts
{
    [MenuItem("Tools/Find Missing Scripts")]
    public static void Find()
    {
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        int count = 0;
        foreach (var go in allObjects)
        {
            if (go.hideFlags != HideFlags.None) continue;
            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.LogWarning($"Missing script on: '{go.name}' (path: {GetPath(go)})", go);
                    count++;
                }
            }
        }
        Debug.Log($"Found {count} missing script(s).");
    }

    static string GetPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null) { path = t.name + "/" + path; t = t.parent; }
        return path;
    }
}

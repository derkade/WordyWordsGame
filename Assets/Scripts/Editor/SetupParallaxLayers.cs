using UnityEngine;
using UnityEditor;

public static class SetupParallaxLayers
{
    private static readonly string[] layerFiles = { "sky.png", "mountains.png", "clouds.png", "middle_ground.png", "foreground.png" };
    private static readonly float[] speeds = { 0f, 0.003f, 0.005f, 0.008f, 0.012f };
    private static readonly float[] defaultAlphas = { 1f, 1f, 1f, 1f, 1f };
    private static readonly ParallaxBackground.VerticalAnchor[] anchors = {
        ParallaxBackground.VerticalAnchor.Stretch,
        ParallaxBackground.VerticalAnchor.Bottom,
        ParallaxBackground.VerticalAnchor.Top,
        ParallaxBackground.VerticalAnchor.Bottom,
        ParallaxBackground.VerticalAnchor.Bottom
    };

    private static readonly string[] themeNames = { "Jungle", "Desert", "IceAge" };
    private static readonly string[] themePaths = {
        "Assets/2D Adventure Background Pack/Backgrounds/Jungle/",
        "Assets/2D Adventure Background Pack/Backgrounds/Desert/",
        "Assets/2D Adventure Background Pack/Backgrounds/IceAge/"
    };

    [MenuItem("Tools/Setup All Parallax Themes")]
    public static void SetupAllThemes()
    {
        var parallaxGO = GameObject.Find("GameCanvas/ParallaxBG");
        if (parallaxGO == null)
        {
            Debug.LogError("Could not find ParallaxBG!");
            return;
        }

        var parallax = parallaxGO.GetComponent<ParallaxBackground>();
        if (parallax == null)
        {
            Debug.LogError("ParallaxBackground component not found on ParallaxBG!");
            return;
        }

        var so = new SerializedObject(parallax);
        var themesProp = so.FindProperty("themes");
        themesProp.arraySize = themeNames.Length;

        for (int t = 0; t < themeNames.Length; t++)
        {
            var themeProp = themesProp.GetArrayElementAtIndex(t);
            themeProp.FindPropertyRelative("name").stringValue = themeNames[t];

            var layersProp = themeProp.FindPropertyRelative("layers");
            layersProp.arraySize = layerFiles.Length;

            for (int i = 0; i < layerFiles.Length; i++)
            {
                string spritePath = themePaths[t] + layerFiles[i];
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                {
                    Debug.LogWarning($"Could not load sprite: {spritePath}");
                    continue;
                }

                var element = layersProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("sprite").objectReferenceValue = sprite;
                element.FindPropertyRelative("scrollSpeed").floatValue = speeds[i];
                element.FindPropertyRelative("alpha").floatValue = defaultAlphas[i];
                element.FindPropertyRelative("verticalAnchor").enumValueIndex = (int)anchors[i];
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(parallax);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"All {themeNames.Length} parallax themes configured successfully.");
    }

    [MenuItem("Tools/Setup Parallax Jungle Layers")]
    public static void SetupJungle() { ApplySingleTheme("Jungle", themePaths[0]); }

    [MenuItem("Tools/Setup Parallax Desert Layers")]
    public static void SetupDesert() { ApplySingleTheme("Desert", themePaths[1]); }

    [MenuItem("Tools/Setup Parallax IceAge Layers")]
    public static void SetupIceAge() { ApplySingleTheme("IceAge", themePaths[2]); }

    private static void ApplySingleTheme(string themeName, string basePath)
    {
        var parallaxGO = GameObject.Find("GameCanvas/ParallaxBG");
        if (parallaxGO == null) { Debug.LogError("Could not find ParallaxBG!"); return; }
        var parallax = parallaxGO.GetComponent<ParallaxBackground>();
        if (parallax == null) { Debug.LogError("ParallaxBackground component not found!"); return; }

        var so = new SerializedObject(parallax);
        var layersProp = so.FindProperty("layers");
        layersProp.arraySize = layerFiles.Length;

        for (int i = 0; i < layerFiles.Length; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(basePath + layerFiles[i]);
            if (sprite == null) { Debug.LogWarning($"Could not load sprite: {basePath + layerFiles[i]}"); continue; }

            var element = layersProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            element.FindPropertyRelative("scrollSpeed").floatValue = speeds[i];
            element.FindPropertyRelative("alpha").floatValue = defaultAlphas[i];
            element.FindPropertyRelative("verticalAnchor").enumValueIndex = (int)anchors[i];
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(parallax);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"Parallax {themeName} layers configured: {layerFiles.Length} layers set up successfully.");
    }
}

using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Forces Unity to detect script changes and recompile even when the editor
/// is not focused. Without this, Unity only recompiles when you click back
/// on the editor window.
/// </summary>
[InitializeOnLoad]
public static class CompileInBackground
{
    private static FileSystemWatcher watcher;

    static CompileInBackground()
    {
        string scriptsPath = Path.Combine(Application.dataPath, "Scripts");
        if (!Directory.Exists(scriptsPath)) return;

        watcher = new FileSystemWatcher(scriptsPath, "*.cs")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };

        watcher.Changed += OnScriptChanged;
        watcher.Created += OnScriptChanged;
        watcher.Deleted += OnScriptChanged;
        watcher.Renamed += OnScriptRenamed;
        watcher.EnableRaisingEvents = true;

        EditorApplication.quitting += () =>
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;
            }
        };
    }

    private static void OnScriptChanged(object sender, FileSystemEventArgs e)
    {
        // AssetDatabase.Refresh must run on Unity's main thread
        EditorApplication.delayCall += () => AssetDatabase.Refresh();
    }

    private static void OnScriptRenamed(object sender, RenamedEventArgs e)
    {
        EditorApplication.delayCall += () => AssetDatabase.Refresh();
    }
}

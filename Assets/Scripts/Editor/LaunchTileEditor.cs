using UnityEngine;
using UnityEditor;
using System.Diagnostics;

public static class LaunchTileEditor
{
    [MenuItem("Tools/Tile Editor (Underwater Diving)")]
    public static void Launch()
    {
        string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
        string script = System.IO.Path.Combine(projectRoot, "tools", "tile_editor.py");

        if (!System.IO.File.Exists(script))
        {
            UnityEngine.Debug.LogError($"Tile editor not found: {script}");
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{script}\"",
            WorkingDirectory = projectRoot,
            UseShellExecute = true
        };

        Process.Start(psi);
        UnityEngine.Debug.Log("Tile Editor launched.");
    }
}

using UnityEditor;
using UnityEngine;

/// <summary>
/// Build scripts for batchmode execution.
/// WebGL: Unity.exe -batchmode -executeMethod BuildScript.BuildWebGL -quit
/// Windows: Unity.exe -batchmode -executeMethod BuildScript.BuildWindows -quit
/// </summary>
public static class BuildScript
{
    private static readonly string[] Scenes = { "Assets/Scenes/Main.unity" };

    public static void BuildWebGL()
    {
        // Disable compression for faster builds
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.dataCaching = false;

        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = "Build/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.Development
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError($"[BuildScript] WebGL build failed: {report.summary.totalErrors} errors");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("[BuildScript] WebGL build succeeded");
        }
    }

    public static void BuildWindows()
    {
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = "Build/Windows/TowerOfSaviors.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError($"[BuildScript] Windows build failed: {report.summary.totalErrors} errors");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("[BuildScript] Windows build succeeded");
        }
    }
}

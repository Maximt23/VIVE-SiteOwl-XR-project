using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

/// <summary>
/// Build script for command-line and automated builds via BUILD.bat.
/// Called with: -executeMethod BuildScript.BuildAndroid
/// </summary>
public class BuildScript
{
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/MainScene.unity"
    };

    private static string OutputDir => Path.GetFullPath(
        Path.Combine(Application.dataPath, "..", "..", "Builds"));

    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        string apkPath = Path.Combine(OutputDir, "SiteOwl_XR_Capture.apk");
        Directory.CreateDirectory(OutputDir);

        var opts = new BuildPlayerOptions
        {
            scenes           = Scenes,
            locationPathName = apkPath,
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
        };

        Debug.Log($"[BuildScript] Building Android APK -> {apkPath}");
        var report  = BuildPipeline.BuildPlayer(opts);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] SUCCESS  {summary.outputPath}  " +
                      $"({summary.totalSize / 1_048_576f:F1} MB  {summary.totalTime.TotalSeconds:F0}s)");
        }
        else
        {
            Debug.LogError($"[BuildScript] FAILED ({summary.totalErrors} error(s))");
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Build/Build and Run Android")]
    public static void BuildAndRunAndroid()
    {
        string apkPath = Path.Combine(OutputDir, "SiteOwl_XR_Capture.apk");
        Directory.CreateDirectory(OutputDir);

        BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes           = Scenes,
            locationPathName = apkPath,
            target           = BuildTarget.Android,
            options          = BuildOptions.AutoRunPlayer,
        });
    }
}

using UnityEditor;
using UnityEngine;

/// <summary>
/// Build script for command-line and automated builds.
/// </summary>
public class BuildScript
{
    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        // Setup build options
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/XR_Test.unity" },
            locationPathName = "../Builds/SiteOwl_XR_Capture.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        
        // Build
        Debug.Log("Starting Android build...");
        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;
        
        // Check result
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded! Time: {summary.totalTime}, Size: {summary.totalSize} bytes");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("Build failed!");
            EditorApplication.Exit(1);
        }
    }
    
    [MenuItem("Build/Build and Run Android")]
    public static void BuildAndRunAndroid()
    {
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/XR_Test.unity" },
            locationPathName = "../Builds/SiteOwl_XR_Capture.apk",
            target = BuildTarget.Android,
            options = BuildOptions.AutoRunPlayer
        };
        
        Debug.Log("Building and running on device...");
        BuildPipeline.BuildPlayer(buildOptions);
    }
}

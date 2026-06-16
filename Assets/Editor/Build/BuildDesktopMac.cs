using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildDesktopMac
{
    private const string MenuPath = "Tools/Build/Build Desktop Mac";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string DefaultPackageName = "com.yarik.dvizki3.desktop";
    private const string BuildDirectory = "Builds/Desktop";
    private const string AppFileName = "dvizki3_desktop.app";

    [MenuItem(MenuPath)]
    public static void BuildMac()
    {
        if (EditorApplication.isCompiling)
        {
            Debug.LogError("Desktop build aborted: Unity is still compiling scripts.");
            return;
        }

        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
        {
            Debug.LogError("Desktop build aborted: macOS Standalone Build Support is not installed in this Unity Editor.");
            return;
        }

        try
        {
            EnsureSampleSceneInBuildSettings();
            ConfigureStandalonePlayerSettings();

            if (!EnsureStandaloneBuildTarget())
                return;

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outputDirectory = Path.Combine(projectRoot, BuildDirectory);
            Directory.CreateDirectory(outputDirectory);
            string appPath = Path.Combine(outputDirectory, AppFileName);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { SampleScenePath },
                locationPathName = appPath,
                target = BuildTarget.StandaloneOSX,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError(
                    $"Desktop build failed. Result: {report.summary.result}. Errors: {report.summary.totalErrors}. Warnings: {report.summary.totalWarnings}.");
                return;
            }

            Debug.Log($"Desktop build completed successfully.\nApp: {appPath}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Desktop build failed: {exception}");
        }
    }

    private static void EnsureSampleSceneInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        int sceneIndex = scenes.FindIndex(scene => scene.path == SampleScenePath);
        if (sceneIndex >= 0)
        {
            EditorBuildSettingsScene existingScene = scenes[sceneIndex];
            existingScene.enabled = true;
            scenes.RemoveAt(sceneIndex);
            scenes.Insert(0, existingScene);
        }
        else
        {
            scenes.Insert(0, new EditorBuildSettingsScene(SampleScenePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void ConfigureStandalonePlayerSettings()
    {
        PlayerSettings.applicationIdentifier = DefaultPackageName;
        PlayerSettings.productName = "dvizki 3";
        PlayerSettings.bundleVersion = string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion) ? "0.1.0" : PlayerSettings.bundleVersion;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Unity_4_8);
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SplashScreen.show = false;
    }

    private static bool EnsureStandaloneBuildTarget()
    {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX)
            return true;

        if (EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
            return true;

        Debug.LogError("Desktop build aborted: could not switch active build target to macOS Standalone.");
        return false;
    }
}

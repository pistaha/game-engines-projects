using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

public static class BuildQuestApk
{
    private const string MenuPath = "Tools/Build/Build Quest APK";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string DefaultPackageName = "com.yarik.dvizki2";
    private const string BuildDirectory = "Builds/Quest";
    private const string ApkFileName = "dvizki2_quest_build.apk";
    private const string OpenXRLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";

    [MenuItem(MenuPath)]
    public static void BuildQuest()
    {
        if (EditorApplication.isCompiling)
        {
            Debug.LogError("Quest build aborted: Unity is still compiling scripts.");
            return;
        }

        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("Quest build aborted: Android Build Support module is not installed in this Unity Editor.");
            return;
        }

        try
        {
            EnsureSampleSceneInBuildSettings();
            VRSceneSetupUtility.SetupSampleSceneVrObjects();
            ConfigureAndroidPlayerSettings();
            if (!EnsureAndroidXrSetup())
                return;

            if (!EnsureAndroidBuildTarget())
                return;

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outputDirectory = Path.Combine(projectRoot, BuildDirectory);
            Directory.CreateDirectory(outputDirectory);
            string apkPath = Path.Combine(outputDirectory, ApkFileName);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { SampleScenePath },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError(
                    $"Quest APK build failed. Result: {report.summary.result}. Errors: {report.summary.totalErrors}. Warnings: {report.summary.totalWarnings}.");
                return;
            }

            Debug.Log($"Quest APK build completed successfully.\nAPK: {apkPath}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Quest build failed: {exception}");
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

    private static void ConfigureAndroidPlayerSettings()
    {
        PlayerSettings.applicationIdentifier = DefaultPackageName;
        PlayerSettings.bundleVersion = string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion) ? "0.1.0" : PlayerSettings.bundleVersion;
        PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.forceInternetPermission = false;
        PlayerSettings.Android.forceSDCardPermission = false;

        SetAndroidTextureCompressionToAstc();
    }

    private static void SetAndroidTextureCompressionToAstc()
    {
        PropertyInfo androidBuildSubtargetProperty = typeof(EditorUserBuildSettings).GetProperty(
            "androidBuildSubtarget",
            BindingFlags.Public | BindingFlags.Static);

        if (androidBuildSubtargetProperty == null)
            return;

        try
        {
            object astcValue = Enum.Parse(androidBuildSubtargetProperty.PropertyType, "ASTC");
            androidBuildSubtargetProperty.SetValue(null, astcValue);
        }
        catch (ArgumentException)
        {
            Debug.LogWarning("Quest build setup could not set Android texture compression to ASTC automatically.");
        }
    }

    private static bool EnsureAndroidXrSetup()
    {
        XRGeneralSettings generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        if (generalSettings == null)
        {
            Debug.LogError("Quest build aborted: Android XR General Settings are missing.");
            return false;
        }

        XRManagerSettings managerSettings = generalSettings.AssignedSettings;
        if (managerSettings == null)
        {
            Debug.LogError("Quest build aborted: Android XR Manager Settings are missing.");
            return false;
        }

        if (!XRPackageMetadataStore.AssignLoader(managerSettings, OpenXRLoaderTypeName, BuildTargetGroup.Android))
        {
            bool hasOpenXrLoader = managerSettings.activeLoaders.Any(loader => loader != null && loader.GetType().FullName == OpenXRLoaderTypeName);
            if (!hasOpenXrLoader)
            {
                Debug.LogError("Quest build aborted: OpenXR loader is not assigned for Android XR Management.");
                return false;
            }
        }

        OpenXRSettings openXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (openXrSettings == null)
        {
            Debug.LogError("Quest build aborted: Android OpenXR settings are missing.");
            return false;
        }

        SetFeatureEnabled(openXrSettings.GetFeature<MetaQuestFeature>(), true);
        SetFeatureEnabled(openXrSettings.GetFeature<OculusTouchControllerProfile>(), true);

        DisableDeprecatedOculusQuestFeature(openXrSettings);

        ConfigureMetaQuestFeature(openXrSettings.GetFeature<MetaQuestFeature>());
        EditorUtility.SetDirty(openXrSettings);
        AssetDatabase.SaveAssets();
        return true;
    }

    private static void ConfigureMetaQuestFeature(MetaQuestFeature metaQuestFeature)
    {
        if (metaQuestFeature == null)
            return;

        SerializedObject serializedObject = new SerializedObject(metaQuestFeature);

        SerializedProperty forceRemoveInternetPermission = serializedObject.FindProperty("forceRemoveInternetPermission");
        if (forceRemoveInternetPermission != null)
            forceRemoveInternetPermission.boolValue = true;

        SerializedProperty targetDevices = serializedObject.FindProperty("targetDevices");
        if (targetDevices != null && targetDevices.arraySize == 0)
        {
            AddTargetDevice(targetDevices, "Quest", "quest");
            AddTargetDevice(targetDevices, "Quest 2", "quest2");
            AddTargetDevice(targetDevices, "Quest Pro", "cambria");
            AddTargetDevice(targetDevices, "Quest 3", "eureka");
            AddTargetDevice(targetDevices, "Quest 3S", "quest3s");
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(metaQuestFeature);
    }

    private static void DisableDeprecatedOculusQuestFeature(OpenXRSettings openXrSettings)
    {
        if (openXrSettings == null)
            return;

        Type deprecatedFeatureType = Type.GetType(
            "UnityEngine.XR.OpenXR.Features.OculusQuestSupport.OculusQuestFeature, Unity.XR.OpenXR.Features.OculusQuestSupport");
        if (deprecatedFeatureType == null)
            return;

        List<OpenXRFeature> features = new List<OpenXRFeature>();
        openXrSettings.GetFeatures(deprecatedFeatureType, features);
        foreach (OpenXRFeature feature in features)
        {
            if (feature == null || !feature.enabled)
                continue;

            feature.enabled = false;
            EditorUtility.SetDirty(feature);
        }
    }

    private static void AddTargetDevice(SerializedProperty targetDevices, string visibleName, string manifestName)
    {
        int index = targetDevices.arraySize;
        targetDevices.InsertArrayElementAtIndex(index);
        SerializedProperty element = targetDevices.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("visibleName").stringValue = visibleName;
        element.FindPropertyRelative("manifestName").stringValue = manifestName;
        element.FindPropertyRelative("enabled").boolValue = true;
    }

    private static void SetFeatureEnabled(OpenXRFeature feature, bool enabled)
    {
        if (feature == null)
            return;

        feature.enabled = enabled;
        EditorUtility.SetDirty(feature);
    }

    private static bool EnsureAndroidBuildTarget()
    {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            return true;

        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("Quest build aborted: could not switch active build target to Android.");
            return false;
        }

        return true;
    }
}

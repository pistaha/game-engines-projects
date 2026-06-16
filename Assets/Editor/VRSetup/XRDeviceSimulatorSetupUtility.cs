using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class XRDeviceSimulatorSetupUtility
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string SimulatorPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.0.10/XR Device Simulator/XR Device Simulator.prefab";
    private const string MenuPath = "Tools/VR/Add XR Device Simulator To SampleScene";

    [MenuItem(MenuPath)]
    public static void AddSimulatorToSampleScene()
    {
        Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        bool changed = false;

        GameObject simulator = GameObject.Find("XR Device Simulator");
        if (simulator == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"XR Device Simulator prefab was not found at '{SimulatorPrefabPath}'.");
                return;
            }

            simulator = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            if (simulator == null)
            {
                Debug.LogError("Failed to instantiate XR Device Simulator prefab.");
                return;
            }

            Undo.RegisterCreatedObjectUndo(simulator, "Add XR Device Simulator");
            changed = true;
            Debug.Log("Added XR Device Simulator to SampleScene.");
        }

        Transform mainCamera = FindMainCameraTransform(scene);
        if (mainCamera != null)
        {
            var serializedObject = new SerializedObject(simulator.GetComponent<MonoBehaviour>());
            var cameraProperty = serializedObject.FindProperty("m_CameraTransform");
            if (cameraProperty != null && cameraProperty.objectReferenceValue != mainCamera)
            {
                cameraProperty.objectReferenceValue = mainCamera;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
                Debug.Log("Assigned Main Camera transform to XR Device Simulator.");
            }
        }
        else
        {
            Debug.LogWarning("XR Device Simulator setup could not find Main Camera in SampleScene.");
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("XR Device Simulator setup updated SampleScene. Save the scene to keep the simulator.");
        }
        else
        {
            Debug.Log("XR Device Simulator is already present in SampleScene.");
        }
    }

    private static Transform FindMainCameraTransform(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Camera camera = roots[i].GetComponentInChildren<Camera>(true);
            if (camera != null && camera.name == "Main Camera")
                return camera.transform;
        }

        return Camera.main != null ? Camera.main.transform : null;
    }
}

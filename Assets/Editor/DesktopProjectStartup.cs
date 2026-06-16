using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DesktopProjectStartup
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    static DesktopProjectStartup()
    {
        EditorApplication.delayCall += OpenSampleSceneIfEditorStartedEmpty;
    }

    [MenuItem("Tools/Desktop/Open Main Scene")]
    public static void OpenMainScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
    }

    private static void OpenSampleSceneIfEditorStartedEmpty()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        bool editorStartedEmpty =
            string.IsNullOrEmpty(activeScene.path)
            && activeScene.name == "Untitled"
            && activeScene.rootCount <= 2;

        if (!editorStartedEmpty)
            return;

        EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
    }
}

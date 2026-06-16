using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ErikaAnimatorSetup
{
    private const string ControllerPath = "Assets/Erika_Controller.controller";
    private const string ModelPath = "Assets/Erika Archer.fbx";
    private const string SalutePath = "Assets/Erika Archer@Salute.fbx";
    private const string TakingPunchPath = "Assets/Erika Archer@Taking Punch.fbx";
    private const string ScenePath = "Assets/LowPolyDungeonsLite_Demo.unity";

    [MenuItem("Tools/Fix Erika Animator")]
    public static void Fix()
    {
        var saluteClip = LoadClip(SalutePath);
        var takingPunchClip = LoadClip(TakingPunchPath);

        if (saluteClip == null || takingPunchClip == null)
        {
            Debug.LogError("ErikaAnimatorSetup: не найдены Salute или Taking Punch clips.");
            return;
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (controller == null)
        {
            Debug.LogError("ErikaAnimatorSetup: Erika_Controller.controller не найден.");
            return;
        }

        ConfigureController(controller, saluteClip, takingPunchClip);
        ConfigureSceneAnimator(controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ErikaAnimatorSetup: Idle/Salute/Taking Punch настроены.");
    }

    private static AnimationClip LoadClip(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal));
    }

    private static void ConfigureController(AnimatorController controller, AnimationClip saluteClip, AnimationClip takingPunchClip)
    {
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = FindOrCreateState(stateMachine, "Idle", new Vector3(250f, 100f, 0f));
        AnimatorState salute = FindOrCreateState(stateMachine, "Salute", new Vector3(500f, 40f, 0f));
        AnimatorState takingPunch = FindOrCreateState(stateMachine, "Taking Punch", new Vector3(500f, 160f, 0f));

        idle.motion = null;
        salute.motion = saluteClip;
        takingPunch.motion = takingPunchClip;
        stateMachine.defaultState = idle;

        EnsureTrigger(controller, "Salute");
        EnsureTrigger(controller, "TakingPunch");

        ClearTransitions(idle);
        ClearTransitions(salute);
        ClearTransitions(takingPunch);

        AddTriggerTransition(idle, salute, "Salute");
        AddTriggerTransition(idle, takingPunch, "TakingPunch");
        AddExitTransition(salute, idle);
        AddExitTransition(takingPunch, idle);

        EditorUtility.SetDirty(controller);
    }

    private static AnimatorState FindOrCreateState(AnimatorStateMachine stateMachine, string name, Vector3 position)
    {
        ChildAnimatorState child = stateMachine.states.FirstOrDefault(state => state.state.name == name);
        return child.state != null ? child.state : stateMachine.AddState(name, position);
    }

    private static void EnsureTrigger(AnimatorController controller, string parameterName)
    {
        AnimatorControllerParameter existing = controller.parameters.FirstOrDefault(parameter => parameter.name == parameterName);
        if (existing != null && existing.type == AnimatorControllerParameterType.Trigger)
            return;

        if (existing != null)
            controller.RemoveParameter(existing);

        controller.AddParameter(parameterName, AnimatorControllerParameterType.Trigger);
    }

    private static void ClearTransitions(AnimatorState state)
    {
        foreach (AnimatorStateTransition transition in state.transitions.ToArray())
            state.RemoveTransition(transition);
    }

    private static void AddTriggerTransition(AnimatorState from, AnimatorState to, string trigger)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void AddExitTransition(AnimatorState from, AnimatorState to)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = 0.95f;
        transition.duration = 0.05f;
    }

    private static void ConfigureSceneAnimator(RuntimeAnimatorController controller)
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject erika = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .FirstOrDefault(go => go.name == "Erika Archer");

        if (erika == null)
            return;

        Animator animator = erika.GetComponent<Animator>();
        if (animator != null)
        {
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            if (avatar != null)
                animator.avatar = avatar;
        }

        EditorUtility.SetDirty(erika);
        EditorSceneManager.SaveOpenScenes();
    }
}

using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class AudioSetupTool
{
    public static void PrintAudioMixerControllerMethods()
    {
        Type controllerType = Type.GetType("UnityEditor.Audio.AudioMixerController,UnityEditor");

        if (controllerType == null)
        {
            Debug.LogError("AudioMixerController type not found");
            EditorApplication.Exit(1);
            return;
        }

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        MethodInfo[] methods = controllerType.GetMethods(flags)
            .Where(m => m.Name.Contains("Group") || m.Name.Contains("Mixer") || m.Name.Contains("Audio"))
            .OrderBy(m => m.Name)
            .ToArray();

        foreach (MethodInfo method in methods)
        {
            string parameters = string.Join(", ",
                method.GetParameters().Select(p => $"{p.ParameterType.FullName} {p.Name}"));
            Debug.Log($"{method.ReturnType.FullName} {method.Name}({parameters})");
        }

        EditorApplication.Exit(0);
    }
}

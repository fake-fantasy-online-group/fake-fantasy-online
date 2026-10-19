using System;
using UnityEditor;
using UnityEngine;

namespace FakeFantasyOnline.Editor
{
/// <summary>Explicit candidate-build entry point. This does not publish or deploy.</summary>
public static class CiClientBuild
{
    public const string RequiredUnityVersion = "6000.4.9f1";

    public static void BuildAndroidResourceFree()
    {
        if (UnityEngine.Application.unityVersion != RequiredUnityVersion)
            throw new InvalidOperationException("CI requires exact Unity " + RequiredUnityVersion + "; found " + UnityEngine.Application.unityVersion + ".");
        if (string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion))
            throw new InvalidOperationException("Set the application version in ProjectSettings/ProjectSettings.asset.");
        Debug.Log("Building a resource-free development candidate, application " + PlayerSettings.bundleVersion +
            ". Application version does not migrate the login protocol or prove server compatibility.");
        MobileClientBuild.BuildAndroidResourceFree();
    }
}
}

using System;
using UnityEngine;

namespace FakeFantasyOnline.Bootstrap
{
public enum AndroidClientPackageInstallState
{
    Unsupported, Idle, Preparing, Ready, PermissionRequired, Staging,
    AwaitingConfirmation, AwaitingResult, Succeeded, Cancelled, Failed, Interrupted
}

public sealed class AndroidClientPackageInstallSnapshot
{
    public readonly AndroidClientPackageInstallState State;
    public readonly string Message;
    public readonly float Progress;
    public AndroidClientPackageInstallSnapshot(AndroidClientPackageInstallState state, string message, float progress)
    { State = state; Message = message ?? ""; Progress = progress; }
}

/// <summary>
/// Main-thread bridge to the source-owned Android system installer. Preparation and session
/// writes run on Java's worker thread. Nothing installs, opens settings or launches a prompt
/// merely by polling; each handoff needs the corresponding explicit UI action.
/// </summary>
public static class AndroidClientPackageInstaller
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private const string JavaClass = "org.fakefantasyonline.updates.ClientPackageInstaller";
    private static AndroidClientPackageInstallSnapshot bridgeFailure;

    private static void Invoke(string method, params object[] arguments)
    {
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var bridge = new AndroidJavaClass(JavaClass))
            {
                var args = new object[arguments.Length + 1];
                args[0] = activity;
                Array.Copy(arguments, 0, args, 1, arguments.Length);
                bridge.CallStatic(method, args);
            }
            bridgeFailure = null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Android package installer bridge failed: " + exception);
            bridgeFailure = new AndroidClientPackageInstallSnapshot(AndroidClientPackageInstallState.Failed,
                "无法连接 Android 系统安装组件。请保留已下载的安装包并重试。", -1);
        }
    }
#endif
    public static void Prepare(string localApkPath, string expectedVersion, string expectedSha256, long expectedBytes)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Invoke("prepare", localApkPath, expectedVersion, expectedSha256, expectedBytes);
#endif
    }
    public static void Begin()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Invoke("begin");
#endif
    }
    public static void OpenPermissionSettings()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Invoke("openPermissionSettings");
#endif
    }
    public static void OpenSystemConfirmation()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Invoke("openSystemConfirmation");
#endif
    }
    public static void Cancel()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Invoke("cancel");
#endif
    }
    public static AndroidClientPackageInstallSnapshot Poll()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bridgeFailure != null) return bridgeFailure;
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var bridge = new AndroidJavaClass(JavaClass))
            {
                // One call provides a coherent snapshot across Java's worker/callback threads.
                string[] parts = bridge.CallStatic<string[]>("poll", activity);
                AndroidClientPackageInstallState state;
                float progress;
                if (parts == null || parts.Length != 3 || !Enum.TryParse(parts[0], out state) ||
                    !Enum.IsDefined(typeof(AndroidClientPackageInstallState), state) ||
                    !float.TryParse(parts[2], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out progress) ||
                    float.IsNaN(progress) || float.IsInfinity(progress))
                    throw new InvalidOperationException("Invalid Android installer state.");
                return new AndroidClientPackageInstallSnapshot(state, parts[1], Mathf.Clamp(progress, -1, 1));
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Android package installer bridge failed: " + exception);
            return new AndroidClientPackageInstallSnapshot(AndroidClientPackageInstallState.Failed,
                "无法读取 Android 安装结果。尚未确认安装成功，请重试或重新启动客户端。", -1);
        }
#else
        return new AndroidClientPackageInstallSnapshot(AndroidClientPackageInstallState.Unsupported,
            "当前平台不支持 Android APK 安装。", -1);
#endif
    }
}
}

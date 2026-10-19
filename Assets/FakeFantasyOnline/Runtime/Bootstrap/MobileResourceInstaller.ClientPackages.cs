using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
using UnityEngine;

namespace FakeFantasyOnline.Bootstrap
{
public sealed partial class MobileResourceInstaller
{
    private bool clientPackageRequested, clientPackageHandoffRequested, clientPackageMode;
    private string verifiedClientPackage, verifiedClientIdentity;
    private bool androidPackageActive;
    private double nextAndroidPoll;
    private AndroidClientPackageInstallState androidPackageState = AndroidClientPackageInstallState.Idle;
    private string clientPackageNotice = "";

    private string ArtifactIdentity()
    {
        var artifact = updatePlan?.Artifact;
        return artifact == null ? null : artifact.Version + "\n" + artifact.Url + "\n" + artifact.SizeBytes + "\n" + artifact.Sha256;
    }
    private bool HasVerifiedClientPackage => verifiedClientPackage != null && verifiedClientIdentity == ArtifactIdentity();
    private bool AndroidInstallBusy => androidPackageActive &&
        (androidPackageState == AndroidClientPackageInstallState.Preparing || androidPackageState == AndroidClientPackageInstallState.Staging ||
         androidPackageState == AndroidClientPackageInstallState.AwaitingConfirmation || androidPackageState == AndroidClientPackageInstallState.AwaitingResult);
    private bool ClientPackageActionEnabled => !androidPackageActive ||
        (androidPackageState != AndroidClientPackageInstallState.AwaitingResult && androidPackageState != AndroidClientPackageInstallState.Unsupported);

    private void ClearClientPackageSelection()
    {
        if (androidPackageActive) AndroidClientPackageInstaller.Cancel();
        androidPackageActive = clientPackageMode = clientPackageRequested = clientPackageHandoffRequested = false;
        verifiedClientPackage = verifiedClientIdentity = null;
        clientPackageNotice = "";
        androidPackageState = AndroidClientPackageInstallState.Idle;
    }

    private string ClientPackageButtonLabel()
    {
        if (androidPackageActive)
        {
            switch (androidPackageState)
            {
                case AndroidClientPackageInstallState.Preparing: return "取消安装包验证";
                case AndroidClientPackageInstallState.Staging: return "取消安装准备";
                case AndroidClientPackageInstallState.PermissionRequired: return "允许安装此来源";
                case AndroidClientPackageInstallState.Ready: return "继续系统安装";
                case AndroidClientPackageInstallState.AwaitingConfirmation: return "打开系统安装确认";
                case AndroidClientPackageInstallState.AwaitingResult: return "等待系统安装结果";
                case AndroidClientPackageInstallState.Succeeded: return "退出后手动重启";
                case AndroidClientPackageInstallState.Unsupported: return "当前平台无法安装";
                default: return "重新验证安装包";
            }
        }
        if (HasVerifiedClientPackage) return PlatformKey() == "android" ? "验证并准备安装" : "打开安装包目录";
        return failed && clientPackageMode ? "重试客户端下载" : "下载客户端";
    }

    private void RequestClientPackageAction()
    {
        if (updatePlan?.Artifact == null || !updatePlan.Available || remoteCancellation != null) return;
        clientPackageMode = true;
        if (androidPackageActive)
        {
            switch (androidPackageState)
            {
                case AndroidClientPackageInstallState.Preparing:
                case AndroidClientPackageInstallState.Staging:
                    AndroidClientPackageInstaller.Cancel(); PollAndroidPackage(true); return;
                case AndroidClientPackageInstallState.PermissionRequired:
                    AndroidClientPackageInstaller.OpenPermissionSettings(); PollAndroidPackage(true); return;
                case AndroidClientPackageInstallState.Ready:
                    AndroidClientPackageInstaller.Begin(); PollAndroidPackage(true); return;
                case AndroidClientPackageInstallState.AwaitingConfirmation:
                    AndroidClientPackageInstaller.OpenSystemConfirmation(); PollAndroidPackage(true); return;
                case AndroidClientPackageInstallState.AwaitingResult: return;
                case AndroidClientPackageInstallState.Succeeded: UnityEngine.Application.Quit(); return;
                case AndroidClientPackageInstallState.Unsupported: return;
            }
        }
        if (HasVerifiedClientPackage) clientPackageHandoffRequested = true;
        else clientPackageRequested = true;
    }

    private IEnumerator ProcessClientPackage(MobileResourceBootstrapConfig config, IMobileResourceTransport transport)
    {
        bool handoff = clientPackageHandoffRequested;
        clientPackageRequested = clientPackageHandoffRequested = false;
        if (updatePlan?.Artifact == null || !updatePlan.Available) yield break;
        var artifact = updatePlan.Artifact;
        string selectedIdentity = ArtifactIdentity();
        var downloader = new ClientArtifactDownload(Path.Combine(UnityEngine.Application.persistentDataPath, "ClientPackages"),
            config.appVersion, PlatformKey(), allowDevelopmentHttp, transport);
        failed = false;
        clientPackageMode = true;
        clientPackageNotice = "";
        verifiedClientPackage = verifiedClientIdentity = null;
        Phase(handoff ? "重新校验客户端安装包" : "准备下载客户端", "客户端下载不会覆盖正在运行的客户端。", 0);
        lock (remoteProgressLock) remoteStatus = null;
        remoteCancellation = new CancellationTokenSource();
        CancellationToken cancellation = remoteCancellation.Token;
        Task<string> operation = Task.Run(() => handoff ? downloader.VerifyCompleted(artifact, cancellation, SetRemoteProgress) :
            downloader.Download(artifact, cancellation, SetRemoteProgress));
        while (!operation.IsCompleted) { ReadRemoteProgress(); yield return null; }
        ReadRemoteProgress();
        string path = null;
        Exception failure = null;
        try { path = operation.GetAwaiter().GetResult(); cancellation.ThrowIfCancellationRequested(); }
        catch (Exception exception) { failure = exception; }
        if (cancellation.IsCancellationRequested) failure = new OperationCanceledException("客户端下载已暂停。");
        remoteCancellation.Dispose(); remoteCancellation = null;
        if (failure != null) { RemoteFailure(failure); yield break; }
        if (selectedIdentity != ArtifactIdentity()) { RemoteFailure(new InvalidDataException("Client update selection changed.")); yield break; }
        verifiedClientPackage = path; verifiedClientIdentity = selectedIdentity;
        Phase("客户端安装包已校验", "长度与 SHA-256 已验证，尚未安装。", 0);
        clientPackageNotice = PlatformKey() == "android" ? "完整性已验证 · 下一步核对包名、版本和签名" :
            "完整性已验证，签名未核验 · 请退出游戏后手动安装";
        if (!handoff) yield break;
        if (PlatformKey() == "android")
        {
            androidPackageActive = true;
            AndroidClientPackageInstaller.Prepare(path, artifact.Version, artifact.Sha256, artifact.SizeBytes);
            PollAndroidPackage(true);
        }
        else
        {
            // Open only the containing directory. Never execute the downloaded file or overwrite this process.
            UnityEngine.Application.OpenURL(new Uri(Path.GetDirectoryName(path) + Path.DirectorySeparatorChar).AbsoluteUri);
            status = "已请求打开安装包目录";
            clientPackageNotice = "请退出游戏后手动安装 · 安装包签名尚未核验";
            Debug.Log("Verified client package ready for manual desktop installation: " + path);
        }
    }

    private void Update()
    {
        if (androidPackageActive && MonotonicSeconds() >= nextAndroidPoll) PollAndroidPackage(false);
    }

    private void PollAndroidPackage(bool force)
    {
        nextAndroidPoll = MonotonicSeconds() + .3;
        var snapshot = AndroidClientPackageInstaller.Poll();
        bool changed = force || snapshot.State != androidPackageState;
        androidPackageState = snapshot.State;
        if (changed)
        {
            Debug.Log("Android client install state: " + snapshot.State + "; " + snapshot.Message);
            failed = snapshot.State == AndroidClientPackageInstallState.Failed || snapshot.State == AndroidClientPackageInstallState.Interrupted;
            conciseError = failed ? SafeAndroidInstallError(snapshot.Message) : "";
            string label;
            switch (snapshot.State)
            {
                case AndroidClientPackageInstallState.Preparing: label = "验证安装包身份与签名"; break;
                case AndroidClientPackageInstallState.Ready: label = "安装包身份验证通过"; break;
                case AndroidClientPackageInstallState.PermissionRequired: label = "需要允许此应用安装更新"; break;
                case AndroidClientPackageInstallState.Staging: label = "准备系统安装会话"; break;
                case AndroidClientPackageInstallState.AwaitingConfirmation: label = "请打开系统界面确认安装"; break;
                case AndroidClientPackageInstallState.AwaitingResult: label = "等待系统安装结果"; break;
                case AndroidClientPackageInstallState.Succeeded: label = "系统确认安装成功，请重启客户端"; break;
                case AndroidClientPackageInstallState.Cancelled: label = "安装已取消，可重新准备"; break;
                case AndroidClientPackageInstallState.Unsupported: label = "当前平台不支持系统安装交接"; break;
                default: label = "安装未完成，可重新准备"; break;
            }
            Phase(label, snapshot.Message, 0);
            clientPackageNotice = snapshot.State == AndroidClientPackageInstallState.PermissionRequired ? "仅由你在系统设置中授权，返回后再次确认安装" :
                snapshot.State == AndroidClientPackageInstallState.Ready ? "包名、递增版本与签名已检查 · 安装仍需系统确认" :
                "系统安装需要你的确认 · 原有客户端不会被静默替换";
        }
        if ((snapshot.State == AndroidClientPackageInstallState.Preparing || snapshot.State == AndroidClientPackageInstallState.Staging) && snapshot.Progress >= 0)
        {
            totalBytes = 1000; measurable = true;
            Interlocked.Exchange(ref completedBytes, (long)(Math.Max(0, Math.Min(1, snapshot.Progress)) * totalBytes));
        }
    }

    private static string SafeAndroidInstallError(string message)
    {
        // Plugin messages remain in full logs; only known, bounded reasons reach the status row.
        if (message.Contains("包名")) return "安装包不属于当前客户端，已拒绝安装";
        if (message.Contains("签名")) return "安装包签名不匹配或无效，已拒绝安装";
        if (message.Contains("版本")) return "安装包版本不匹配或未递增，已拒绝安装";
        if (message.Contains("SHA-256") || message.Contains("长度")) return "安装包校验失败，请重新下载";
        if (message.Contains("空间")) return "安装空间不足，请清理后重试";
        if (message.Contains("权限") || message.Contains("策略")) return "系统权限或管理策略阻止安装";
        if (message.Contains("设备不兼容")) return "安装包与当前设备不兼容";
        if (message.Contains("中断") || message.Contains("结果未确认")) return "上次安装已中断，请重新验证安装包";
        return "客户端安装未完成，请重试；详细原因已记入日志";
    }

    private void ReturnFromOptionalClientPackage()
    {
        if (updatePlan == null || updatePlan.Required || AndroidInstallBusy) return;
        if (androidPackageActive) AndroidClientPackageInstaller.Cancel();
        androidPackageActive = clientPackageMode = clientPackageRequested = clientPackageHandoffRequested = false;
        failed = false;
        Phase(ready ? "准备就绪" : "检查并更新", "可以继续当前兼容版本。", 0);
    }
}
}

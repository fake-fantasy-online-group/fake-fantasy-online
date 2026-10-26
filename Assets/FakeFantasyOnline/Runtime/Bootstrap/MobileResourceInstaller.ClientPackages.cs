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
    private DesktopClientPackageInstaller desktopInstaller;
    private string desktopState;
    private double nextDesktopPoll;
    private bool DesktopInstallBusy => desktopInstaller != null && (desktopState == "preparing" || desktopState == "ready" || desktopState == "committed");
    private double nextAndroidPoll;
    private bool desktopCommitSent, androidBeginSent, androidPermissionOpened, androidConfirmationOpened;
    private AndroidClientPackageInstallState androidPackageState = AndroidClientPackageInstallState.Idle;
    private string clientPackageNotice = "";

    private void QueueAutomaticClientPackage(string currentVersion)
    {
        if (updatePlan == null || !updatePlan.Available) return;
        clientPackageMode = true;
        try
        {
            if (ClientUpdateAutomation.HasAttempt(UnityEngine.Application.persistentDataPath, currentVersion, ArtifactIdentity()))
            {
                RemoteFailure(new InvalidOperationException("Previous native client update did not complete in this app version. Inspect the updater result before retrying."));
                conciseError = "上次安装未完成，已停止自动重试";
                clientPackageNotice = "当前客户端和资源保留 · 检查系统提示后可手动重试";
                return;
            }
            clientPackageRequested = true;
        }
        catch (Exception exception) { RemoteFailure(exception); }
    }

    private string ArtifactIdentity()
    {
        var artifact = updatePlan?.Artifact;
        return artifact == null ? null : artifact.Version + "\n" + artifact.Url + "\n" + artifact.SizeBytes + "\n" + artifact.Sha256;
    }
    private bool HasVerifiedClientPackage => verifiedClientPackage != null && verifiedClientIdentity == ArtifactIdentity();
    private bool AndroidInstallBusy => androidPackageActive &&
        (androidPackageState == AndroidClientPackageInstallState.Preparing || androidPackageState == AndroidClientPackageInstallState.Staging ||
         androidPackageState == AndroidClientPackageInstallState.AwaitingConfirmation || androidPackageState == AndroidClientPackageInstallState.AwaitingResult);
    private bool ClientPackageActionEnabled => desktopInstaller != null || !androidPackageActive ||
        (androidPackageState != AndroidClientPackageInstallState.AwaitingResult && androidPackageState != AndroidClientPackageInstallState.Unsupported);

    private void ClearClientPackageSelection()
    {
        if (androidPackageActive) AndroidClientPackageInstaller.Cancel();
        CancelDesktopUpdate();
        androidPackageActive = clientPackageMode = clientPackageRequested = clientPackageHandoffRequested = false;
        verifiedClientPackage = verifiedClientIdentity = null;
        clientPackageNotice = "";
        desktopCommitSent = androidBeginSent = androidPermissionOpened = androidConfirmationOpened = false;
        androidPackageState = AndroidClientPackageInstallState.Idle;
    }

    private string ClientPackageButtonLabel()
    {
        if (desktopState == "manual") return "打开安装包目录";
        if (desktopInstaller != null)
        {
            if (desktopState == "ready" || desktopState == "committed") return "正在安装更新";
            if (desktopState == "preparing") return "取消安装准备";
            return "打开安装包目录";
        }
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
        if (HasVerifiedClientPackage) return PlatformKey() == "android" ? "验证并准备安装" :
            IsDesktopUpdatePlatform ? "验证并准备桌面更新" : "打开安装包目录";
        return failed && clientPackageMode ? "重试客户端下载" : "下载客户端";
    }

    private void RequestClientPackageAction()
    {
        if (updatePlan?.Artifact == null || !updatePlan.Available || remoteCancellation != null) return;
        clientPackageMode = true;
        if (desktopState == "manual") { OpenClientPackageDirectory(); return; }
        if (desktopInstaller != null)
        {
            try
            {
                if (desktopState == "ready" || desktopState == "committed") return;
                if (desktopState == "preparing") { CancelDesktopUpdate(); Phase("安装准备已取消", "原有客户端和资源已保留。", 0); return; }
                OpenClientPackageDirectory(); return;
            }
            catch (Exception exception) { RemoteFailure(exception); return; }
        }
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
            config.appVersion, PlatformKey(), allowDevelopmentHttp, transport,
            availableBytes: ClientStorageSpace.CreateAvailableBytesQuery());
        failed = false;
        clientPackageMode = true;
        clientPackageNotice = "";
        verifiedClientPackage = verifiedClientIdentity = null;
        Phase(handoff ? "重新校验客户端安装包" : "准备下载客户端", "客户端下载不会覆盖正在运行的客户端。", 0);
        lock (remoteProgressLock) remoteStatus = null;
        remoteCancellation = new CancellationTokenSource();
        CancellationToken cancellation = remoteCancellation.Token;
        Task<string> operation = Task.Run(() => ClientUpdateAutomation.WithNetworkRetry(() => handoff ? downloader.VerifyCompleted(artifact, cancellation, SetRemoteProgress) :
            downloader.Download(artifact, cancellation, SetRemoteProgress), cancellation, ReportNetworkRetry));
        while (!operation.IsCompleted) { ReadRemoteProgress(); yield return null; }
        ReadRemoteProgress();
        string path = null;
        Exception failure = null;
        try { path = operation.GetAwaiter().GetResult(); cancellation.ThrowIfCancellationRequested(); }
        catch (Exception exception) { failure = exception; }
        if (cancellation.IsCancellationRequested) failure = new OperationCanceledException("客户端下载已暂停。");
        remoteCancellation.Dispose(); remoteCancellation = null;
        if (failure != null) { RemoteFailure(failure); ResumeInterruptedTransfer(); yield break; }
        if (selectedIdentity != ArtifactIdentity()) { RemoteFailure(new InvalidDataException("Client update selection changed.")); yield break; }
        verifiedClientPackage = path; verifiedClientIdentity = selectedIdentity;
        Phase("客户端安装包已校验", "长度与 SHA-256 已验证，尚未安装。", 0);
        clientPackageNotice = PlatformKey() == "android" ? "完整性已验证 · 下一步核对包名、版本和签名" :
            "完整性已验证，签名未核验 · 请退出游戏后手动安装";
        // Record before native preparation, so an interrupted/rolled-back attempt never
        // automatically repeats when the old app is launched again. Manual Retry is allowed.
        try { ClientUpdateAutomation.RecordAttempt(UnityEngine.Application.persistentDataPath, config.appVersion, selectedIdentity); }
        catch (Exception exception) { RemoteFailure(exception); yield break; }
        desktopCommitSent = androidBeginSent = androidPermissionOpened = androidConfirmationOpened = false;
        if (PlatformKey() == "android")
        {
            androidPackageActive = true;
            AndroidClientPackageInstaller.Prepare(path, artifact.Version, artifact.Sha256, artifact.SizeBytes);
            PollAndroidPackage(true);
        }
        else if (IsDesktopUpdatePlatform)
        {
            try
            {
                using (var process = System.Diagnostics.Process.GetCurrentProcess())
                {
                    string executableDirectory = Path.GetDirectoryName(process.MainModule.FileName);
                    string installRoot = PlatformKey() == "macos" ? Directory.GetParent(Directory.GetParent(executableDirectory).FullName).FullName : executableDirectory;
                    desktopInstaller = DesktopClientPackageInstaller.Prepare(installRoot, UnityEngine.Application.streamingAssetsPath,
                        UnityEngine.Application.persistentDataPath, path, PlatformKey(), config.appVersion, artifact);
                }
                desktopState = "preparing";
                Phase("准备桌面更新", "独立更新器正在检查更新包、应用身份和当前发行的签名要求。游戏尚未退出。", 0);
                clientPackageNotice = "验证通过后自动安装并重启 · 用户数据和旧版本保留";
            }
            catch (Exception exception)
            {
                RemoteFailure(exception);
                clientPackageNotice = "自动安装不可用 · 再次点击可打开安装包目录，退出后手动安装";
                desktopState = "manual";
            }
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
        if (desktopInstaller != null && !desktopCommitSent && MonotonicSeconds() >= nextDesktopPoll) PollDesktopUpdate();
    }

    private static bool IsDesktopUpdatePlatform => PlatformKey() == "windows" || PlatformKey() == "macos";

    private void OpenClientPackageDirectory()
    {
        if (!HasVerifiedClientPackage) return;
        UnityEngine.Application.OpenURL(new Uri(Path.GetDirectoryName(verifiedClientPackage) + Path.DirectorySeparatorChar).AbsoluteUri);
        clientPackageNotice = "请退出游戏后手动安装 · 不要绕过系统签名或安全提示";
    }

    private void CancelDesktopUpdate()
    {
        if (desktopInstaller != null)
        {
            try { desktopInstaller.Cancel(); } catch (Exception exception) { Debug.LogWarning(exception); }
            desktopInstaller.Dispose(); desktopInstaller = null;
        }
        desktopState = null;
    }

    private void PollDesktopUpdate()
    {
        if (desktopCommitSent) return;
        nextDesktopPoll = MonotonicSeconds() + .3;
        try
        {
            string snapshot = desktopInstaller.Poll();
            if (snapshot == desktopState) return;
            desktopState = snapshot;
            if (snapshot == "ready")
            {
                failed = false;
                Phase("正在安装并重新启动", "验证通过，独立更新器将替换应用并重新打开；系统可能要求授权。", 0);
                clientPackageNotice = "自动安装并重启 · 用户设置和资源保留 · 启动失败可恢复旧应用";
                desktopInstaller.Commit();
                desktopCommitSent = true;
                desktopState = "committed";
                UnityEngine.Application.Quit();
            }
            else if (snapshot.StartsWith("failed\n", StringComparison.Ordinal))
            {
                Debug.LogWarning(snapshot);
                failed = true; conciseError = "桌面更新准备失败，请检查日志或手动安装";
                clientPackageNotice = "旧应用与资源保留 · 可打开安装包目录 · 不要绕过系统安全提示";
            }
            else if (snapshot == "cancelled") { CancelDesktopUpdate(); Phase("安装准备已取消", "原有客户端和资源已保留。", 0); }
        }
        catch (Exception exception) { RemoteFailure(exception); desktopState = "manual"; }
    }

    private void PollAndroidPackage(bool force)
    {
        nextAndroidPoll = MonotonicSeconds() + .3;
        var snapshot = AndroidClientPackageInstaller.Poll();
        bool changed = force || snapshot.State != androidPackageState;
        androidPackageState = snapshot.State;
        // Permission may be revoked between Ready polling and Begin. Native only
        // reports this state before staging, so a later grant can safely retry Begin.
        if (snapshot.State == AndroidClientPackageInstallState.PermissionRequired) androidBeginSent = false;
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
                case AndroidClientPackageInstallState.AwaitingConfirmation: label = "请在系统界面确认安装"; break;
                case AndroidClientPackageInstallState.AwaitingResult: label = "等待系统安装结果"; break;
                case AndroidClientPackageInstallState.Succeeded: label = "安装成功，已请求重新打开游戏"; break;
                case AndroidClientPackageInstallState.Cancelled: label = "安装已取消，可重新准备"; break;
                case AndroidClientPackageInstallState.Unsupported: label = "当前平台不支持系统安装交接"; break;
                default: label = "安装未完成，可重新准备"; break;
            }
            Phase(label, snapshot.Message, 0);
            clientPackageNotice = snapshot.State == AndroidClientPackageInstallState.Succeeded ? "若系统阻止自动打开，请从桌面启动新版本" :
                snapshot.State == AndroidClientPackageInstallState.PermissionRequired ? "仅由你在系统设置中授权，返回后自动继续" :
                snapshot.State == AndroidClientPackageInstallState.Ready ? "包名、递增版本与签名已检查 · 是否确认由系统决定" :
                "系统决定是否需要授权或确认 · 安装后尝试自动重新打开";
        }
        // Launch each native step once, only while foreground. Repeated polling and
        // returning from denied/cancelled OS UI must not keep reopening that UI.
        if (!mobilePaused)
        {
            if (snapshot.State == AndroidClientPackageInstallState.PermissionRequired && !androidPermissionOpened)
            { androidPermissionOpened = true; AndroidClientPackageInstaller.OpenPermissionSettings(); }
            else if (snapshot.State == AndroidClientPackageInstallState.Ready && !androidBeginSent)
            { androidBeginSent = true; AndroidClientPackageInstaller.Begin(); }
            else if (snapshot.State == AndroidClientPackageInstallState.AwaitingConfirmation && !androidConfirmationOpened)
            { androidConfirmationOpened = true; AndroidClientPackageInstaller.OpenSystemConfirmation(); }
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
        CancelDesktopUpdate();
        if (androidPackageActive) AndroidClientPackageInstaller.Cancel();
        androidPackageActive = clientPackageMode = clientPackageRequested = clientPackageHandoffRequested = false;
        failed = false;
        skipOptionalClientUpdate = true;
        downloadRequested = true;
        Phase(ready ? "准备就绪" : "检查并更新", "可以继续当前兼容版本。", 0);
    }
}
}

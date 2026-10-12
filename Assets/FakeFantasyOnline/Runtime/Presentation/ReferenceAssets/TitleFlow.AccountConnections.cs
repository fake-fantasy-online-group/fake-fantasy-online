using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    // 06000f0f retains per-click snapshots; registration/recovery below read this
    // component's live fields only when their connection callbacks complete.
    private void BeginTheftAppeal(string account, string when, string reason)
    {
        guiHost._session.ConnectForAccount(ok =>
        {
            if (!ok)
            {
                netMsg = "连接失败, 请重试";
                return;
            }
            guiHost._session.TheftAppeal(account, when, reason);
        });
    }

    private void BeginResetPassword()
    {
        guiHost._session.ConnectForAccount(ok =>
        {
            if (!ok)
            {
                netMsg = "连接失败, 请重试";
                return;
            }
            guiHost._session.ResetPassword(RecoverAccount.Trim(),
                RecoverSecurityCode.Trim(), RecoverPassword);
        });
    }

    private void BeginRegisterAccount()
    {
        guiHost._session.ConnectForAccount(ok =>
        {
            if (!ok)
            {
                netMsg = "连接失败, 请重试";
                return;
            }
            guiHost._session.RegisterAccount(RegisterAccount.Trim(),
                RegisterPassword, RegisterSecurityCode.Trim(),
                RegisterInviteCode.Trim());
        });
    }

    internal void ReceiveRegisterResult(int code, string message)
    {
        netMsg = message;
        if (code != 0) return;
        account = RegisterAccount.Trim();
        CloseRegisterForm();
        guiHost._session.ShowLoginForAccountResult();
        ClearRegisterFields();
    }

    internal void ReceiveResetResult(int code, string message)
    {
        netMsg = message;
        if (code != 0) return;
        account = RecoverAccount.Trim();
        CloseRecoverForm();
        guiHost._session.ShowLoginForAccountResult();
        ClearRecoverFields();
    }

    private void BeginLogin(string submittedAccount, bool savePreferences = true)
    {
        // Accepted native DrawLogin prefix: latch, clock, message, raw password capture,
        // optional preference save, then the actual ConnectAsync call at the button site.
        loggingIn = true;
        loginAtMs = Time.realtimeSinceStartup;
        guiHost._loginDiagnosticsActive = true;
        netMsg = "连接服务器...";
        string transientPassword = password;
        if (savePreferences && (_rememberAccount || _rememberPassword)) SaveLoginPrefs();
        try
        {
            NetClient.I.ConnectTitleLoginForIndependentRuntime(ok =>
            {
                if (!ok)
                {
                    loggingIn = false;
                    PublishPluginTitleResult("connection_failed");
                    transientPassword = null;
                    return;
                }
                // 06000f0d re-reads the current singleton here; it does not retain the connect
                // receiver or read the later edited credential fields. Managed null still throws.
                try { NetClient.I.SendTitleLoginForIndependentRuntime(submittedAccount, transientPassword); }
                finally { transientPassword = null; }
            });
        }
        catch
        {
            transientPassword = null;
            throw;
        }
    }

    internal void ReceiveNativeTitleLoginResult(int code, string message)
    {
        PublishPluginTitleResult(code == 0 ? "login_succeeded" : "login_failed", code);
        netMsg = message;
        loggingIn = false;
        if (code == 0) _page = TitlePage.ServerSelection;
    }

    internal void ReceiveNativeTitleCreateResult(int code, string message)
    {
        netMsg = message;
        if (code == 0) _page = TitlePage.CharacterSelection;
    }

    internal void ReceiveNativeNetworkNotice(string message) => netMsg = message;

    internal void ReceiveNativeNetworkError(string error)
    {
        PublishPluginTitleResult("network_error");
        netMsg = error;
        loggingIn = false;
        entering = false;
    }
}
}

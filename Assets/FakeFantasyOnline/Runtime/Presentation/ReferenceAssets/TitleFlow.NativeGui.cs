using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    // Unity is the only production caller. The session supplies transport/roster inputs;
    // the original TitleFlow.state field independently selects the single visible page.
    private void OnGUI()
    {
        guiHost._projectNativeState?.Invoke();
        if (guiHost._session == null) return;
        TryLoad05Login();
        // Preserve the existing headless/diagnostic boundary without manually
        // invoking this callback or enabling a diagnostic copy of the component.
        if (guiHost._backgroundSessionCheckRequested) return;

        TitlePage page = _page;
        if (page == TitlePage.CharacterCreation)
        {
            creationGui.BindNativeForIndependentRuntime(this,
                guiHost.BeginCreateCharacter, guiHost._session.ReturnToCharacterSelection,
                () => netMsg);
            EnsureStylesForIndependentRuntime();
            EnsureCreationStylesForIndependentRuntime();
            Matrix4x4 previous = BeginGuiFrameForIndependentRuntime(true);
            DrawCharCreate();
            GUI.matrix = previous;
            return;
        }
        if (page == TitlePage.CharacterSelection)
        {
            guiHost._characters = guiHost._session.Characters;
            BindRosterForIndependentRuntime(guiHost._characters);
            EnsureStylesForIndependentRuntime();
            Matrix4x4 previous = BeginGuiFrameForIndependentRuntime(true);
            DrawCharSelect();
            GUI.matrix = previous;
            return;
        }
        if (page == TitlePage.ServerSelection)
        {
            // Keep the live receiver/endpoint readers, not an early state snapshot.
            if (NetClient.I != null)
                NetClient.I.BindTitleServerConnectionForIndependentRuntime(
                    () => guiHost._session.Connected,
                    () => guiHost._session.ServerHostForIndependentRuntime,
                    () => guiHost._session.ServerPortForIndependentRuntime,
                    guiHost.BeginRefreshServerStatus);
            BindServerPageForIndependentRuntime(guiHost._session, () => netMsg,
                guiHost._session.ReturnToLoginFromServerPageForIndependentRuntime,
                result =>
                {
                    if (result.IsSuccess) entering = false;
                    else if (result.Error.Code != ClientSessionCoordinator.ServerSwitchReauthenticationCode)
                        netMsg = ClientRootView.SafeError(result.Error);
                });
            EnsureStylesForIndependentRuntime();
            Matrix4x4 previous = BeginGuiFrameForIndependentRuntime(false);
            DrawServer();
            GUI.matrix = previous;
            return;
        }

        if (page == TitlePage.Register) { DrawRegister(); return; }
        if (page == TitlePage.Recover) { DrawRecover(); return; }
        if (page == TitlePage.Appeal) { DrawAppeal(); return; }
        if (page != TitlePage.Login) return;

        if (loggingIn && Time.realtimeSinceStartup - loginAtMs > 10f)
        {
            loggingIn = false;
            netMsg = "登录超时, 请重试";
        }
        LoadLoginPrefs();
        EnsureStyles();
        Matrix4x4 previousMatrix = BeginGuiFrameForIndependentRuntime(false);
        if (_noticeOpen) DrawUpdateNotice();
        else
        {
            DrawLogin();
            DrawNoticeButton();
        }
        GUI.matrix = previousMatrix;
    }
}
}

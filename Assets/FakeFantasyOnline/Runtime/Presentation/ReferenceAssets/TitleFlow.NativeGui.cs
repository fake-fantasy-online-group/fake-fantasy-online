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
        Color color = GUI.color, background = GUI.backgroundColor, content = GUI.contentColor;
        bool enabled = GUI.enabled;
        Matrix4x4 matrix = GUI.matrix;
        Color selection = GUI.skin.settings.selectionColor, cursor = GUI.skin.settings.cursorColor;
        try { DrawTitleGui(); }
        finally
        {
            GUI.color = color;
            GUI.backgroundColor = background;
            GUI.contentColor = content;
            GUI.enabled = enabled;
            GUI.matrix = matrix;
            GUI.skin.settings.selectionColor = selection;
            GUI.skin.settings.cursorColor = cursor;
        }
    }

    private void DrawTitleGui()
    {
        guiHost._projectNativeState?.Invoke();
        if (guiHost._session == null) { mobileTitleInput.Close(); return; }
        TryLoad05Login();
        // Preserve the existing headless/diagnostic boundary without manually
        // invoking this callback or enabling a diagnostic copy of the component.
        if (guiHost._backgroundSessionCheckRequested) { mobileTitleInput.Close(); return; }

        mobileTitleInput.BeginPage((int)_page);
        if (!MobileClientPresentation.Enabled || _noticeOpen) mobileTitleInput.Close();
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
            Matrix4x4 previous = BeginGuiFrameForIndependentRuntime(false);
            DrawCharSelect();
            GUI.matrix = previous;
            return;
        }
        if (page == TitlePage.ServerSelection)
        {
            PrepareServerPageForPluginAndGui();
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
            ++_loginAttempt;
            loggingIn = false;
            NetClient.I?.CancelTitleLoginForIndependentRuntime();
            netMsg = "登录超时, 请重试";
            PublishPluginTitleResult("login_timeout");
        }
        LoadLoginPrefs();
        EnsureStyles();
        Matrix4x4 previousMatrix = BeginGuiFrameForIndependentRuntime(false);
        DrawLogin();
        GUI.matrix = previousMatrix;
    }
    private void PrepareServerPageForPluginAndGui()
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
    }

}
}

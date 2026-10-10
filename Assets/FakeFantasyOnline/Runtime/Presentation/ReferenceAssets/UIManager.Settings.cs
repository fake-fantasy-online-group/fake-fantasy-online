using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class UIManager
{
    // Original UIManager.Update reaches O before its Escape fallback.
    internal static void UpdateSettingsForIndependentRuntime(
        ReferenceFirstWorldChatUi chat, ReferenceFirstWorldGossipDialog gossip)
    {
        if (chat.TypingNow) return;
        if (MailDlg.Typing) return;
        if (gossip != null && gossip.Typing) return;
        if (GUIUtility.keyboardControl != 0) return;
        if (Input.GetKeyDown(KeyCode.O)) SysConfigDlg.Toggle();
    }
}
}

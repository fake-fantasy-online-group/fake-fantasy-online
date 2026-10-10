using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class UIManager : MonoBehaviour
{
    // The Insert slice of original UIManager.Update shares the original text/focus guards.
    internal static void UpdateHeadHpBarForIndependentRuntime(
        ReferenceFirstWorldChatUi chat, ReferenceFirstWorldGossipDialog gossip)
    {
        if (chat.TypingNow) return;
        if (MailDlg.Typing) return;
        if (gossip != null && gossip.Typing) return;
        if (GUIUtility.keyboardControl != 0) return;
        if (Input.GetKeyDown(KeyCode.Insert)) HeadHpBar.Toggle();
    }
}

}

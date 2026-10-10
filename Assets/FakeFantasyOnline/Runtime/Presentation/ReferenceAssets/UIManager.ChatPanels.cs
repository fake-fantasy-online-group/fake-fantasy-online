using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class UIManager : MonoBehaviour
{
    // The two ChatUI panel slices of official UIManager.Update (0x06000fa8), carried by the one
    // ClientRootView per-frame input driver because that Update itself stays unmounted: there is
    // still no proven official creation point for a UIManager instance, so its Unity Update
    // message never runs.
    //
    // Native shape, identical on both frozen slices (arm64 0x3a7938-0x3a7a28 for Alt+B and
    // 0x3a7fcc-0x3a8010 for V; x86_64 0x3ba5ce-0x3ba821 and the 0x76 test ahead of 0x3d):
    //   Alt+B  ChatUI static +0x68 == 1, GetKeyDown(0x62), GetKey(0x134) || GetKey(0x133),
    //          ChatUI static slot 0 != null  ->  ChatUI.ToggleFacePanel   (0x060000e0)
    //   V      GetKeyDown(0x76), ChatUI static slot 0 != null
    //                                        ->  ChatUI.ToggleChannelList (0x060000e1)
    // 0x62 = KeyCode.B (98) and 0x76 = KeyCode.V (118); 0x134 = KeyCode.LeftAlt (308) and
    // 0x133 = KeyCode.RightAlt (307) are read with GetKey, so the alt modifier must be held.
    // V carries no modifier at all.
    //
    // Alt+B is evaluated before the ordinary-key guards. V is evaluated after all four guards and
    // immediately after Insert. The live driver calls these two bounded methods at those exact
    // points, so TypingNow true admits Alt+B and suppresses V and every ordinary shortcut; false
    // suppresses Alt+B and permits the guarded chain. Nothing else polls B or V.
    internal static void UpdateFacePanelShortcutForIndependentRuntime(
        ReferenceFirstWorldChatUi chat)
    {
        if (ChatUI.TypingNow &&
            Input.GetKeyDown(KeyCode.B) &&
            (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) &&
            ChatUI.I != null)
        {
            ChatUI.I.ToggleFacePanel();
        }
    }

    internal static void UpdateChannelListShortcutForIndependentRuntime(
        ReferenceFirstWorldChatUi chat, ReferenceFirstWorldGossipDialog gossip)
    {
        if (chat.TypingNow) return;
        if (MailDlg.Typing) return;
        if (gossip != null && gossip.Typing) return;
        if (GUIUtility.keyboardControl != 0) return;
        // V: the receiver is the official static ChatUI.I, whose Awake (0x060000e8) is the single
        // writer and which the drawn chat bar is bound to, so the keyboard and the two OnGUI
        // buttons flip one and the same +0x6c byte. A destroyed instance makes the UnityEngine
        // null test false, which is the same skip the native Object.op_Inequality performs.
        if (Input.GetKeyDown(KeyCode.V) && ChatUI.I != null)
        {
            ChatUI.I.ToggleChannelList();
        }
    }
}

}

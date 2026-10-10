using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class UIManager
{
    // Bounded UIManager.Update 0x06000fa8 slices. N precedes O and M in the frozen chain; L, X and
    // unmodified D follow M (and the still-unrestored F/P slices). Separate entry points let the
    // single ClientRootView driver preserve those relative side effects without duplicating a key.
    internal static void UpdateTaskShortcutForIndependentRuntime(
        ReferenceFirstWorldChatUi chat, ReferenceFirstWorldGossipDialog gossip)
    {
        if (chat.TypingNow) return;
        if (MailDlg.Typing) return;
        if (gossip != null && gossip.Typing) return;
        if (GUIUtility.keyboardControl != 0) return;
        if (Input.GetKeyDown(KeyCode.N)) TaskDlg.Toggle();
    }

    internal static void UpdateMailAndDuelShortcutsForIndependentRuntime(
        ReferenceFirstWorldChatUi chat, ReferenceFirstWorldGossipDialog gossip,
        Action toggleMail, Action toggleDuel)
    {
        if (chat.TypingNow) return;
        if (MailDlg.Typing) return;
        if (gossip != null && gossip.Typing) return;
        if (GUIUtility.keyboardControl != 0) return;
        if (Input.GetKeyDown(KeyCode.L)) toggleMail?.Invoke();
        if (Input.GetKeyDown(KeyCode.X)) Win05.ChangeSetHotkey();
        if (Input.GetKeyDown(KeyCode.D)) toggleDuel?.Invoke();
    }
}
}

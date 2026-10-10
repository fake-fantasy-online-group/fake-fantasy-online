using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class UIManager
{
    // Bounded 0x06000fa8 slices for the Q, C, I and T window hotkeys. Both frozen slices run the
    // same four busy/focus guards and then test each hotkey as an independent GetKeyDown IF - no
    // else-if chaining, so a frame in which several keys go down runs every matching action once,
    // in this exact order:
    //   arm64 0x3a7bac TeamDlg.Toggle      (0x06000ea3),  GetKeyDown(0x71)
    //   arm64 0x3a7bdc EquipDlg.Toggle     (0x06000301),  GetKeyDown(99)
    //   arm64 0x3a7c0c InventoryDlg.Toggle (0x06000518),  GetKeyDown(0x69)
    //   arm64 0x3a7c48 Win05.Toggle(StringLiteral:skilldlg, metadata-usage ordinal 16076)
    //                                              (0x06001032), GetKeyDown(0x74)
    // x86_64 reaches 0x3ba94b TeamDlg, 0x3ba97a EquipDlg, 0x3ba9a9 InventoryDlg and 0x3ba9e2
    // Win05 in the same order with the same immediates, so both frozen slices agree.
    //
    // T used to be read by an Event.current test inside ReferenceFirstWorldWindowOwner.DrawHud. It
    // moved onto this Update-time driver for two reasons: that second read lacked the MailDlg.Typing
    // and GUIUtility.keyboardControl guards, and - more importantly - the same press already reached
    // the DrawHud test from OnGUI, so keeping both would toggle the skill dialog twice per press and
    // leave it closed.
    //
    // The three adapter arguments are the very instances the first-world host draws and that its
    // control bar already toggles, so the key and the pointer write one window each instead of
    // instantiating a second copy outside that host lifetime. A missing adapter means the world has
    // no such window yet, and nothing is created for the key press alone.
    internal static void UpdateCharacterWindowHotkeysForIndependentRuntime(
        ReferenceFirstWorldChatUi chat, ReferenceFirstWorldGossipDialog gossip,
        ReferenceFirstWorldTeamDialog team,
        ReferenceFirstWorldLocalEquipmentDialog equipment,
        ReferenceFirstWorldInventoryDialog inventory)
    {
        if (chat.TypingNow) return;
        if (MailDlg.Typing) return;
        if (gossip != null && gossip.Typing) return;
        if (GUIUtility.keyboardControl != 0) return;
        if (Input.GetKeyDown(KeyCode.Q) && team != null) team.Toggle();
        if (Input.GetKeyDown(KeyCode.C) && equipment != null) equipment.Toggle();
        if (Input.GetKeyDown(KeyCode.I) && inventory != null) inventory.Toggle();
        if (Input.GetKeyDown(KeyCode.T)) Win05.Toggle(Win05.SkillDialogName);
    }

    // Bounded 0x06000fa8 slices for F and P, which sit between M and L in the same guarded chain:
    //   arm64 0x3a7de0 String.IsNullOrEmpty(SocialClient.FamilyName) decides the target; the
    //          non-empty branch reaches 0x3a7e20 FamilyDetailDlg.Toggle (0x0600036d) and the empty
    //          branch reaches 0x3a7e04 FamilyDlg.Toggle (0x06000343).
    //   arm64 0x3a7e5c GetKeyDown(0x70) -> Win05.Toggle(StringLiteral:petdlg,
    //                                     metadata-usage ordinal 15578)  (0x06001032)
    // x86_64 0x3bab35 FamilyDlg.Toggle / 0x3bab3e FamilyDetailDlg.Toggle and 0x3bab77 Win05.Toggle
    // sit in the same relative positions.
    // Both family windows are drawn by the first-world host through their own Draw entry points, so
    // flipping the static visibility here shows and hides the same window the host already renders.
    internal static void UpdateFamilyAndPetHotkeysForIndependentRuntime(
        ReferenceFirstWorldChatUi chat, ReferenceFirstWorldGossipDialog gossip)
    {
        if (chat.TypingNow) return;
        if (MailDlg.Typing) return;
        if (gossip != null && gossip.Typing) return;
        if (GUIUtility.keyboardControl != 0) return;
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (!string.IsNullOrEmpty(SocialClient.FamilyName))
            {
                FamilyDetailDlg.Toggle();
            }
            else
            {
                FamilyDlg.Toggle();
            }
        }
        if (Input.GetKeyDown(KeyCode.P)) Win05.Toggle(Win05.PetDialogName);
    }
}

}

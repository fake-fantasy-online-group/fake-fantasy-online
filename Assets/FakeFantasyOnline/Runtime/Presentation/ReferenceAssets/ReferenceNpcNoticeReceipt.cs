using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>Recovered display tails of Dispatch 0600077f, invoked at the receipt boundary.</summary>
internal static partial class ReferenceNpcNoticeReceipt
{
    internal static void ReceiveNotice(string text, byte mode,
        Func<DialogUI> currentDialog)
    {
        // Reuses the established 1.5.8 mode bodies: arm64 switch 2587d0,
        // x86_64 2674ef. The host adapter supplies only current owner identity.
        if (mode == 6)
        {
            // arm64 25c288..25c348 / x86_64 ShowSay tail 26b10e:
            // existing, visible DialogUI -> ShowSay; otherwise the separate chat tail.
            DialogUI dialog = currentDialog();
            if (dialog != null && dialog.Visible)
                dialog.ShowSay(text);
            else
                ChatUI.Add("系统", text,
                    new Color(220f / 255f, 220f / 255f, 180f / 255f, 1f), string.Empty, null);
            return;
        }
        if (mode == 4)
        {
            // arm64 2587f0..258850 / x86_64 26ae89: call even without a world;
            // PlayerTaskTrack.Show owns its native absent-instance behavior.
            PlayerTaskTrack.Show(text);
            return;
        }
        if (mode == 5)
        {
            // arm64 25c34c..25c394 / x86_64 26b019: raw text, no Push(id)/seen writer.
            NewbieTipBox.PushText(text);
            return;
        }

        // Native low-byte RGB conversion: mode 1 ff78ff78, mode 2 ffffdc78,
        // modes 0,3,>=7 ff7878ff. Alpha is 1 in every arm.
        Color colour;
        if (mode == 1)
            colour = new Color(120f / 255f, 1f, 120f / 255f, 1f);
        else if (mode == 2)
            colour = new Color(120f / 255f, 220f / 255f, 1f, 1f);
        else
            colour = new Color(1f, 120f / 255f, 120f / 255f, 1f);
        ChatUI.Add("系统", text, colour, string.Empty, null);
        if (mode == 3)
        {
            // arm64 25c408..25c4f0 / x86_64 Add 26af43..26af64: Add first;
            // only then the Unity-valid PlayerController.I check and CancelWork call.
            PlayerController controller = PlayerController.I;
            if (controller)
                controller.CancelWork();
        }
    }
}

}

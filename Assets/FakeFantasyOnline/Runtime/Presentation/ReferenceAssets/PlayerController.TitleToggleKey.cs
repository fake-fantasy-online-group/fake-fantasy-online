using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class PlayerController
{
    /// <summary>
    /// Official 1.5.8 <c>PlayerController.Update</c> (0x06000a00) title-visibility branch:
    /// <c>Input.GetKeyDown(KeyCode.Z)</c> inverts <c>CharData.showTitle</c> (field token
    /// 67114318, static slot <c>+0x248</c>) and then reports the state it reached through
    /// <c>ChatUI.Add</c>.
    /// </summary>
    /// <remarks>
    /// The evidence is the frozen dual-architecture export. arm64 (Update 0x2aca04) calls
    /// <c>Input.GetKeyDown(0x7a)</c> at 0x2adb34 and then evaluates the guards in this order:
    /// 0x2adb58 <c>ChatUI.get_TypingNow</c> (ChatUI static byte at +0x68), 0x2adb78
    /// <c>Win05.GmTyping</c> (0x06001057), 0x2adba0 <c>GUIUtility.keyboardControl == 0</c>;
    /// the flip itself is the byte load at 0x2adbc0 (<c>ldrb w9, [x8, #0x248]</c>), the xor at
    /// 0x2adbc4 and the store at 0x2adbc8. x86_64 (Update 0x2bd5f0) repeats the same branch with
    /// the same key literal and the flip at 0x2be803 (<c>movzbl 0x248(%rax), %ecx</c>),
    /// 0x2be80c (<c>xorb $0x1, %dl</c>) and 0x2be80f (<c>movb %dl, 0x248(%rax)</c>).
    /// Two facts are easy to get wrong and are deliberate here:
    /// the key is <c>Z</c> (Unity <c>KeyCode.Z</c> = 122 = 0x7a), not a guessed letter, and
    /// <c>MailDlg.Typing</c> plus <c>GossipDlg.get_Typing</c> are absent from this branch -
    /// they only guard the Space pickup branch further down the same method (arm64 0x2adc58),
    /// so adding them here would be a product invention.
    /// The confirmation line is part of the same branch, not a separate feature: arm64 calls
    /// <c>ChatUI.Add</c> (0x060000e9) at 0x2adc4c, after the store, with the channel literal at
    /// 0x14aa7d8 (系统, metadata literal 7565), the colour components
    /// <c>0x3f34b4b5 = 180f/255f</c>, <c>0x3f5cdcdd = 220f/255f</c>, <c>1f</c>, <c>1f</c> in
    /// s0..s3, the who literal at 0x149bb98 (empty string) and a null share list in x3.
    /// The text is selected by 0x2adbe8/0x2adbec from the value the byte held <b>before</b> the
    /// flip: <c>cmp w9, #0x0</c> then <c>csel x8, x10, x11, eq</c> picks 0x14a9768
    /// (已显示称号, literal 7039) when the old byte was 0 and 0x14a97a8 (已隐藏称号,
    /// literal 7047) otherwise. x86_64 is the same selection inverted by
    /// <c>testb %cl, %cl</c> / <c>jne</c> at 0x2be81c, with the literals at 0x149ec50 (系统),
    /// 0x149dbe0 (已显示称号), 0x149dc20 (已隐藏称号) and 0x1490010 (empty who).
    /// </remarks>
    private void HandleTitleToggleKey()
    {
        if (!Input.GetKeyDown(KeyCode.Z)) return;
        if (ChatUI.TypingNow || Win05.GmTyping || GUIUtility.keyboardControl != 0) return;
        bool visibleBeforeFlip = CharData.showTitle;
        CharData.showTitle = !visibleBeforeFlip;
        // Official order: invert first, then report the state the byte reached, chosen from
        // the pre-flip value. One keypress is one inversion and one chat line.
        ChatUI.Add("系统", visibleBeforeFlip ? "已隐藏称号" : "已显示称号",
            new Color(180f / 255f, 220f / 255f, 1f, 1f), "", null);
    }
}
}

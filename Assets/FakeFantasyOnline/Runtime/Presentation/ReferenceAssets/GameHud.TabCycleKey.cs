using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    /// <summary>
    /// The official keyboard arm that owns CombatWorld.CycleNearbyMonster (0x0600014a). Its only
    /// native caller is GameHud.OnGUI (0x06000414): arm64 bl 0x2432f0 at 0x42cdf4, x86_64
    /// call 0x250b40 at 0x4429a4, one call site per architecture and no other xref.
    /// </summary>
    /// <remarks>
    /// Gates, in the order both frozen bodies read them:
    /// 1. GameSettings.HotbarBottom (static System.Boolean, GameSettings static slot 7): arm64
    ///    0x42c404/0x42c428 "ldrb w8,[x8,#7]" then "cmp w8,#1", x86_64 the same byte at
    ///    0x4428cf's enclosing block. w20 (arm64, defined 0x42c4e4 = 0 and 0x42c728 = 1) / r15b
    ///    (x86_64, tested at 0x4428cf) is the local that block leaves behind.
    /// 2. hb24Bk (+0x250, UnityEngine.Texture2D) != null through Object.op_Inequality: arm64
    ///    0x42c358 "ldr x21,[x26,#0x250]" then bl 0x104b39c at 0x42c37c, whose false arm drops to
    ///    w20 = 0. This is the hotbar-present door; official OnGUI reads it as a field of its own
    ///    receiver, so the product's static GameHud.I null test before the call has no native
    ///    counterpart.
    /// 3. Event.current != null, then Event.get_type == 4 (EventType.KeyDown): arm64 0x42ccd4 /
    ///    "cmp w0,#4" at 0x42cce0, x86_64 0x4428c1 / "cmp eax,4" at 0x4428c6.
    /// 4. Event.get_keyCode == 9 (KeyCode.Tab): arm64 0x42ccec / "cmp w0,#9" at 0x42ccf8,
    ///    x86_64 0x4428dd / "cmp eax,9" at 0x4428e2. No modifier, no GetKey, no rawType and no
    ///    second key is read anywhere in the arm.
    /// 5. !ChatUI.TypingNow (static System.Boolean, ChatUI static slot 0x68): arm64 0x42cd38
    ///    "ldrb w8,[x8,#0x68]" with tbz w8,#0 entering the arm, x86_64 0x44291a "cmp byte ptr
    ///    [rax+0x68],0" with je entering.
    /// 6. GUIUtility.get_keyboardControl == 0: arm64 0x42cdd0 bl 0x107c398 then cbnz w0,#0 leaves,
    ///    x86_64 0x442981 call 0x106c1a0 then "test eax,eax / jne" leaves.
    /// Only then comes CycleNearbyMonster, and a true return runs exactly one Event.Use on the same
    /// Event (arm64 0x42ce04 bl 0x1073858, x86_64 0x4429b2 call 0x1063c90) - nothing else, no
    /// sound, no packet and no second selection.
    /// </remarks>
    internal void HandleTabCycleKeyForIndependentRuntime()
    {
        if (!GameSettings.HotbarBottom || hb24Bk == null) return;
        Event current = Event.current;
        if (current == null || current.type != EventType.KeyDown) return;
        if (current.keyCode != KeyCode.Tab) return;
        if (ChatUI.TypingNow) return;
        if (GUIUtility.keyboardControl != 0) return;
        if (CombatWorld.CycleNearbyMonster()) current.Use();
    }
}
}

using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class PlayerController
{
    // Official 1.5.8 PlayerController.Update (0x06000a00), between the stall sign and Z.
    internal void HandleGlowToggleKeyForIndependentRuntime()
    {
        bool modifier = Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl) ||
            Input.GetKey(KeyCode.LeftCommand) ||
            Input.GetKey(KeyCode.RightCommand);

        if (Input.GetKeyDown(KeyCode.G) && modifier &&
            (ChatUI.TypingNow || Win05.GmTyping || GUIUtility.keyboardControl != 0))
        {
            ChatUI.Add("系统", "有输入框正占着键盘(聊天/窗口), 先点一下地面再按 Ctrl+G",
                new Color(1f, 180f / 255f, 120f / 255f, 1f), "", null);
        }

        // The native branch reads G and the focus gates again, even after its warning.
        if (!Input.GetKeyDown(KeyCode.G) || !modifier ||
            ChatUI.TypingNow || Win05.GmTyping || GUIUtility.keyboardControl != 0)
            return;

        PlayerAvatar.Appear appearance = CharData.CombinedAppear();
        int weaponR = appearance.WeaponR;
        int weaponL = appearance.WeaponL;
        if (SuitFxDB.GlowTableCount > 0 &&
            !SuitFxDB.IsGlowWeaponModel(weaponR) &&
            !SuitFxDB.IsGlowWeaponModel(weaponL))
        {
            string message = string.Format(
                "当前武器没有光效, 无法调整 [外形R={0} L={1} 变装R={2} 装备R={3} 表={4}]",
                new object[] { weaponR, weaponL, CharData.avWeaponR, CharData.apWeaponR,
                    SuitFxDB.GlowTableCount });
            ChatUI.Add("系统", message,
                new Color(1f, 160f / 255f, 120f / 255f, 1f), "", null);
            return;
        }

        int oldMode = CharData.glowMode;
        int unlock = CharData.glowUnlock;
        int usableCount = EffectPlayer.GlowUsableCount(unlock);
        // Preserve the official arm although the current helper returns at least two.
        if (usableCount < 2)
        {
            ChatUI.Add("系统", "没有可切换的武器光效档",
                new Color(1f, 180f / 255f, 120f / 255f, 1f), "", null);
            return;
        }

        int nextMode = EffectPlayer.GlowNextMode(oldMode, CharData.glowUnlock);
        CharData.glowMode = nextMode;
        NetClient client = NetClient.I;
        // NetClient is a MonoBehaviour, but this native gate is a managed-reference test.
        if (!ReferenceEquals(client, null)) client.SetGlowModeForIndependentRuntime(nextMode);

        string suffix = usableCount > 3
            ? string.Format(" ({0}档可用)", usableCount)
            : " (更多颜色用【光效染色珠】解锁)";
        // Direct indexing intentionally retains null/bounds failures after the write/send.
        string feedback = string.Concat("武器光效→", EffectPlayer.GlowModeName[nextMode], suffix);
        ChatUI.Add("系统", feedback,
            new Color(1f, 200f / 255f, 120f / 255f, 1f), "", null);
    }
}
}

using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    private static float _lockReqAt;
    private static string _lockReqName = "";

    private static void LockReqSent(string name)
    {
        _lockReqAt = Time.realtimeSinceStartup;
        _lockReqName = name;
    }

    internal static void LockReqSentPublic(string name) => LockReqSent(name);
    internal static void LockReqClear() => _lockReqAt = 0f;

    internal static void TickLockWatchdogForIndependentRuntime(System.Action<string, Color> systemMessage)
    {
        if (_lockReqAt > 0f)
        {
            if (Time.realtimeSinceStartup - _lockReqAt >= 2.5f)
            {
                _lockReqAt = 0f;
                systemMessage("服务端没有响应【" + _lockReqName +
                    "】的道具锁请求(2.5秒) —— 这条功能可能还没部署到你连的服务器上",
                    new Color(1f, 150f / 255f, 150f / 255f, 1f));
            }
        }
    }

    // Official 0x06000521. The owner writes directly to the system chat.
    public static void TickLockWatchdog()
    {
        if (_lockReqAt <= 0f || Time.realtimeSinceStartup - _lockReqAt < 2.5f) return;
        _lockReqAt = 0f;
        ChatUI.Add(
            "系统",
            "服务端没有响应【" + _lockReqName + "】的道具锁请求(2.5秒) —— 这条功能可能还没部署到你连的服务器上",
            new Color(1f, 100f / 170f, 100f / 170f, 1f),
            "",
            null);
    }

    private static Texture2D _lockIco;
    public static Texture2D LockIco
    {
        get
        {
            if (_lockIco == null && ResLoader.I != null)
                _lockIco = ResLoader.I.LoadGsa("res/effect/lock_b_01.gsa");
            return _lockIco;
        }
    }

    public partial struct Item
    {
        public bool locked;
    }

    public static bool LockModHeld(Event ev)
    {
        if (ev != null && (ev.modifiers & EventModifiers.Shift) != 0) return true;
        if (Input.GetKey(KeyCode.LeftShift)) return true;
        if (Input.GetKey(KeyCode.RightShift)) return true;
        if (Input.GetKey(KeyCode.LeftCommand)) return true;
        if (Input.GetKey(KeyCode.RightCommand)) return true;
        return ev != null && (ev.modifiers & EventModifiers.Command) != 0;
    }

    public static bool IsLocked(int tab, int bagIdx)
    {
        if (tab < 0 || tab >= Items.Length || Items[tab] == null) return false;
        foreach (Item item in Items[tab])
            if (item.bagIdx == bagIdx) return item.locked;
        return false;
    }

    public static void DrawLockBadge(Rect cell, float s)
    {
        Texture2D icon = LockIco;
        if (icon == null) return;
        float width = GsaDecoder.W(icon);
        float height = GsaDecoder.H(icon);
        GUI.DrawTexture(new Rect(cell.xMax - s * (width + 2f), cell.yMax - s * (height + 2f),
            s * width, s * height), icon);
    }
}
}

using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class EquipDlg
{
    public static EquipDlg I;

    // Official 0x06000311 step 1: this is the delegate slot ApplyTitle reads from
    // EquipDlg static_fields +0x18 (arm64 0x3fd7c4 load, indirect call CALLIND at 0x3fd8c0;
    // x86_64 0x412a94 load, indirect call CALLIND at 0x412d88) with the caller's cat and sub
    // before every other step. EquipDlg..cctor (0x06000317) never writes this slot, so it is
    // null unless something assigns it; no subscriber is wired here, which leaves it null and
    // makes the invocation a no-op. Whether the official client ever assigns a subscriber is
    // not established by this evidence pack - only that ..cctor is not the writer.
    public static Action<int, int> OnSetTitle;

    private bool open, loaded, titleOpen, taskOpen, placed;
    private int tab;
    private int titleCat = 5;
    private int taskSel, taskScroll;
    private float winX = -1f, winY;
    private bool dragging;
    private Vector2 dragOff;

    // Official 0x06000308. Apart from the two class-init guards the whole emitted body is one
    // store: the instance Unity has just created becomes the singleton. arm64 0x3f488c loads
    // static_fields (+0xb8) and 0x3f4890 stores param_1 into slot 0, write barrier at
    // 0x3f48a4; x86_64 0x408ffa..0x409019 are the two class-init guards and 0x409032 is the
    // matching write barrier for the same store. No GameObject is created here and no OnGUI is
    // registered: Awake only adopts the instance a creator already made, so adding it cannot
    // produce a second window or a duplicated OnGUI pass.
    private void Awake() => I = this;

    // Official 0x0600030a first statement: EquipDlg.Load stores this byte (+0x21) before the
    // ResLoader instance test and before any art is read (arm64 0x3f4cf4 stores the constant 1,
    // x86_64 0x40942c), so the window counts as loaded even when the art never arrives and
    // Load is never retried.
    internal bool LoadedForIndependentRuntime { get => loaded; set => loaded = value; }

    internal bool OpenForIndependentRuntime { get => open; set => open = value; }
    internal bool TitleOpenForIndependentRuntime { get => titleOpen; set => titleOpen = value; }
    internal bool TaskOpenForIndependentRuntime { get => taskOpen; set => taskOpen = value; }
    internal bool PlacedForIndependentRuntime { get => placed; set => placed = value; }
    internal int TabForIndependentRuntime { get => tab; set => tab = value; }
    internal float XForIndependentRuntime { get => winX; set => winX = value; }
    internal float YForIndependentRuntime { get => winY; set => winY = value; }

    // Official 0x0600030d equipment-page doll slots: the window's own PlayerAvatar at +0x130,
    // the number of PlayerAvatar.Build attempts already spent on the current revision at +0x138
    // and the CharData revision those attempts belong to at +0x13c - EquipDlg field names doll,
    // dollTry and dollVer (arm64
    // 0600030d.c:996-1035, x86_64 0600030d.c:1049-1088). EquipDlg..cctor (0x06000317) writes
    // none of them. Instance ctor0316 stores dollVer = -1 before allocating pend[7].
    private PlayerAvatar doll;
    private int dollTry;
    private int dollVer = -1;
    private float s;
    private readonly int[] pend = new int[7];

    internal PlayerAvatar DollForIndependentRuntime { get => doll; set => doll = value; }
    internal int DollTryForIndependentRuntime { get => dollTry; set => dollTry = value; }
    internal int DollVersionForIndependentRuntime { get => dollVer; set => dollVer = value; }

    // Official 0x06000301.
    public static void Toggle()
    {
        if (I == null) I = new GameObject("EquipDlg").AddComponent<EquipDlg>();
        I.open = !I.open;
        if (I.open) I.EnsureOnScreen(257f * UIScale.Factor, 506f * UIScale.Factor);
    }

    // Official 0x06000302.
    internal void EnsureOnScreen(float w, float h)
    {
        if (!placed) return;
        float x = winX, y = winY;
        int width = Screen.width;
        int height = Screen.height;
        ApplyScreenBounds(x, y, w, h, width, height);
    }

    internal bool ApplyScreenBounds(float x, float y, float w, float h, int width, int height)
    {
        if (!placed) return false;
        if (!(x < width && x + w > 0f && y < height && y + h > 0f)) placed = false;
        return placed;
    }

    // Official 0x06000303 / 0x06000304.
    public static bool Opened => I != null && I.open;
    public static bool TitleSubOpen => I != null && I.open && I.titleOpen;

    // Official 0x06000305. No post-creation Unity-null early return.
    public static void EnsureOpen()
    {
        if (I == null) I = new GameObject("EquipDlg").AddComponent<EquipDlg>();
        I.open = true;
        I.EnsureOnScreen(257f * UIScale.Factor, 506f * UIScale.Factor);
    }

    // Official 0x06000306.
    public static void OpenPanel()
    {
        if (I == null) I = new GameObject("EquipDlg").AddComponent<EquipDlg>();
        I.open = true;
        I.tab = 0;
        float scale = UIScale.Factor;
        I.winX = 8f;
        I.winY = (Screen.height - 506f * scale) * 0.5f;
        I.placed = true;
    }

    // Official 0x06000307.
    public bool IsOpen => open;

    // Live native arrays, never a snapshot written back by a map borrower.
    public int At(int index)
    {
        int[] values = CharData.attrsTotal ?? CharData.attrs;
        if (values == null || index >= values.Length) return 0;
        return values[index];
    }

    private int PendSum
    {
        get
        {
            int sum = 0;
            for (int index = 0; index < pend.Length; index++) sum += pend[index];
            return sum;
        }
    }

    // Official 0x0600030c: sample live input only after the instance open/placed gates.
    // Use the scale last written by this window's OnGUI, including its initial zero value.
    public bool MouseOver()
    {
        if (!open || !placed) return false;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        Vector2 point = new Vector2(mx, my);
        return StrictRect(winX - 26f * s, winY, 313f * s, 506f * s, point) ||
            titleOpen && StrictRect(winX + 284f * s, winY + 12f * s, 98f * s, 112f * s, point) ||
            taskOpen && StrictRect(winX + 384f * s, winY + 36f * s, 126f * s, 179f * s, point);
    }

    private static bool StrictRect(float x, float y, float width, float height, Vector2 point)
    {
        return point.x >= x && point.x < x + width && point.y >= y && point.y < y + height;
    }

    // Scene-root lifetime belongs to the existing final world purge. In particular there
    // is no borrower Dispose, native close, DDOL, OnDestroy reset or art reload here.
}
}

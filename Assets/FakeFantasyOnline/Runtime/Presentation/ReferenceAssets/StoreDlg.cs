using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class StoreDlg : MonoBehaviour
{
    public struct WItem
    {
        public int tab, slot, id, count, curDur, maxDur, quality;
        public byte blocked;
        public string name, desc;
        public Texture2D icon;
    }
    public static StoreDlg I;
    public static List<WItem> WhItems = new List<WItem>();
    public static long WhBank;
    private bool visible, tried, needDock;
    private int tab;
    private readonly Texture2D[] tabCover = new Texture2D[4];
    public int TabCount = 1;
    private float s, winX = float.NaN, winY;
    private int _wdGen = -1;

    // Official 0x06000fd8..0x06000fdc.
    public static int CurTab => I != null ? I.tab : 0;
    // Official 0x06000fdf: reads the instance bool field visible at +0x20 only while the static
    // instance exists, and is false otherwise. arm64 rva 0x3c93e0 and x86_64 rva 0x3ddb60 agree.
    public static bool Visible => I != null && I.visible;
    public static void SetData(byte tabs, long money, List<WItem> items)
    {
        Ensure();
        I.TabCount = tabs;
        WhBank = money;
        WhItems = items ?? new List<WItem>();
        if (I.tab >= tabs) I.tab = 0;
    }
    public static void PatchSlot(int whTab, int slot, int id, WItem item)
    {
        Ensure();
        int index = WhItems.FindIndex(row => row.tab == whTab && row.slot == slot);
        if (id <= 0) { if (index >= 0) WhItems.RemoveAt(index); }
        else if (index < 0) WhItems.Add(item);
        else WhItems[index] = item;
    }
    public static void PatchHeader(byte tabs, long money)
    {
        Ensure();
        I.TabCount = tabs;
        WhBank = money;
        if (I.tab >= tabs) I.tab = 0;
    }
    private static void Ensure()
    {
        if (I != null) return;
        var root = new GameObject("StoreDlg");
        I = root.AddComponent<StoreDlg>();
        DontDestroyOnLoad(root);
    }
    // Official 0x06000fdd..0x06000fe0.
    public static void Toggle()
    {
        Ensure();
        I.visible = !I.visible;
        if (I.visible)
        {
            I.needDock = true;
            if (NetClient.I != null) NetClient.I.OpenWarehouse();
        }
    }
    public static void OpenNow() { Ensure(); if (!I.visible) Toggle(); }
    // GUI-space, explicit physical-pointer hit testing; independent of Input.mousePosition.
    internal static bool MobileContainsPointer(Vector2 point)
    {
        if (!Visible) return false;
        float scale = UIScale.Factor;
        float width = 423f, height = 501f;
        bool centered = float.IsNaN(I.winX);
        float x = centered ? (Screen.width - width * scale) * .5f : I.winX;
        float y = centered ? (Screen.height - height * scale) * .5f : I.winY;
        if (I.needDock)
        {
            float limit = Mathf.Max(0f, Screen.width - 850f * scale);
            x = centered ? limit * .5f : Mathf.Clamp(x, 0f, limit);
            y = Mathf.Clamp(y, 0f, Mathf.Max(0f, Screen.height - height * scale));
        }
        if (new Rect(x, y, width * scale, height * scale).Contains(point)) return true;
        return false;
    }


    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        return I.WindowRectForIndependentRuntime(scale).Contains(mouse);
    }
    // Official 0x06000fe1. Any point inside the window is handled; non-cell targets use -1.
    public static bool TryDropDeposit(byte bagTab, int bagSlot, Vector2 mouse)
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        Rect window = I.WindowRectForIndependentRuntime(scale);
        if (!window.Contains(mouse)) return false;
        Vector2 local = (mouse - window.position) / scale;
        float x = local.x - 14f, y = local.y - 66f;
        int target = -1;
        if (x >= 0f && y >= 0f)
        {
            int column = (int)(x / 40f), row = (int)(y / 40f);
            if (column < 10 && row < 10 && x - column * 40f < 36f && y - row * 40f < 36f)
                target = column + row * 10;
        }
        if (NetClient.I != null) NetClient.I.WarehouseMoveForIndependentRuntime(0, bagTab, bagSlot, I.tab, int.MaxValue, target);
        return true;
    }
    internal Rect WindowRectForIndependentRuntime(float scale)
    {
        bool center = float.IsNaN(winX);
        return new Rect(center ? (Screen.width - 423f * scale) * .5f : winX,
            center ? (Screen.height - 501f * scale) * .5f : winY, 423f * scale, 501f * scale);
    }
    internal static void ReleaseIndependentRuntime()
    {
        // Queue the original root for Unity destruction. Keep its live fields and static
        // reference intact until the engine destroys it; map borrowers never release it.
        if (I != null) Destroy(I.gameObject);
    }
}
}

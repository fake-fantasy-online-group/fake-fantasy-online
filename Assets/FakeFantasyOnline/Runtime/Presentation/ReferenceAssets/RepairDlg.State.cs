using System;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class RepairDlg : MonoBehaviour
{
    public static RepairDlg I;
    public static bool Picking;
    public static byte PickMode;
    private bool visible;
    private byte mode;
    private int slot;
    private long fee;
    private int count, itemId;
    private string info;
    private Texture2D icon;
    private byte targetKind = 1, tab;
    private Texture2D bg, closeIc, repairBtn, cancelBtn, goldIc, silverIc, copperIc;
    private bool tried;
    private const float NW = 193f, NH = 213f;
    private float s, winX, winY;

    public static bool Visible => I != null && I.visible;
    public static void Ensure()
    {
        if (I != null) return;
        var go = new GameObject("RepairDlg");
        I = go.AddComponent<RepairDlg>();
        DontDestroyOnLoad(go);
    }
    public static void Hide() { if (I != null) I.visible = false; }

    // Official 0x06000b3f: Ensure -> Picking = true -> PickMode = mode -> EquipDlg.OpenPanel()
    // -> if (!InventoryDlg.IsOpen) InventoryDlg.Toggle() -> if (CursorManager.I == null) return
    // -> CursorManager.I.SetRepairMode(true).
    public static void BeginPick(byte mode)
    {
        Ensure();
        Picking = true;
        PickMode = mode;
        EquipDlg.OpenPanel();
        if (!InventoryDlg.IsOpen) InventoryDlg.Toggle();
        if (CursorManager.I == null) return;
        CursorManager.I.SetRepairMode(true);
    }

    public static void CancelPick()
    {
        if (!Picking) return;
        Picking = false;
        if (CursorManager.I != null) CursorManager.I.SetRepairMode(false);
    }

    public static void Show(byte mode, int slot, long fee, int count, int itemId, string info,
        Texture2D icon, byte targetKind = 1, byte tab = 0)
    {
        Ensure();
        I.mode = mode; I.slot = slot; I.fee = fee; I.count = count; I.itemId = itemId;
        I.info = info; I.icon = icon; I.targetKind = targetKind; I.tab = tab;
        I.visible = true;
        CancelPick();
    }
}
}

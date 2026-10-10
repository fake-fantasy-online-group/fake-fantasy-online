using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    private static int heldWhTab, heldWhCount, heldBarPet = -1;
    private static Texture2D heldIcon;
    private static string heldName;
    private static bool heldPet, heldBound;
    private static int heldId;
    private static Action clearBarPetForIndependentRuntime;
    private bool open, placed;
    // Official 0x06000532: the constructor sets winX to -1 and leaves winY at default.
    private float winX = -1f, winY;
    internal bool OpenForIndependentRuntime { get => open; set => open = value; }
    internal float XForIndependentRuntime { get => winX; set => winX = value; }
    internal float YForIndependentRuntime { get => winY; set => winY = value; }
    internal static bool HeldPetForIndependentRuntime => heldPet;
    public static bool HasHeldAny => heldSlot >= 0 || heldWhSlot >= 0 || heldBarPet >= 0;
    public static bool HasHeldBarPet => heldBarPet >= 0;
    // Official 0x060004fb / 0x060004fc: all held sources share one pickup gate.
    public static int HeldBarPet => heldBarPet;
    public static void PickUpBarPet(int carrySlot, Texture2D icon, string name)
    {
        if (HasHeldAny) return;
        heldIcon = icon; heldBarPet = carrySlot;
        heldName = name ?? ""; heldPet = false;
    }
    public static bool HeldFromWarehouse => heldWhSlot >= 0;
    public static int HeldWhSlot => heldWhSlot;
    public static int HeldWhTab => heldWhTab;
    public static Texture2D HeldIcon => heldIcon;
    public static string HeldName => heldName;
    // Official 0x06000503: preserve an already held bag item, warehouse item or bar pet.
    public static void PickUpFromWarehouse(int tab, int slot, Texture2D icon, string name, int count)
    {
        if (HasHeldAny) return;
        heldWhTab = tab; heldWhSlot = slot; heldWhCount = count;
        heldIcon = icon; heldName = name ?? ""; heldPet = false;
    }
    public static bool ShiftHeld(Event e) => (e != null && e.shift) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    public static void DockRightOf(float x, float y)
    {
        if (I == null) I = new GameObject("InventoryDlg").AddComponent<InventoryDlg>();
        I.open = true; I.winX = x; I.winY = y; I.placed = true;
    }
    internal static void BindBarPetClearForIndependentRuntime(Action clear)
    {
        clearBarPetForIndependentRuntime = clear;
    }
    internal static void ClearBarPetForIndependentRuntime()
    {
        heldBarPet = -1; clearBarPetForIndependentRuntime = null;
    }
    internal static void BindHeldDetailsForIndependentRuntime(bool pet, bool bound, Texture2D icon, string name, int id)
    { heldPet = pet; heldBound = bound; heldIcon = icon; heldName = name; heldId = id; }
    // Original InventoryDlg.OnGUI held-item tail (0527), after cell input.
    internal void HeldClickForIndependentRuntime(Rect inventoryWindow, Event ev)
    {
        if ((!HasHeld && !HeldFromWarehouse) || ev.type != EventType.MouseDown) return;
        PlayerController.SuppressWorldClick();
        if (ev.button == 1)
        {
            heldWhSlot = -1;
            DropBagReferenceForIndependentRuntime();
            ev.Use(); return;
        }
        if (ev.button != 0) return;
        if (heldWhSlot >= 0)
        {
            if (inventoryWindow.Contains(ev.mousePosition))
            {
                if (NetClient.I != null) NetClient.I.WarehouseMoveForIndependentRuntime(1, 0, heldWhSlot, heldWhTab, heldWhCount, -1);
                heldWhSlot = -1; ev.Use();
            }
            else if (!StoreDlg.MouseOver()) { heldWhSlot = -1; ev.Use(); }
            return;
        }
        if (!inventoryWindow.Contains(ev.mousePosition)) PlaceBagOutsideForIndependentRuntime(ev);
    }
    private static void DropBagReferenceForIndependentRuntime()
    {
        var clear = clearHeldForIndependentRuntime;
        heldSlot = -1; clearHeldForIndependentRuntime = null;
        clear?.Invoke();
    }
    internal static int WarehouseHeldCountForIndependentRuntime => heldWhCount;
}
}

using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    // Official 0x06000522. The original component publishes itself as the static instance.
    private void Awake()
    {
        I = this;
    }

    // Official 0x06000533 static backing field, initialized with the other official
    // tab button assets: medicinebtn, warebtn, equipbtn, specialbtn.
    internal static string[] TabAssets;

    // Official 0x06000533. Members already restored as field initializers keep their official
    // values and are not re-assigned here: Items is four List<Item> instances, ItemFocusBkColor
    // is the .rodata literal at 0x1128020 = (0.92, 0.12, 0.10, 0.55), heldSlot / heldWhSlot /
    // heldBarPet start at -1 and _lockReqName at "". This initializer restores the remaining
    // official defaults in the original order (heldName, MaxWeight, TabAssets, splitText).
    static InventoryDlg()
    {
        heldName = "";
        MaxWeight = 100;
        TabAssets = new string[4] { "medicinebtn", "warebtn", "equipbtn", "specialbtn" };
        splitText = "";
    }
}
}

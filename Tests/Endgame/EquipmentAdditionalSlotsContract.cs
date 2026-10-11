// Executes the production EquipDlg.OnGUI.cs with deterministic drawing/network substitutes.
using System;
using System.Collections.Generic;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

class EquipmentAdditionalSlotsContract
{
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static EquipDlg.EquippedItem Item(int slot) => new EquipDlg.EquippedItem
        { slot = slot, id = slot + 1000, name = "装备" + slot, desc = "原生属性", icon = new Texture2D { ID = slot } };
    static void Main()
    {
        var dialog = new EquipDlg(); NetClient.I = new NetClient();
        EquipDlg.equipped.Add(Item(2)); EquipDlg.equipped.Add(Item(13));
        dialog.Frame(100, 223, 0);
        Check(!dialog.AdditionalOpen, "no unused additional control when no higher slots are worn");
        Check(ItemIconUI.Drawn.Contains(2) && ItemIconUI.Drawn.Contains(13), "original native head and two-hand alias remain");
        for (int slot = 14; slot <= 25; slot++) EquipDlg.equipped.Add(Item(slot));
        dialog.Frame(100, 223, 0);
        Check(dialog.AdditionalOpen, "character-window control opens higher worn slots");
        for (int slot = 14; slot <= 25; slot++)
        {
            int index = slot - 14;
            NetClient.I.LastUnequipped = 0;
            dialog.Frame(84 + index % 3 * 36, 82 + index / 3 * 34, 1);
            Check(NetClient.I.LastUnequipped == slot, "native unequip route for slot " + slot);
            Check(UITip.Last.Contains("装备" + slot) && UITip.Last.Contains("原生属性"), "native tooltip for slot " + slot);
            Check(ItemIconUI.Drawn.Contains(slot), "native icon for slot " + slot);
        }
        Check(EquipDlg.equipped.Count == 14, "no optimistic local equipment mutation");
        dialog.Frame(100, 223, 1);
        Check(dialog.AdditionalOpen, "right-click does not accidentally toggle control");
        dialog.Frame(23, 257, 1);
        Check(NetClient.I.LastUnequipped == 13, "two-hand native slot acts while additional gear is open");
        AttrWashDlg.Picking = true;
        dialog.Frame(156, 184, 0);
        Check(AttrWashDlg.Target == 25, "higher slot uses native wash picker");
        AttrWashDlg.Picking = false; ToolTarget.Active = true;
        dialog.Frame(84, 82, 0);
        Check(ToolTarget.Target == 14, "higher slot uses native tool picker");
        ToolTarget.Active = false; RepairDlg.Picking = true;
        dialog.Frame(120, 82, 0);
        Check(RepairDlg.Target == 15, "higher slot uses native repair picker");
        RepairDlg.Picking = false; InventoryDlg.Sharing = true;
        dialog.Frame(156, 82, 0);
        Check(InventoryDlg.SharedSlot == 16, "higher slot uses native chat sharing");
        InventoryDlg.Sharing = false; AttrWashDlg.Over = true; NetClient.I.LastUnequipped = 0;
        dialog.Frame(84, 82, 1);
        Check(NetClient.I.LastUnequipped == 0, "overlapping native wash window blocks extra slot action");
        AttrWashDlg.Over = false;
        dialog.Tab = 1; dialog.Frame(84, 82, 1);
        Check(NetClient.I.LastUnequipped == 0 && !ItemIconUI.Drawn.Contains(14), "attribute page does not expose hidden slot actions");
        dialog.Tab = 0; dialog.Frame(100, 223, 0);
        Check(!dialog.AdditionalOpen && !ItemIconUI.Drawn.Contains(14), "return control restores doll area");
        dialog.Frame(84, 82, 1);
        Check(NetClient.I.LastUnequipped == 0, "collapsed slots cannot be clicked");
        dialog.Frame(100, 223, 0); dialog.Frame(235, 23, 0);
        Check(!dialog.Open, "native close hides expanded window");
        dialog.Open = true; dialog.Frame(84, 82, 1);
        Check(NetClient.I.LastUnequipped == 14, "reopening retains working native high-slot controls");
        EquipDlg.equipped.RemoveAll(item => item.slot >= 14);
        dialog.Frame(0, 0, -1);
        Check(!dialog.AdditionalOpen, "removing final high slot returns to doll automatically");
        EquipDlg.equipped.Add(Item(25)); EquipDlg.equipped.Add(Item(26));
        UIScale.Factor = 2; dialog.X = 40; dialog.Y = 60;
        dialog.Frame(240, 506, 0); NetClient.I.LastUnequipped = 0;
        dialog.Frame(352, 428, 1);
        Check(NetClient.I.LastUnequipped == 25, "scaled moved window maps slot 25 correctly");
        Check(!ItemIconUI.Drawn.Contains(26), "unsupported slots do not invent extra controls");
        NetClient.I.LastUnequipped = 0; dialog.Frame(208, 224, 1);
        Check(NetClient.I.LastUnequipped == 0, "empty additional slot does not target unrelated equipment");
        int[] original = { 2, 6, 8, 9, 4, 1, 3, 10, 7, 5, 11 };
        Check(EquipDlg.SlotPartsForIndependentRuntime.Count == original.Length, "original slot mapping length unchanged");
        for (int i = 0; i < original.Length; i++)
            Check(EquipDlg.SlotPartsForIndependentRuntime[i] == original[i], "original native slot mapping " + i);
        Console.WriteLine("PASS " + checks + " additional equipment slot contracts; actual native OnGUI, deterministic Unity/network substitutes.");
    }
}

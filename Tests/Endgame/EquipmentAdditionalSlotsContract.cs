// Executes the production EquipDlg.OnGUI.cs with deterministic drawing/network substitutes.
using System;
using System.Collections.Generic;
using System.Linq;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

class EquipmentAdditionalSlotsContract
{
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static EquipDlg.EquippedItem Item(int slot) => new EquipDlg.EquippedItem
        { slot = slot, id = slot + 1000, name = "装备" + slot, desc = "原生属性", icon = new Texture2D { ID = slot } };
    static EquipDlg MobileDialog(bool artifact=false)
    {
        MobileClientPresentation.Enabled=true;MobileClientPresentation.Scale=1;
        MobileClientPresentation.SafeAreaGui=new Rect(40,20,620,600);UIScale.Factor=1;
        Input.touches=new Touch[0];UnityEngine.Application.isMobilePlatform=false;GUI.enabled=true;GUI.ResetButton();
        AvatarFuse.Active=ToolTarget.Active=AttrWashDlg.Picking=AttrWashDlg.Over=RepairDlg.Picking=InventoryDlg.Sharing=false;
        RepairDlg.Target=AttrWashDlg.Target=ToolTarget.Target=InventoryDlg.SharedSlot=0;
        GameHud.Bindings=GameHud.Pickups=0;GameHud.BoundSlot=-1;GameHud.BoundSkill=0;
        CharData.skills=new Dictionary<int,int>{{14001,0},{14003,2}};
        EquipDlg.equipped.Clear();EquipDlg.equipped.Add(Item(artifact?11:2));
        NetClient.I=new NetClient();UITip.Last=null;
        var dialog=new EquipDlg();dialog.Frame(new Vector2(0,0),EventType.Repaint);
        return dialog;
    }
    static Vector2 SlotPoint(EquipDlg dialog,bool artifact=false)=>
        new Vector2(dialog.X+(artifact?75:25)*dialog.Scale,dialog.Y+(artifact?414:85)*dialog.Scale);
    static void Tap(EquipDlg dialog,Vector2 point)
    {dialog.Frame(point,EventType.MouseDown);dialog.Frame(point,EventType.MouseUp,.1f);}
    static void Hold(EquipDlg dialog,Vector2 point)
    {
        dialog.Frame(point,EventType.MouseDown);
        dialog.Frame(point,EventType.Repaint,.55f);
        Check(dialog.MobileActions,"Long hold opens native equipment action choices");
        dialog.Frame(point,EventType.MouseUp);
        Check(dialog.MobileActions&&Event.current.type==EventType.Used,"Hold release retains menu and is consumed before actions");
    }
    static Vector2 ActionPoint(string label)
    {return ArtworkSkin.Buttons.Single(button=>button.Item2==label).Item1.center;}
    static void ClickAction(EquipDlg dialog,string label)
    {
        Vector2 point=ActionPoint(label);Rect before=dialog.ActionRect;
        dialog.Frame(point,EventType.MouseDown);
        Check(dialog.ActionRect==before,"Pressing existing menu action preserves its gesture anchor: "+label);
        dialog.Frame(point,EventType.MouseUp);
    }
    static void MobileEquipmentActions()
    {
        var dialog=MobileDialog();Vector2 point=SlotPoint(dialog);
        Rect panel=MobileClientPresentation.Registered,safe=MobileClientPresentation.SafeAreaGui;
        Check(panel.x>=safe.x&&panel.y>=safe.y&&panel.xMax<=safe.xMax&&panel.yMax<=safe.yMax,
            "Original equipment panel including side tabs fits nonzero mobile safe insets");
        Tap(dialog,point);
        Check(dialog.MobileSlot==2&&!dialog.MobileActions&&UITip.Last.Contains("装备2")&&UITip.Last.Contains("原生属性"),
            "Mobile tap selects original native equipment tooltip");
        Check(NetClient.I.UnequipCount==0&&InventoryDlg.SharedSlot==0&&GameHud.Pickups==0,
            "Tap and synthetic release do not unequip, share or pick up a skill");
        Hold(dialog,point);
        Check(NetClient.I.UnequipCount==0&&GameHud.Bindings==0,"Hold and its release never execute an equipment command");
        ClickAction(dialog,"卸下");
        Check(NetClient.I.LastUnequipped==2&&NetClient.I.UnequipCount==1&&dialog.MobileSlot<0,
            "Explicit mobile unequip delegates once to original equipped-slot action");
        dialog.Frame(point,EventType.MouseUp);
        Check(NetClient.I.UnequipCount==1,"Repeated release cannot repeat unequip");
        Check(EquipDlg.equipped.Count==1,"Mobile unequip keeps authoritative equipment until receipt");

        dialog=MobileDialog();point=SlotPoint(dialog);Hold(dialog,point);ClickAction(dialog,"分享");
        Check(InventoryDlg.SharedSlot==2&&NetClient.I.UnequipCount==0&&dialog.MobileSlot<0,
            "Explicit mobile share passes original equipped identity without unequip");
        dialog=MobileDialog();RepairDlg.Picking=true;point=SlotPoint(dialog);Tap(dialog,point);
        Check(RepairDlg.Target==0&&UITip.Last.Contains("修理此装备"),"Tap during repair remains original picker tooltip without sending repair");
        Hold(dialog,point);
        Check(!ArtworkSkin.Buttons.Single(button=>button.Item2=="分享").Item3,
            "Repair picker disables unrelated share command");
        ClickAction(dialog,"分享");
        Check(RepairDlg.Target==0&&InventoryDlg.SharedSlot==0&&dialog.MobileActions,"Disabled share cannot trigger picker or share");
        ClickAction(dialog,"选择此装备");
        Check(RepairDlg.Target==2&&NetClient.I.UnequipCount==0&&dialog.MobileSlot<0,
            "Explicit repair selection goes through original repair picker");

        foreach(bool removed in new[]{false,true})
        {
            dialog=MobileDialog();point=SlotPoint(dialog);Hold(dialog,point);
            Vector2 action=ActionPoint("卸下");
            if(removed)EquipDlg.equipped.Clear();else{var replacement=EquipDlg.equipped[0];replacement.id++;EquipDlg.equipped[0]=replacement;}
            dialog.Frame(action,EventType.MouseDown);dialog.Frame(action,EventType.MouseUp);
            Check(NetClient.I.UnequipCount==0&&dialog.MobileSlot<0&&!dialog.MobileActions,
                "Removed or replaced equipment invalidates stale action menu: "+removed);
        }
        dialog=MobileDialog();point=SlotPoint(dialog);Hold(dialog,point);dialog.Tab=1;
        dialog.Frame(ActionPoint("卸下"),EventType.MouseDown);
        Check(dialog.MobileSlot<0&&NetClient.I.UnequipCount==0,"Tab change invalidates mobile selection before an action");

        dialog=MobileDialog(true);point=SlotPoint(dialog,true);Tap(dialog,point);
        Check(GameHud.Pickups==0&&GameHud.Bindings==0&&UITip.Last.Contains("法宝技"),
            "Artifact tap retains original skill tooltip without starting desktop drag");
        Hold(dialog,point);ClickAction(dialog,"放入快捷栏");
        Check(dialog.MobileBinding&&GameHud.Bindings==0,"Explicit artifact bind opens chooser without binding yet");
        dialog.Frame(point,EventType.Repaint);
        for(int page=0;page<7;page++){ClickAction(dialog,"下一页");dialog.Frame(point,EventType.Repaint);}
        Check(ArtworkSkin.Buttons.Any(button=>button.Item2=="8页2")&&!ArtworkSkin.Buttons.Any(button=>button.Item2=="8页3"),
            "Artifact chooser exposes exactly the existing 30 hotbar slots");
        ClickAction(dialog,"8页2");
        Check(GameHud.Bindings==1&&GameHud.BoundSlot==29&&GameHud.BoundSkill==14003&&GameHud.Pickups==0&&dialog.MobileSlot<0,
            "Explicit final-page choice binds learned artifact skill through mobile hotbar owner");

        // Exercise raw device touch as well as the editor mouse fallback above.
        dialog=MobileDialog();point=SlotPoint(dialog);UnityEngine.Application.isMobilePlatform=true;
        Input.touches=new[]{new Touch{fingerId=7,position=new Vector2(point.x,Screen.height-point.y),phase=TouchPhase.Began}};
        dialog.Frame(point,EventType.MouseDown);
        Input.touches=new[]{new Touch{fingerId=7,position=new Vector2(point.x,Screen.height-point.y),phase=TouchPhase.Ended}};
        dialog.Frame(point,EventType.MouseUp,.1f);Input.touches=new Touch[0];
        Check(dialog.MobileSlot==2&&!dialog.MobileActions&&NetClient.I.UnequipCount==0&&Event.current.type==EventType.Used,
            "Raw touch tap selects native tooltip and consumes synthetic device release");
        UnityEngine.Application.isMobilePlatform=false;MobileClientPresentation.Enabled=false;
    }
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
        Check(checks==68,"All original 68 desktop equipment checks remain intact");
        MobileEquipmentActions();
        Console.WriteLine("PASS " + checks + " native equipment slot/mobile action contracts; actual OnGUI and mobile gesture adapter, deterministic Unity/network substitutes.");
    }
}

using System;
using System.Collections.Generic;
using FakeFantasyOnline.Presentation.ReferenceAssets;

internal static class NativeInlayFurnaceContract
{
    private static int checks;
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
    public static void Main()
    {
        var native = new NetClient(); NetClient.I = native;
        int probes=0, inlays=0, drills=0; byte target=255, targetTab=255, hole=255, runeTab=255; int slot=-1, runeSlot=-1;
        native.BindFurnaceWritersForIndependentRuntime(_ => {}, (a,b) => {}, (a,b,c,d,e,f) => {},
            (a,b,c,d,e) => { if (d==0) probes++; else drills++; },
            (a,b,c,d,e,f) => { inlays++; target=a; targetTab=b; slot=c; hole=d; runeTab=e; runeSlot=f; });
        InventoryDlg.Items[2].Add(new InventoryDlg.Item { id=1001, bagIdx=3, name="原装备" });
        InventoryDlg.Items[1].Add(new InventoryDlg.Item { id=1900028001, bagIdx=7, name="角木蛟星篆" });
        SculpDlg.Open("镶嵌炉", "镶嵌");
        Check(SculpDlg.Visible && !SocketUI.HasTarget, "original furnace opens with empty native target");
        Check(!SculpDlg.TryDropRune(1,7,0) && inlays==0, "rune cannot submit before target selection");
        Check(!SculpDlg.TryDropSculp(1,7) && !SculpDlg.TryDropSculp(2,-1) && !SculpDlg.TryDropSculp(2,99) && probes==0, "wrong tab and missing equipment remain held without native query");
        Check(SculpDlg.TryDropSculp(2,3) && probes==1 && SocketUI.ItemId==1001 && SocketUI.CurLevel==-1, "native equipment drag retains target probe protocol");
        Check(!SculpDlg.TryDropRune(1,7,0), "pending target probe cannot inlay");
        SocketUI.SetInfo(1001,0,new List<int> {0,1900028002,0},100,90,true,null,null,5,"");
        NetClient.I = null;
        Check(!SculpDlg.TryDropRune(1,7,0) && !SculpDlg.TryDropSculp(2,3) && inlays==0 && probes==1, "disconnected drag keeps held items without submitting");
        NetClient.I = native;
        Check(SocketUI.FirstEmptyHole()==0 && SocketUI.CanInlayHole(0) && !SocketUI.CanInlayHole(1), "authoritative empty and occupied holes gate insertion");
        foreach(int invalid in new[] {-1,3,5,256})
        { Check(!SculpDlg.TryDropRune(1,7,invalid), "invalid/unopened hole cannot consume held item"); SocketUI.Inlay(invalid,1,7); }
        Check(!SculpDlg.TryDropRune(4,7,0) && !SculpDlg.TryDropRune(255,7,0) && !SculpDlg.TryDropRune(1,-1,0) && !SculpDlg.TryDropRune(1,99,0) && inlays==0, "invalid rune tab/slot is bounded and stale native row cannot submit");
        SocketUI.Inlay(1,1,7); SocketUI.Inlay(0,255,7); SocketUI.Inlay(0,1,-1);
        Check(inlays==0, "direct native sender rejects occupied and malformed input");
        Check(SculpDlg.TryDropRune(1,7,2) && inlays==1 && target==0 && targetTab==2 && slot==3 && hole==2 && runeTab==1 && runeSlot==7,
            "star rune uses existing equipment/rune bag positions and zero-based hole protocol");
        Check(SocketUI.Sockets[2]==0 && InventoryDlg.Items[1].Count==1, "send does not optimistically consume inventory or fabricate socket receipt");
        SocketUI.SetInfo(1001,0,new List<int> {0,1900028002,1900028001},100,90,true,null,null);
        Check(!SculpDlg.TryDropRune(1,7,2) && inlays==1, "authoritative receipt prevents duplicate insertion into occupied hole");
        SocketUI.Drill(); Check(drills==1, "original drilling command remains available");
        InventoryDlg.Items[2].Add(new InventoryDlg.Item { id=1002, bagIdx=4, name="新装备" });
        Check(SculpDlg.TryDropSculp(2,4) && probes==2 && !SocketUI.CanInlayHole(0), "switching equipment retires old holes until new native receipt");
        SculpDlg.Open("镶嵌炉", "镶嵌");
        Check(!SocketUI.HasTarget && !SocketUI.CanInlayHole(0), "reopening resets target and old socket state");
        Console.WriteLine("PASS " + checks + " original inlay furnace contracts; real state, drag handlers and native writers.");
    }
}

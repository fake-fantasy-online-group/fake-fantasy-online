using System;
using System.Reflection;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;
partial class EndgameUiContract
{
    static readonly string[] SpiritKeys = { "shoering", "glovesoul", "wingsoul", "ClothSoul", "neckring", "facesoul", "capsoul", "HonorSoul", "WeaponSoul" };
    static string SpiritDefinitionsJson()
    {
        var rows = new string[9];
        for (int i = 0; i < rows.Length; i++) rows[i] = "{\"kind\":" + (i+1) + ",\"name\":\"灵"+i+"\",\"itemId\":"+(24000+i)+",\"essenceId\":"+(35000+i)+",\"separatorId\":"+(14000+i)+",\"npcId\":"+(160+i)+",\"mapId\":7,\"npcName\":\"仙子"+i+"\",\"npcScript\":\"Nspirit"+i+"\",\"slot\":1,\"uiKey\":\""+SpiritKeys[i]+"\",\"processMoney\":\"100000\"}";
        return string.Join(",", rows);
    }
    static void SpiritSetup(Client c, EndgamePanel p)
    {
        c.Endgame.Reset(); p.Bind(c); c.EndgameInWorld = true; GameBootstrap.CurMapId = 7;
        InventoryDlg.rows.Clear(); InventoryDlg.rowTabs.Clear(); InventoryDlg.locked.Clear(); EquipDlg.Equipped.Clear();
        SysMsgBox.Visible = false;
        c.Endgame.Begin(new EndgameRequest("hello"));
        Apply(c,"{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.spirit.v1\"],\"spirits\":["+SpiritDefinitionsJson()+"],\"spiritAppearanceIds\":[49000]}");
        for(int s=0;s<4;s++) InventoryDlg.rows[s]=new InventoryDlg.Item { id=s==0?24000:s==1?35000:s==2?1001:14000, bagIdx=s,count=200,name="fixture" };
        EquipDlg.Equipped.Add(new EquipDlg.EquippedItem {id=1002,slot=1,name="worn"});
        Apply(c,"{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"spirit-fixture\",\"chunk\":0,\"chunks\":1,\"revision\":\"1\",\"items\":[{\"uid\":\"10\",\"itemId\":24000,\"location\":\"bag\",\"tab\":2,\"slot\":0},{\"uid\":\"20\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":2},{\"uid\":\"21\",\"itemId\":1002,\"location\":\"equipped\",\"slot\":1}]}");
    }
    static string SpiritPanelView = "{\"kind\":1,\"itemId\":24000,\"sealed\":false,\"level\":15,\"attrs\":[{\"attr\":1,\"value\":300,\"permanent\":300,\"last\":20,\"level\":15,\"perfectLevel\":15},{\"attr\":2,\"value\":10,\"permanent\":0,\"last\":10,\"level\":1,\"perfectLevel\":0}],\"canUnseal\":false,\"canProcess\":true,\"canRollback\":true,\"canReset\":true,\"unsealEssence\":2,\"processEssence\":5,\"rollbackEssence\":1,\"resetEssence\":200,\"processMoney\":\"100000\"}";
    static void SpiritPanelContracts(Client c, EndgamePanel p)
    {
        SpiritSetup(c,p);
        Check(c.Endgame.Spirits.Count==9,"nine negotiated NPC definitions");
        Check(EndgamePanel.SpiritNpcDefinition(7,"戒灵仙子","Njieling")==null,"ring NPC remains outside new implementation");
        for(int i=0;i<9;i++) foreach(string mode in new[]{"unseal","process","reset"})
        {
            Check(EndgamePanel.TryOpenSpiritFromNpc(7,100+i,"仙子"+i,"Nspirit"+i,mode,()=>true),"nine NPCs open all three original windows");
            object layout=typeof(EndgamePanel).GetMethod("SpiritArtwork",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,null);
            var t=layout.GetType(); int w=(int)t.GetField("Width").GetValue(layout), h=(int)t.GetField("Height").GetValue(layout);
            foreach(string field in mode=="process"?new[]{"Spirit","Essence","Stats","Action","Back","Clear","CloseRect"}:new[]{"Spirit","Essence","Stats","Action","Clear","CloseRect"})
            {
                Rect r=(Rect)t.GetField(field).GetValue(layout);
                Check(r.x>=0&&r.y>=0&&r.x+r.width<=w&&r.y+r.height<=h,"original layout bounds "+i+" "+mode+" "+field);
            }
            Check(((string)t.GetField("Background").GetValue(layout)).StartsWith("res/uires/"),"original packaged artwork only");
            Call(p,"Close");
        }
        Check(!EndgamePanel.TryOpenSpiritFromNpc(7,1,"仙子0","Nspirit0","process",()=>false),"departed NPC blocked");
        Check(!EndgamePanel.TryOpenSpiritFromNpc(7,1,"仙子0","Nspirit0","rollback",()=>true),"rollback is not independent window");
        Check(EndgamePanel.TryOpenSpiritFromNpc(7,1,"仙子0","Nspirit0","process",()=>true),"process window opens");
        Check(EndgamePanel.TryPutSpiritBagItem(2,1),"essence reservation accepts whole stack without quantity dialog");
        Check(EndgamePanel.TryPutSpiritBagItem(2,0)&&c.Last.Op=="spirit.preview","spirit reservation previews exact bag slot");
        var preview=c.Last;
        Apply(c,"{\"kind\":\"spirit.preview\",\"ok\":true,\"requestId\":\""+preview.RequestID+"\",\"revision\":\"1\",\"spirit\":"+SpiritPanelView+"}");
        typeof(EndgamePanel).GetMethod("SubmitSpirit",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,new object[]{"process"});
        Check(c.Last.Op=="spirit.process"&&c.Endgame.Pending,"authoritative CanProcess permits overall level15 with other incomplete row");
        Check(c.Last.Json.Contains("\"spiritMaterial\"")&&!c.Last.Json.Contains("\"count\""),"mutation uses two selected identities not client material count");
        var request=c.Last; Call(p,"Close");
        Apply(c,"{\"kind\":\"spirit.result\",\"ok\":true,\"requestId\":\""+request.RequestID+"\",\"revision\":\"2\",\"spirit\":"+SpiritPanelView+"}");
        Check(EndgamePanel.TryOpenSpiritFromNpc(7,1,"仙子0","Nspirit0","process",()=>true)&&Get(p,"spiritSelection")==null,"reopen does not reuse previous reservation");
        Call(p,"Close");
        foreach(byte kind in new byte[]{4,5,6}) foreach(string location in new[]{"bag","equipped"})
        {
            SpiritSetup(c,p); int sourceSlot=kind==4?0:3, id=kind==4?24000:kind==5?14000:49000;
            if(kind==6) InventoryDlg.rows[3]=new InventoryDlg.Item{id=id,count=1,bagIdx=3,name="appearance"};
            Check(EndgamePanel.HandleSpiritItemForIndependentRuntime(id,2,sourceSlot)&&AvatarFuse.BegunKind==kind,"right click selects matching spirit tool mode");
            Check(EndgamePanel.ApplySpiritItem(2,sourceSlot,location,2,location=="bag"?2:1,kind),"spirit tool applies to bag or worn identity");
            Check(c.Last.Op==(kind==4?"spirit.fuse":kind==5?"spirit.separate":"spirit.appearance"),"target request operation matches tool");
        }
        SpiritSetup(c,p);
        EndgamePanel.HandleSpiritItemForIndependentRuntime(24000,2,0);
        InventoryDlg.rows[0]=new InventoryDlg.Item{id=24001,count=1,bagIdx=0};
        Check(!EndgamePanel.ApplySpiritItem(2,0,"bag",2,2,4),"replaced source slot cannot consume another spirit");
        SpiritSetup(c,p); EndgamePanel.HandleSpiritItemForIndependentRuntime(24000,2,0);
        Check(!EndgamePanel.ApplySpiritItem(2,0,"bag",2,2,6),"changed tool mode cannot turn fusion into appearance mutation");
        SpiritSetup(c,p); EndgamePanel.HandleSpiritItemForIndependentRuntime(24000,2,0);
        Check(!EndgamePanel.ApplySpiritItem(2,0,"bag",2,0,4),"spirit cannot target its own source slot");
        SpiritSetup(c,p); EndgamePanel.HandleSpiritItemForIndependentRuntime(24000,2,0); c.Endgame.NoteNativeInventoryPublished();
        Check(!EndgamePanel.ApplySpiritItem(2,0,"equipped",2,1,4),"stale native inventory blocks worn fusion");
        c.Endgame.Reset();
        Check(!EndgamePanel.HandleSpiritItemForIndependentRuntime(24000,2,0),"legacy capability does not hijack ordinary item use");
    }
}

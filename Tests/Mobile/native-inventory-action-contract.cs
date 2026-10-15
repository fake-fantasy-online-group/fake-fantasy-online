using System;
using System.Collections.Generic;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

// Rendering, transport and picker destinations are fixtures. ExecuteMobileAction,
// SlotPickers/RightButton/LeftButton/Activate, trade admission, lock/share, receipt
// invalidation and split opening/commit are compiled from the production source.
namespace UnityEngine
{
    public struct Color { public Color(float r, float g, float b, float a) { } }
    public class Texture2D { }
    public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
    public struct Rect { public Vector2 center => new Vector2(400,300); }
    public enum EventType { MouseDown, MouseUp, Used }
    [Flags] public enum EventModifiers { None=0, Shift=1, Control=2, Command=4 }
    public enum KeyCode { LeftControl,RightControl,LeftCommand,RightCommand,LeftShift,RightShift }
    public class Event
    {
        public EventType type; public int button,clickCount; public Vector2 mousePosition;
        public EventModifiers modifiers; public bool shift => (modifiers & EventModifiers.Shift)!=0;
        public void Use(){type=EventType.Used;}
    }
    public static class Input { public static bool GetKey(KeyCode key)=>false; }
    public static class Screen { public static int width=800,height=600; }
    public static class Mathf
    {
        public static int Clamp(int n,int a,int b)=>Math.Min(b,Math.Max(a,n));
        public static float Clamp(float n,float a,float b)=>Math.Min(b,Math.Max(a,n));
    }
}
namespace FakeFantasyOnline.Application
{
    public struct PetBrief { public int opened; public string nickname,species; }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal static class Probe
    {
        public static List<string> Calls=new List<string>(); public static List<string> Messages=new List<string>();
        public static void Call(string text){Calls.Add(text);}
    }
    internal partial class InventoryDlg
    {
        internal struct Item
        {
            public int id,count,bagIdx,quality,sell,avatar,slot; public bool locked,bound,cosmetic;
            public string name,desc,fusedDesc; public Texture2D icon; public FakeFantasyOnline.Application.PetBrief? pet;
        }
        internal static InventoryDlg I; internal static float _lockReqAt;
        internal static List<Item>[] Items={new List<Item>(),new List<Item>(),new List<Item>(),new List<Item>()};
        internal static int heldSlot=-1,heldWhSlot=-1,heldBarPet=-1;
        internal static bool HasHeldAny=>heldSlot>=0||heldWhSlot>=0||heldBarPet>=0;
        internal int tab,mobileSlot=-1,mobileId,mobileTab=-1,clears;
        private float s=1;
        internal static bool splitOpen,splitDrag; private static byte splitZone,splitTab,splitWhTab=0;
        private static int splitSlot,splitMax,splitVal,splitItemId; private static string splitText;
        private static Texture2D splitIcon; private static Vector2 splitWin; private const int SplitMaxCap=999;
        internal static Action<int,int> OnUseItem;
        internal static bool LockModHeld(Event ev)=>ev.shift;
        internal static bool ShiftHeld(Event ev)=>ev.shift;
        internal static bool WarnIfLocked(int tab,int slot,string action)=>ItemAt(tab,slot).locked;
        internal static void LockReqSent(string name){Probe.Call("lock-request:"+name);}
        private void ClearMobileSelection(){mobileSlot=-1;mobileId=0;clears++;}
        internal void Select(int bagTab,Item item){tab=bagTab;mobileTab=bagTab;mobileSlot=item.bagIdx;mobileId=item.id;}
        internal void Execute(Item item,int action){ExecuteMobileAction(item,action);}
        internal string Label(Item item)=>MobilePrimaryLabel(item);
        internal static void CloseSplit(){splitOpen=false;splitDrag=false;splitIcon=null;}
        internal static void Commit(){CommitSplit();}
        internal static int SplitMaximum=>splitMax;
        internal static void PickUpBagForIndependentRuntime(Event ev,byte tab,Item item,object unused){Probe.Call("pickup");}
        internal static void EquipHeld(Item item){Probe.Call("equip:"+item.bagIdx);}
    }
    internal static class MobileClientPresentation { public static Rect SafeAreaGui; }
    internal static class EquipFuseDlg
    { public static bool Picking; public static void SetEquip(byte tab,int slot,int id,int quality,string name,Texture2D icon){Probe.Call("fuse:"+slot);} }
    internal static class ToolTarget
    { public static bool Active; public static int ToolId; public static void PickBag(byte tab,int slot,string name,bool bound,bool avatar){Probe.Call("tool:"+slot);} public static void Cancel(){Probe.Call("tool-cancel");} }
    internal static class PetForgeDlg
    { public static bool Picking; public static void SetPet(byte tab,int slot,FakeFantasyOnline.Application.PetBrief pet,Texture2D icon){Probe.Call("forge-pet:"+slot);} public static void SetItem(int kind,int tab,int slot,int id,string name,Texture2D icon){Probe.Call("forge-item:"+slot);} }
    internal static class PetForgeIds
    { public static bool IsTalisman(int id)=>id==9001; public static bool IsBook(int id)=>id==9002; public static bool IsPot(int id)=>id==9003; }
    internal static class AttrWashDlg
    { public static bool Picking; public static void SetTarget(int source,byte tab,int slot,string name,Texture2D icon){Probe.Call("wash:"+slot);} }
    internal static class AvatarFuse
    { public static bool Active; public static void PickTargetBag(byte tab,int slot,string name,bool avatar){Probe.Call("avatar:"+slot);} public static void Cancel(){Probe.Call("avatar-cancel");} }
    internal static class RepairDlg
    { public static bool Picking; public static void PickBag(byte tab,int slot){Probe.Call("repair:"+tab+":"+slot);} }
    internal static class EndgamePanel
    { public static bool CubeContainerOpen,CubeAccepts=true; public static bool TryPutCubeBagItem(int tab,int slot){Probe.Call("cube:"+tab+":"+slot);return CubeAccepts;} }
    internal static class StoreDlg { public static bool Visible; public static byte CurTab=3; }
    internal static class Win05 { public static bool Wardrobe; public static bool IsShowing(string id)=>Wardrobe; }
    internal static class StallConfigDlg { public static bool Active,IsSell=true; }
    internal static class StallDetailDlg
    { public static void Open(byte tab,int slot,int id,int count,string name,Texture2D icon,int sell,int quality,string desc){Probe.Call("stall:"+count);} }
    internal static class SocialClient { public static bool TradeActive,MyLocked,OtherLocked,MyConfirmed,OtherConfirmed; public static int OtherId=7; }
    internal sealed partial class DealDlg
    {
        internal static DealDlg I; internal bool visible; public static bool Visible=>I!=null&&I.visible;
        internal List<(byte,int,int)> myOffer=new List<(byte,int,int)>(); internal string LastFusedMessageForIndependentRuntime;
        private void SendOffer(){Probe.Call("offer");}
        internal void NextSession(){offerSession++;myOffer.Clear();}
    }
    internal class SysMsgBox
    {
        internal static SysMsgBox I=new SysMsgBox(); internal static bool Visible; internal static Action Yes;
        internal string MessageForIndependentRuntime;
        internal static void Show(string message,Action yes,Action no){I.MessageForIndependentRuntime=message;Visible=true;Yes=yes;}
        internal static void Confirm(){var yes=Yes;Yes=null;Visible=false;yes?.Invoke();}
    }
    internal static class AccountSec
    { internal static bool NeedSecCodeForUnlock; internal static void NoteUnlockTarget(byte tab,int slot,string name){Probe.Call("unlock-target:"+slot);} }
    internal static class SecCodeAskDlg
    { internal static Action<string> Submit; internal static void Open(string name,string message,Action<string> callback){Submit=callback;} }
    internal class NetClient
    {
        internal static NetClient I;
        internal void UseItem(int id,byte tab,int slot){Probe.Call("use:"+id+":"+tab+":"+slot);}
        internal void PetHatchForIndependentRuntime(int slot){Probe.Call("hatch:"+slot);}
        internal void PetDeployForIndependentRuntime(int slot){Probe.Call("deploy:"+slot);}
        internal void WarehouseMoveForIndependentRuntime(int direction,byte tab,int slot,byte to,int count,int target){Probe.Call("warehouse:"+tab+":"+slot+":"+count);}
        internal void WardrobeStoreForIndependentRuntime(byte tab,int slot){Probe.Call("wardrobe:"+tab+":"+slot);}
        internal void SetItemLockForIndependentRuntime(byte tab,int slot,bool state,string code,int id){Probe.Call("lock:"+tab+":"+slot+":"+state+":"+id);}
        internal void WarehouseSplitForIndependentRuntime(byte tab,int slot,int count){Probe.Call("warehouse-split:"+count);}
        internal void SplitItemForIndependentRuntime(byte tab,int slot,int count){Probe.Call("split:"+tab+":"+slot+":"+count);}
    }
    internal static class ItemActions
    {
        internal static bool Handles;
        internal static bool Handle(int id,byte tab,int slot,bool bound,bool cosmetic,string name,Action equip)
        {if(tab==2&&equip!=null){equip();return true;}return Handles;}
    }
    internal static class GameHud
    { internal static float Cooldown; internal static float CdFrac(int id)=>Cooldown; internal static void StartItemCd(int id){Probe.Call("cooldown:"+id);} }
    internal static class ChatUI
    {
        public static void Add(string channel,string text,Color colour){Probe.Messages.Add(text);}
        public static void InsertShare(byte kind,byte tab,int slot,string name){Probe.Call("share-chat:"+kind+":"+tab+":"+slot+":"+name);}
    }
    internal static class HornDialog
    { internal static bool InputFocused; internal static bool InsertShare(byte kind,byte tab,int slot,string name){Probe.Call("share-horn:"+name);return false;} }
    internal static class GossipDlg
    { internal static bool InputFocused; internal static bool InsertShare(byte kind,byte tab,int slot,string name){Probe.Call("share-gossip:"+name);return false;} }
}
internal static class NativeInventoryActionContract
{
    static int checks; static InventoryDlg inventory; static InventoryDlg.Item item;
    static void Check(bool result,string description)
    { if(!result)throw new Exception(description+"; calls="+string.Join(",",Probe.Calls));checks++; }
    static bool Has(string call)=>Probe.Calls.Contains(call);
    static void Only(string call,string description)=>Check(Probe.Calls.Count==1&&Has(call),description);
    static void Idle(string description)=>Check(Probe.Calls.Count==0,description);
    static void Reset(int tab=1)
    {
        inventory=new InventoryDlg(); InventoryDlg.I=inventory;
        foreach(var bag in InventoryDlg.Items)bag.Clear();
        InventoryDlg.heldSlot=InventoryDlg.heldWhSlot=InventoryDlg.heldBarPet=-1;
        InventoryDlg.CloseSplit(); InventoryDlg.OnUseItem=null;
        item=new InventoryDlg.Item{id=123,count=6,bagIdx=44,name="live item",slot=4,quality=2,sell=10,avatar=0,cosmetic=false,icon=null,desc="description"};
        InventoryDlg.Items[tab].Add(item);inventory.Select(tab,item);
        DealDlg.I=new DealDlg(); SocialClient.TradeActive=SocialClient.MyLocked=SocialClient.OtherLocked=SocialClient.MyConfirmed=SocialClient.OtherConfirmed=false;
        SocialClient.OtherId=7;
        EquipFuseDlg.Picking=ToolTarget.Active=PetForgeDlg.Picking=AttrWashDlg.Picking=AvatarFuse.Active=RepairDlg.Picking=false;
        ToolTarget.ToolId=0;
        EndgamePanel.CubeContainerOpen=StoreDlg.Visible=Win05.Wardrobe=StallConfigDlg.Active=false;
        EndgamePanel.CubeAccepts=StallConfigDlg.IsSell=true;
        SysMsgBox.Visible=false;SysMsgBox.Yes=null;SecCodeAskDlg.Submit=null;AccountSec.NeedSecCodeForUnlock=false;
        NetClient.I=new NetClient();ItemActions.Handles=false;GameHud.Cooldown=0;
        HornDialog.InputFocused=GossipDlg.InputFocused=false;
        Probe.Calls.Clear();Probe.Messages.Clear();
    }
    static void Replace(InventoryDlg.Item value){InventoryDlg.Items[inventory.tab][0]=value;}
    static void Trade(){DealDlg.I.visible=true;SocialClient.TradeActive=true;}
    static void Execute(int action=0){inventory.Execute(item,action);}
    static void Main()
    {
        Reset();Execute();Check(Has("use:123:1:44")&&Has("cooldown:123")&&inventory.mobileSlot==-1,"primary uses live original activation and clears selection");
        Probe.Calls.Clear();Execute();Idle("repeated cached action is inert after selection clears");
        Reset(2);Execute();Only("equip:44","equipment primary retains original equipment handler");
        Reset();GameHud.Cooldown=1;Execute();Idle("original cooldown prevents primary use");
        Reset();ItemActions.Handles=true;Execute();Idle("handled item never falls through to ordinary UseItem");
        Reset();NetClient.I=null;InventoryDlg.OnUseItem=(tab,slot)=>Probe.Call("offline:"+tab+":"+slot);Execute();Only("offline:1:44","offline primary preserves selected grid slot");
        Reset();var live=item;live.id=124;Replace(live);Execute();Idle("replaced item id invalidates cached action");
        Reset();inventory.mobileId=999;Execute();Idle("selection id mismatch rejects action");
        Reset();inventory.mobileSlot=-1;Execute();Idle("negative selection slot rejects action");
        Reset();inventory.mobileTab=2;Execute();Idle("changed tab rejects even matching item identity");
        Reset();InventoryDlg.Items[1].Clear();inventory.mobileId=0;item=default;Execute();Idle("empty live item cannot dispatch with zero cached identities");
        Reset();InventoryDlg.Items[1].Clear();Execute();Idle("removed item rejects action");
        Reset();InventoryDlg.SetItems(1,new List<InventoryDlg.Item>{item});Execute();Idle("identical replacement receipt still invalidates selection");
        Reset();InventoryDlg.PatchSlot(1,44,item.id,item);Execute();Idle("same-identity delta receipt invalidates selection");
        for(int held=0;held<4;held++)
        { Reset();if(held==0)InventoryDlg.heldSlot=3;if(held==1)InventoryDlg.heldWhSlot=3;if(held==2)InventoryDlg.heldBarPet=3;if(held==3)InventoryDlg.splitOpen=true;
          for(int action=0;action<4;action++)Execute(action);Idle("held bag/warehouse/pet and open split reject all actions "+held); }

        Reset();Trade();live=item;live.count=2;Replace(live);Execute();Only("offer","trade primary does not activate item");Check(DealDlg.I.myOffer[0].Item3==2,"trade uses live stack count instead of tooltip snapshot");
        Reset();Trade();EndgamePanel.CubeContainerOpen=true;Execute();Only("offer","trade takes primary before cube deposit");
        Reset();Trade();DealDlg.I.myOffer.Add((1,44,6));Execute();Only("offer","offered primary withdraws once");Check(DealDlg.I.myOffer.Count==0,"withdraw removes original offer row");
        for(int action=1;action<4;action++)
        {Reset();Trade();DealDlg.I.myOffer.Add((1,44,6));RepairDlg.Picking=true;Execute(action);Idle("offered row blocks secondary action "+action);Check(DealDlg.I.myOffer.Count==1,"secondary never withdraws offer "+action);}
        for(int state=0;state<4;state++)
        {Reset();Trade();if(state==0)SocialClient.MyLocked=true;if(state==1)SocialClient.MyConfirmed=true;if(state==2)SocialClient.OtherConfirmed=true;if(state==3)SocialClient.TradeActive=false;
         Execute();Idle("locked/finalized/ended trade primary cannot fall through to use "+state);}
        Reset();Trade();SocialClient.OtherLocked=true;Execute();Only("offer","other-side lock alone preserves editable-side native admission");
        Reset();Trade();live=item;live.locked=true;Replace(live);Execute();Idle("live locked trade item is rejected");
        Reset();Trade();live=item;live.bound=true;Replace(live);Execute();Idle("live bound trade item is rejected");
        Reset();SocialClient.TradeActive=true;Execute();Idle("active trade without visible panel still blocks original use");Check(Probe.Messages.Count==1,"active trade warning retained");
        Reset();Trade();live=item;live.fusedDesc="fused";Replace(live);Execute();Idle("fused trade waits for original confirmation");Check(SysMsgBox.Visible,"fused confirmation remains visible");SysMsgBox.Confirm();Only("offer","confirmed fused trade adds once");SysMsgBox.Confirm();Only("offer","repeated confirmation cannot add twice");
        Reset();Trade();live=item;live.fusedDesc="fused";Replace(live);Execute();InventoryDlg.SetItems(1,new List<InventoryDlg.Item>{live});SysMsgBox.Confirm();Idle("receipt invalidates pending fused offer");

        Reset();EndgamePanel.CubeContainerOpen=true;StoreDlg.Visible=true;Win05.Wardrobe=true;Execute();Only("cube:1:44","cube takes original right-click precedence over warehouse/wardrobe");
        Reset();EndgamePanel.CubeContainerOpen=true;EndgamePanel.CubeAccepts=false;Execute();Only("cube:1:44","rejected cube deposit never falls through to item activation");
        Reset();StoreDlg.Visible=true;live=item;live.count=3;Replace(live);Execute();Only("warehouse:1:44:3","warehouse uses live full count");
        Reset();Win05.Wardrobe=true;Execute();Idle("wardrobe primary waits for confirmation");Check(SysMsgBox.Visible,"wardrobe confirmation displayed");SysMsgBox.Confirm();Only("wardrobe:1:44","wardrobe confirmation uses original callback");
        Reset();item.pet=new FakeFantasyOnline.Application.PetBrief{opened=0,species="egg"};Replace(item);EndgamePanel.CubeContainerOpen=true;StoreDlg.Visible=true;Win05.Wardrobe=true;Execute();Only("hatch:44","egg skips ordinary deposits and hatches");
        Reset();item.pet=new FakeFantasyOnline.Application.PetBrief{opened=1,species="pet"};Replace(item);Execute();Only("deploy:44","opened pet deploys through original handler");
        Reset();StallConfigDlg.Active=true;Execute();Only("stall:6","mobile primary retains sell-stall branch");
        Reset();StallConfigDlg.Active=true;StallConfigDlg.IsSell=false;Execute();Only("stall:9999","buy stall retains native sentinel");
        Reset();StallConfigDlg.Active=true;live=item;live.locked=true;Replace(live);Execute();Idle("locked live stall item cannot be offered or used");Check(Probe.Messages.Count==1,"locked stall warning retained");

        for(int action=0;action<4;action++)
        {Reset(2);Trade();EndgamePanel.CubeContainerOpen=true;RepairDlg.Picking=true;Execute(action);Only("repair:2:44","repair picker gets first refusal for action "+action);}
        Reset(2);RepairDlg.Picking=EquipFuseDlg.Picking=true;Execute();Only("fuse:44","earlier equip fuse picker retains priority over repair");
        Reset(2);RepairDlg.Picking=ToolTarget.Active=true;Execute();Only("tool:44","tool picker retains priority over repair");
        Reset(2);RepairDlg.Picking=AttrWashDlg.Picking=true;Execute();Only("wash:44","attribute wash retains priority over repair");
        Reset(2);RepairDlg.Picking=AvatarFuse.Active=true;Execute();Only("avatar:44","avatar fuse retains priority over repair");
        Reset();RepairDlg.Picking=true;Execute();Idle("repair on non-equipment tab cannot activate incompatible item");Check(inventory.mobileSlot==-1,"incompatible picker dismisses selection");
        Reset(2);RepairDlg.Picking=true;item.pet=new FakeFantasyOnline.Application.PetBrief{opened=1,species="pet"};Replace(item);Execute();Idle("repair never deploys incompatible pet row");
        for(int picker=0;picker<5;picker++)
        {Reset();if(picker==0)EquipFuseDlg.Picking=true;if(picker==1)ToolTarget.Active=true;if(picker==2)PetForgeDlg.Picking=true;if(picker==3)AttrWashDlg.Picking=true;if(picker==4)AvatarFuse.Active=true;
         EndgamePanel.CubeContainerOpen=true;Trade();Execute();Idle("incompatible picker blocks trade/cube/use primary "+picker);}
        Reset(2);EquipFuseDlg.Picking=true;item.cosmetic=true;Replace(item);Execute();Idle("cosmetic equipment rejected by fuse cannot equip instead");
        Reset(2);PetForgeDlg.Picking=true;item.pet=new FakeFantasyOnline.Application.PetBrief{opened=0,species="egg"};Replace(item);Execute();Idle("forge rejects egg without hatching it");
        Reset();RepairDlg.Picking=true;Execute(3);Only("share-chat:0:1:44:live item","incompatible picker guard is limited to primary action");
        Reset(2);PetForgeDlg.Picking=true;item.pet=new FakeFantasyOnline.Application.PetBrief{opened=1,species="pet"};Replace(item);Execute();Only("forge-pet:44","opened pet enters forge picker");

        Reset();Execute(1);Check(Has("lock:1:44:True:123")&&!Has("use:123:1:44"),"lock action uses original lock request");
        Reset();live=item;live.locked=true;Replace(live);Execute(1);Check(SysMsgBox.Visible&&!Has("lock:1:44:False:123"),"unlock uses live lock state and original confirmation");SysMsgBox.Confirm();Check(Has("lock:1:44:False:123"),"confirmed unlock sends original item identity");
        Reset();live=item;live.locked=true;Replace(live);AccountSec.NeedSecCodeForUnlock=true;Execute(1);Check(SecCodeAskDlg.Submit!=null&&!Has("lock:1:44:False:123"),"security-code unlock stays behind original prompt");SecCodeAskDlg.Submit("fixture-code");Check(Has("lock:1:44:False:123"),"security-code callback retains unlock dispatch");
        Reset();NetClient.I=null;Execute(1);Idle("offline lock is never dispatched");Check(Probe.Messages.Count==1,"offline lock original warning retained");
        Reset();live=item;live.count=4;Replace(live);Execute(2);Idle("split opens without premature packet");Check(InventoryDlg.splitOpen&&InventoryDlg.SplitMaximum==3,"split uses live count and keeps one item");InventoryDlg.Commit();Only("split:1:44:2","split confirmation dispatches original split amount");
        Reset();live=item;live.count=5000;Replace(live);Execute(2);Check(InventoryDlg.SplitMaximum==999,"large stacks retain native split cap");
        Reset();live=item;live.count=1;Replace(live);Execute(2);Idle("shrunk one-item stack cannot split or pick up");Check(!InventoryDlg.splitOpen,"ineligible split remains closed");
        Reset();item.pet=new FakeFantasyOnline.Application.PetBrief{opened=1,species="pet"};Replace(item);Execute(2);Idle("pet cannot split");
        Reset();Execute(3);Only("share-chat:0:1:44:live item","share retains original bag identity");
        Reset();live=item;live.name="updated name";Replace(live);Execute(3);Only("share-chat:0:1:44:updated name","share uses live metadata");
        Reset();item.pet=new FakeFantasyOnline.Application.PetBrief{opened=1,species="pet",nickname="nickname"};Replace(item);Execute(3);Only("share-chat:0:1:44:nickname","share preserves pet nickname");
        Reset();HornDialog.InputFocused=GossipDlg.InputFocused=true;Execute(3);Only("share-horn:live item","focused horn gets first refusal even when insertion fails");
        Reset();GossipDlg.InputFocused=true;Execute(3);Only("share-gossip:live item","focused gossip receives share");
        Console.WriteLine("PASS native mobile inventory actions: "+checks+" checks");
    }
}

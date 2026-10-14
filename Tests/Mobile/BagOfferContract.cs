using System;
using System.Collections.Generic;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace UnityEngine { public struct Color { public Color(float r,float g,float b,float a){} } }
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal static class SocialClient { public static bool TradeActive, MyLocked, OtherLocked, MyConfirmed, OtherConfirmed; public static int OtherId; }
    internal partial class InventoryDlg
    {
        internal struct Item { public int id,count,bagIdx; public bool locked,bound; public string name,fusedDesc; }
        internal static readonly List<Item>[] Items = { new List<Item>(),new List<Item>(),new List<Item>(),new List<Item>() };
        internal static InventoryDlg I; static float _lockReqAt;
        void ClearMobileSelection(){}
        internal static Item ItemAt(int tab,int slot) { foreach(var item in Items[tab])if(item.bagIdx==slot)return item; return default; }
        internal static bool WarnIfLocked(int tab,int slot,string action)=>ItemAt(tab,slot).locked;
    }
    internal class SysMsgBox
    {
        internal static SysMsgBox I = new SysMsgBox(); internal static bool Visible; internal static Action Yes;
        internal string MessageForIndependentRuntime;
        internal static void Show(string text,Action yes,Action no){ I.MessageForIndependentRuntime=text;Visible=true;Yes=yes; }
        internal static void Finish(){var yes=Yes;Yes=null;Visible=false;yes?.Invoke();}
    }
    internal static class ChatUI { public static int Messages; internal static void Add(string c,string text,UnityEngine.Color colour){Messages++;} }
    internal sealed partial class DealDlg
    {
        internal static DealDlg I; internal static bool Visible;
        internal List<(byte,int,int)> myOffer = new List<(byte,int,int)>();
        internal int Sends; internal string LastFusedMessageForIndependentRuntime;
        private void SendOffer(){Sends++;}
        internal void NewSession(){offerSession++;myOffer.Clear();}
        internal void Offer(byte tab,int slot)=>OfferBagItem(tab,slot);
    }
}
class BagOfferContract
{
    static int checks;
    static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
    static void Reset(DealDlg d, bool fused=false)
    {
        DealDlg.Visible=true;SocialClient.TradeActive=true;SocialClient.OtherId=7;
        SocialClient.MyLocked=SocialClient.OtherLocked=SocialClient.MyConfirmed=SocialClient.OtherConfirmed=false;
        SysMsgBox.Visible=false;SysMsgBox.Yes=null;d.NewSession();d.Sends=0;
        Set(5,false,false,fused);
    }
    static void Set(int count,bool locked=false,bool bound=false,bool fused=false)
    { InventoryDlg.SetItems(1,new List<InventoryDlg.Item>{new InventoryDlg.Item{id=123,bagIdx=4,count=count,locked=locked,bound=bound,name="stack",fusedDesc=fused?"fused":null}}); }
    static void Main()
    {
        var d=new DealDlg();DealDlg.I=d;Reset(d);
        DealDlg.ChangeBagOffer(1,4);Check(d.myOffer.Count==1&&d.myOffer[0].Item3==5&&d.Sends==1,"explicit add preserves full stack count");
        d.Offer(1,4);Check(d.myOffer.Count==1&&d.Sends==1,"duplicate held-item add does not resend");
        DealDlg.ChangeBagOffer(1,4);Check(d.myOffer.Count==0&&d.Sends==2,"explicit withdrawal uses same live offer list");
        Set(2);DealDlg.ChangeBagOffer(1,4);Check(d.myOffer[0].Item3==2,"post-split receipt uses current stack count");
        foreach(int state in new[]{0,1,2,3,4})
        {
            Reset(d);if(state==0)SocialClient.MyLocked=true;if(state==1)SocialClient.MyConfirmed=true;if(state==2)SocialClient.OtherConfirmed=true;if(state==3)SocialClient.TradeActive=false;if(state==4)DealDlg.Visible=false;
            DealDlg.ChangeBagOffer(1,4);Check(d.Sends==0,"closed or finalized trade blocks add "+state);
            d.myOffer.Add((1,4,5));DealDlg.ChangeBagOffer(1,4);Check(d.myOffer.Count==1&&d.Sends==0,"closed or finalized trade blocks withdrawal "+state);
        }
        Reset(d);SocialClient.OtherLocked=true;DealDlg.ChangeBagOffer(1,4);Check(d.Sends==1,"other lock alone preserves native editable-side semantics");
        Reset(d);Set(5,true);DealDlg.ChangeBagOffer(1,4);Check(d.Sends==0,"locked item denied");
        Set(5,false,true);DealDlg.ChangeBagOffer(1,4);Check(d.Sends==0&&ChatUI.Messages>0,"bound item denied with original warning");
        Reset(d);for(int i=0;i<20;i++)d.myOffer.Add((0,i,1));DealDlg.ChangeBagOffer(1,4);Check(d.Sends==0&&d.myOffer.Count==20,"offer capacity preserved");
        Reset(d,true);DealDlg.ChangeBagOffer(1,4);Check(d.Sends==0&&SysMsgBox.Visible,"fused item awaits original confirmation");SysMsgBox.Finish();Check(d.Sends==1,"confirmed fused item added once");SysMsgBox.Finish();Check(d.Sends==1,"duplicate confirmation inert");
        foreach(int change in new[]{0,1,2,3,4,5,6})
        {
            Reset(d,true);DealDlg.ChangeBagOffer(1,4);
            if(change==0)Set(5,false,false,true); // identical replacement receipt still invalidates
            if(change==1)SocialClient.MyLocked=true;
            if(change==2)SocialClient.OtherConfirmed=true;
            if(change==3)DealDlg.Visible=false;
            if(change==4)d.NewSession();
            if(change==5)SocialClient.OtherId++;
            if(change==6){var row=InventoryDlg.Items[1][0];row.locked=true;InventoryDlg.Items[1][0]=row;}
            SysMsgBox.Finish();Check(d.Sends==0,"stale confirmation blocked "+change);
        }
        Reset(d);DealDlg.ChangeBagOffer(255,4);DealDlg.ChangeBagOffer(1,-1);Check(d.Sends==0,"invalid bag identity inert");
        Console.WriteLine("PASS shared bag trade actions: "+checks+" checks");
    }
}

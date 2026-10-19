using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine { public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;} } }
namespace FakeFantasyOnline.Application { public class WorldChatSharedEntryState { public string name; } }
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal static class BitmapFont { internal static object LayoutIdentity=new object(); }
    internal static class EmoteText
    {
        internal static int WrapCalls;
        internal static List<string> Wrap(string text,float width,float scale)
        {
            WrapCalls++;
            int step=Math.Max(1,(int)(width/scale));
            var rows=new List<string>();
            if(string.IsNullOrEmpty(text)) rows.Add("");
            else for(int i=0;i<text.Length;i+=step) rows.Add(text.Substring(i,Math.Min(step,text.Length-i)));
            return rows;
        }
    }
    internal partial class ChatUI
    {
        private static readonly List<Line> chatLines=new List<Line>(),_sysTmp=new List<Line>(),_chatTmp=new List<Line>();
        private static int filterHide,filterCalls;
        private static string _filterChar, requestedCharacter="first";
        private static int requestedMask;
        private static ChatUI I;
        private string whisperTarget; private bool panelsVisible; private static bool _pendingFocus;
        private static void LoadFilter() { if (_filterChar!=requestedCharacter){_filterChar=requestedCharacter;filterHide=requestedMask;} }
        private static bool ChanShown(string ch) {filterCalls++;return !(ch=="世界"&&(filterHide&1)!=0)&&!(ch=="附近"&&(filterHide&2)!=0);}
        private static readonly Func<long> AllocatedBytes=(Func<long>)Delegate.CreateDelegate(typeof(Func<long>),typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread"));
        private static int assertions;
        private static void Check(bool condition,string label){if(!condition)throw new Exception(label);assertions++;}
        private static void AssertRows(List<ReferenceChatVisualRow> actual,params string[] expected)
        {
            Check(actual.Count==expected.Length,"Visible row count");
            for(int i=0;i<expected.Length;i++) Check(actual[i].Text==expected[i],"Visible row order/text");
        }
        private static void Main()
        {
            var view=new ChatUI(); var mobile=new List<Line>();
            var shares=new List<FakeFantasyOnline.Application.WorldChatSharedEntryState>{new FakeFantasyOnline.Application.WorldChatSharedEntryState{name="before"}};
            Add("附近","abcdef",new Color(1,0,0,1),"sender",shares);
            Add("系统","alert",new Color(0,1,0,1));
            Add("世界","world",new Color(0,0,1,1));
            CollectMobileLines(mobile); PartitionLinesForIndependentRuntime();
            Check(mobile.Count==3&&_sysTmp.Count==1&&_chatTmp.Count==2,"Mobile interleaving and desktop partition preserved");
            var rows=view.VisibleRows(mobile,3,3,1);
            AssertRows(rows,"rt","wor","ld"); Check(!rows[0].First&&rows[1].First&&rows[1].Channel=="[世界]"&&!rows[2].First,"Prefix only on original first wrapped row");
            int scanned=filterCalls,wrapped=EmoteText.WrapCalls;
            long allocated=AllocatedBytes();
            for(int i=0;i<1000;i++) {CollectMobileLines(mobile);PartitionLinesForIndependentRuntime();view.VisibleRows(mobile,3,3,1);}
            long bytes=AllocatedBytes()-allocated;
            Check(filterCalls==scanned&&EmoteText.WrapCalls==wrapped,"1000 stable passes neither scan history nor wrap rows");
            Check(bytes==0,"1000 stable content/layout cache passes allocate zero managed bytes");
            view.VisibleRows(mobile,3.01f,3,1); Check(EmoteText.WrapCalls>wrapped,"Subpixel width change invalidates rows"); wrapped=EmoteText.WrapCalls;
            view.VisibleRows(mobile,3.01f,3,1.5f); Check(EmoteText.WrapCalls>wrapped,"Scale/text-size change invalidates rows"); wrapped=EmoteText.WrapCalls;
            view.VisibleRows(mobile,3.01f,4,1.5f); Check(EmoteText.WrapCalls>wrapped,"Panel capacity change invalidates rows"); wrapped=EmoteText.WrapCalls;
            BitmapFont.LayoutIdentity=new object(); view.VisibleRows(mobile,3.01f,4,1.5f); Check(EmoteText.WrapCalls>wrapped,"Font metric identity change invalidates rows");
            filterHide=1; CollectMobileLines(mobile);PartitionLinesForIndependentRuntime();
            Check(mobile.Count==2&&_chatTmp.Count==1,"Live channel checkbox invalidates both projections");
            requestedCharacter="second";requestedMask=2;CollectMobileLines(mobile);PartitionLinesForIndependentRuntime();
            Check(mobile.Count==2&&mobile[0].ch=="系统"&&_chatTmp[0].ch=="世界","Character-specific filter reload invalidates both projections");
            requestedCharacter="third";requestedMask=0;CollectMobileLines(mobile);
            rows=view.VisibleRows(mobile,100,3,1);
            shares[0].name="after";
            Check(ReferenceEquals(rows[0].Line.shares,shares)&&rows[0].Line.shares[0].name=="after","Cached rows retain live share metadata instead of stale projections");
            Check(rows[0].Line.who=="sender"&&rows[0].Line.col.r==1,"Sender and colors retained for interaction/rendering");
            int version=Ver;ClearLinesForIndependentRuntime();CollectMobileLines(mobile);PartitionLinesForIndependentRuntime();
            Check(Ver==version,"Clear preserves public history-version semantics");
            Check(mobile.Count==0&&_sysTmp.Count==0&&_chatTmp.Count==0&&view.VisibleRows(mobile,100,3,1).Count==0,"Clear invalidates all projections and visible rows");
            Add("附近","replacement",new Color()); CollectMobileLines(mobile); rows=view.VisibleRows(mobile,100,3,1);AssertRows(rows,"replacement");
            ClearLinesForIndependentRuntime();Add("附近","same count",new Color());CollectMobileLines(mobile);rows=view.VisibleRows(mobile,100,3,1);AssertRows(rows,"same count");
            ClearLinesForIndependentRuntime();for(int i=0;i<800;i++)Add("附近",i.ToString(),new Color());
            CollectMobileLines(mobile);PartitionLinesForIndependentRuntime();view.VisibleRows(mobile,100,3,1);
            scanned=filterCalls;wrapped=EmoteText.WrapCalls;Add("附近","800",new Color());CollectMobileLines(mobile);PartitionLinesForIndependentRuntime();
            Check(mobile.Count==800&&mobile[0].text=="1"&&_chatTmp[799].text=="800","Rollover invalidates at unchanged 800-line count");
            rows=view.VisibleRows(mobile,100,3,1);AssertRows(rows,"798","799","800");
            Check(filterCalls==scanned+1600&&EmoteText.WrapCalls>wrapped,"Rollover rebuilds each bounded projection once");
            scanned=filterCalls;wrapped=EmoteText.WrapCalls;allocated=AllocatedBytes();
            for(int i=0;i<1000;i++){CollectMobileLines(mobile);PartitionLinesForIndependentRuntime();view.VisibleRows(mobile,100,3,1);}
            bytes=AllocatedBytes()-allocated;
            Check(filterCalls==scanned&&EmoteText.WrapCalls==wrapped&&bytes==0,"800-line steady history: zero scan/wrap/allocation work across 1000 passes");
            var newDestination=new List<Line>();CollectMobileLines(newDestination);
            Check(newDestination.Count==800,"Replacement mobile owner gets populated");
            newDestination.Clear();CollectMobileLines(newDestination);Check(newDestination.Count==800,"Externally emptied destination is repopulated");
            for(int i=0;i<10;i++)view.VisibleRows(new List<Line>(mobile),100,3,1);
            Check(view.visualRowsCache.Length==3,"Visible source-cache storage remains bounded");
            Check(view.VisibleRows(mobile,100,0,1).Count==0,"Zero-capacity panels have no rows");
            Console.WriteLine("PASS chat caches: "+assertions+" assertions. At 800 messages, 1000 stable passes: 0 history predicates, 0 wrapping calls, 0 managed bytes. Actual cache/message sources; deterministic font/filter substitutes, no Unity/device run.");
        }
    }
}

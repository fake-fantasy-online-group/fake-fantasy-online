using System;
using System.Collections.Generic;
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
internal partial class ChatUI {
 internal struct Line {public string ch,text;}
 private static readonly List<Line> chatLines=new List<Line>();
 private static string requestedFilter,loadedFilter;
 private static int loadCount, filterCalls, filterHide;
 private static string _filterChar;
 private static void LoadFilter(){loadedFilter=requestedFilter;_filterChar=requestedFilter;loadCount++;}
 public static bool ChanShown(string channel){filterCalls++;return channel!=loadedFilter;}
 static int checks;
 static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 static void Main(){
 var output=new List<Line>();
 chatLines.Add(new Line{ch="附近",text="普通聊天"});
 chatLines.Add(new Line{ch="系统",text="技能正在冷却中"});
 chatLines.Add(new Line{ch="世界",text="世界消息"});
 chatLines.Add(new Line{ch="系统",text="物品不能使用"});
 requestedFilter="世界";
 CollectMobileLines(output);
 Check(loadCount==1,"Character filters load before first mobile draw");
 Check(output.Count==3,"System messages retained and hidden channel excluded");
 Check(output[0].text=="普通聊天","Ordinary order retained");
 Check(output[1].text=="技能正在冷却中","Cooldown feedback visible");
 Check(output[2].text=="物品不能使用","Item feedback remains latest");
 Check(chatLines.Count==4,"Presentation never emits/re-enables messages");
 requestedFilter="附近";CollectMobileLines(output);
 Check(loadCount==2,"Character change/filter rechecked");
 Check(output.Count==3&&output[0].ch=="系统"&&output[1].ch=="世界","Updated filter and chronological interleaving");
 int scanned=filterCalls;
 for(int i=0;i<1000;i++) CollectMobileLines(output);
 Check(output.Count==3,"Repeated draws do not duplicate rows");
 Check(filterCalls==scanned,"Unchanged mobile passes perform no history rescan");
 var other=new List<Line>();CollectMobileLines(other);
 Check(other.Count==3&&filterCalls==scanned+4,"A replacement view gets current rows");
 chatLines.Clear();CollectMobileLines(output);Check(output.Count==0,"Clear removes stale system and ordinary text");
 Console.WriteLine("PASS mobile chat: "+checks+" assertions; actual collection source, deterministic chat/filter substitutes.");
 }
}
}

using System;
using System.Collections.Generic;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Application {
 public class WorldNpcTaskOfferState { public int TaskId; public byte State; public string Title,Description,Award; }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 public sealed partial class DialogUI {
  public static DialogUI I; public bool Visible,InSubView;
  public FakeFantasyOnline.Application.WorldNpcTaskOfferState LastOfferIntent;
  public int CurrentNpcEntityId; public void Close(){Visible=false;}
 }
 public static partial class TaskClient { public static ActiveTask GetActive(int id){return Active.Find(t=>t.id==id);} }
 public class NetClient {
  public static NetClient I; public Action OnComplete;
  public void TaskNpcListForIndependentRuntime(string s){} public void TaskAcceptForIndependentRuntime(int i){}
  public void TaskAbandonForIndependentRuntime(int i){} public void TaskCompleteForIndependentRuntime(int i){OnComplete?.Invoke();}
 }
}
class QuestFeedbackContract {
 static int checks;
 static void Assert(bool b,string name){if(!b)throw new Exception(name);checks++;}
 static void State(int id,int step,bool done=false){TaskClient.SetState(id==0?new List<TaskClient.ActiveTask>():new List<TaskClient.ActiveTask>{new TaskClient.ActiveTask{id=id,step=step}},done?new List<int>{7}:new List<int>());}
 static DialogUI Open(){var d=new DialogUI{Visible=true,InSubView=true,CurrentNpcEntityId=42,LastOfferIntent=new FakeFantasyOnline.Application.WorldNpcTaskOfferState{TaskId=7}};DialogUI.I=d;return d;}
 static void Main(){
  NetClient.I=new NetClient();State(7,2);var d=Open();TaskClient.Complete(7);
  Assert(d.Visible,"send is not confirmation"); State(7,2);Assert(d.Visible,"unchanged/rejected snapshot stays open");
  State(7,3);Assert(!d.Visible,"matching step advance closes");
  State(7,2);d=Open();TaskClient.Complete(7);State(0,0,true);Assert(!d.Visible,"completed hand-in closes");
  State(7,2);d=Open();TaskClient.Complete(7);State(0,0);Assert(!d.Visible,"repeatable removed task closes without done bit");
  State(7,2);d=Open();TaskClient.Complete(8);State(7,3);Assert(d.Visible,"unrelated command does not arm close");
  State(7,2);d=Open();TaskClient.Complete(7);d.LastOfferIntent=new FakeFantasyOnline.Application.WorldNpcTaskOfferState{TaskId=7};State(7,3);Assert(d.Visible,"late reply cannot close reopened offer");
  State(7,2);d=Open();TaskClient.Complete(7);d.CurrentNpcEntityId=99;State(7,3);Assert(d.Visible,"late reply cannot close different NPC");
  State(7,2);d=Open();NetClient.I.OnComplete=()=>State(7,3);TaskClient.Complete(7);Assert(!d.Visible,"track before synchronous receipt");NetClient.I.OnComplete=null;
  State(7,2);d=Open();NetClient.I=null;TaskClient.Complete(7);State(7,3);Assert(d.Visible,"no transport does not arm close");
  var layout=new TaskPromptLayout();int measured=0;Func<string,float> measure=t=>{measured++;return t.Length==2&&char.IsSurrogate(t[0])?12:t[0]>127?12:6;};
  var lines=new List<string>{"任务进度很长，目标还有很多，继续前往下一处", "abc\r\ndef😀ghi"};
  layout.Update(lines,48,1,measure);Assert(layout.Rows.Count>3,"long red prompt wraps");
  for(int i=0;i<layout.Rows.Count;i++)Assert(layout.Widths[i]<=48,"row fits width");
  Assert(string.Join("",layout.Rows)==string.Join("",lines).Replace("\r","").Replace("\n",""),"wrap preserves all text");
  foreach(string row in layout.Rows)Assert(row.Length==0||!char.IsLowSurrogate(row[0]),"surrogate pair not split");
  int before=measured;layout.Update(lines,48,1,measure);Assert(before==measured,"repeat GUI pass uses cache");
  layout.Update(lines,24,1,measure);Assert(measured>before,"resize relayouts");
  before=measured;layout.Update(lines,24,2,measure);Assert(measured>before,"scale change relayouts");
  lines[0]="新消息";layout.Update(lines,24,2,measure);Assert(layout.Rows[0]=="新消","same-count replacement relayouts");
  layout.Update(new List<string>{"甲乙"},1,1,measure);Assert(layout.Rows.Count==2,"tiny viewport terminates without empty leading row");
  Console.WriteLine("PASS: "+checks+" quest receipt and prompt layout checks");
 }
}

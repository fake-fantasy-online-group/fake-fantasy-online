using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Application {
public sealed class WorldNpcState { public int EntityId; public string DisplayName, Script; }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
public sealed class ReferenceNpcDialogOption {
 public readonly string Label; public readonly Action Act;
 public ReferenceNpcDialogOption(string label,Action act){Label=label;Act=act;}
}
public sealed partial class DialogUI {
 public string NpcName="",Greeting=""; public bool Visible=true;
 public readonly List<ReferenceNpcDialogOption> Options=new List<ReferenceNpcDialogOption>();
 private void Add(ReferenceNpcDialogOption option){Options.Add(option);}
 public void Close(){Visible=false;}
 public void OpenForTest(WorldNpcState npc){NpcName=npc.DisplayName;CaptureRiftExit(npc);BuildForTest();Visible=true;}
 public void BuildForTest(){Options.Clear();AddRiftExitOptions();}
}
public sealed class NetClient {
 public static NetClient I=new NetClient();public readonly List<string> Commands=new List<string>();
 public void GmCmdForIndependentRuntime(string command){Commands.Add(command);}
}
public static class EndgamePanel { public static EndgameClientState WorldState; }
public sealed class GameBootstrap {
 public static GameBootstrap I=new GameBootstrap(); public static int CurMapId=921;
 public readonly TestWorld Window=new TestWorld();
}
public sealed class TestWorld {
 public WorldNpcState Current;
 public bool IsNpcStateCurrentForIndependentRuntime(WorldNpcState source)=>ReferenceEquals(Current,source);
}
public static class RiftExitDialogContract {
 static int checks;
 static void Check(bool condition,string error){checks++;if(!condition)throw new Exception(error);}
 static WorldNpcState Npc(string name="洞天残魂",string script="endgame_rift_exit")=>new WorldNpcState{EntityId=101,DisplayName=name,Script=script};
 static DialogUI Open(WorldNpcState npc=null){npc=npc??Npc();GameBootstrap.I.Window.Current=npc;var d=new DialogUI();d.OpenForTest(npc);return d;}
 static void Unsent(Action change,string error){int before=NetClient.I.Commands.Count;var d=Open();Action click=d.Options[0].Act;change();click();Check(NetClient.I.Commands.Count==before,error);}
 public static void Main(){
  EndgamePanel.WorldState=new EndgameClientState();
  var dialog=Open();Check(dialog.Options.Count==1,"missing native NPC exit option");
  Check(dialog.Greeting=="好久没见到别的人了……这里的风，总算静下来了。来吧，我这就送你回去。","incorrect spirit greeting");
  Check(dialog.Options[0].Label=="劳烦送我回去","wrong native exit row");
  Action first=dialog.Options[0].Act;first();
  Check(!dialog.Visible&&NetClient.I.Commands.Count==1&&NetClient.I.Commands[0]=="trialleave","exit did not dispatch directly");
  first();Check(NetClient.I.Commands.Count==1,"repeated click dispatched twice");
  dialog=Open();Action cancelled=dialog.Options[0].Act;dialog.Close();cancelled();Check(NetClient.I.Commands.Count==1,"dismissed dialogue dispatched");
  dialog=Open();Action old=dialog.Options[0].Act;dialog.OpenForTest(GameBootstrap.I.Window.Current);old();Check(NetClient.I.Commands.Count==1,"reopened dialog revived stale action");
  dialog.Options[0].Act();Check(NetClient.I.Commands.Count==2,"fresh reopened action was blocked");
  Unsent(()=>EndgamePanel.WorldState.Reset(),"reconnect revived stale generation");
  Unsent(()=>EndgamePanel.WorldState=new EndgameClientState(),"new session revived stale state");
  Unsent(()=>EndgamePanel.WorldState=null,"disconnected state dispatched");EndgamePanel.WorldState=new EndgameClientState();
  Unsent(()=>GameBootstrap.I.Window.Current=Npc(),"reused actor ID revived stale projection");
  Unsent(()=>GameBootstrap.CurMapId++,"map change revived stale actor");
  dialog=Open();int before=NetClient.I.Commands.Count;var client=NetClient.I;NetClient.I=null;dialog.Options[0].Act();NetClient.I=client;Check(client.Commands.Count==before&&!dialog.Visible,"missing network client was not safe");
  dialog=Open(Npc("归元道使"));Check(dialog.Options.Count==1,"historical frozen run lost exit service");dialog.Options[0].Act();Check(client.Commands.Count==before+1,"legacy exit did not dispatch directly");
  Check(Open(Npc("洞天残魂的邻居")).Options.Count==0,"substring granted exit service");
  Check(Open(Npc("妖魔结界守卫")).Options.Count==0,"entrance gained exit service");
  Check(Open(Npc(script:"N362")).Options.Count==0,"display name alone granted exit service");
  var empty=Npc();empty.EntityId=0;Check(Open(empty).Options.Count==0,"missing actor gained exit service");
  Console.WriteLine("PASS "+checks+" native rift exit dialogue assertions: direct action, exact identity, dismissal, repeated clicks, reopen, reconnect, actor replacement, and legacy runs");
 }
}
}

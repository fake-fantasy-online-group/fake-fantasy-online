using System;
using System.Collections.Generic;
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
public sealed class ReferenceNpcDialogOption {
 public readonly string Label; public readonly Action Act;
 public ReferenceNpcDialogOption(string label,Action act){Label=label;Act=act;}
}
public sealed partial class DialogUI {
 public string NpcName="",Greeting=""; public bool Visible=true;
 public readonly List<ReferenceNpcDialogOption> Options=new List<ReferenceNpcDialogOption>();
 private void Add(ReferenceNpcDialogOption option){Options.Add(option);}
 public void BuildForTest(){Options.Clear();AddRiftExitOptions();}
}
public sealed class NetClient {
 public static NetClient I=new NetClient();public readonly List<string> Commands=new List<string>();
 public void GmCmdForIndependentRuntime(string command){Commands.Add(command);}
}
public static class SysMsgBox {
 public static Action Confirm,Cancel;public static string Prompt;
 public static void Show(string text,Action confirm,Action cancel){Prompt=text;Confirm=confirm;Cancel=cancel;}
}
public static class RiftExitDialogContract {
 static void Check(bool condition,string error){if(!condition)throw new Exception(error);}
 public static void Main(){
  var dialog=new DialogUI{NpcName="归元道使"};dialog.BuildForTest();
  Check(dialog.Options.Count==1,"missing native NPC exit option");
  Check(dialog.Greeting.Contains("劫影已散"),"missing story greeting");
  dialog.Options[0].Act();
  Check(!dialog.Visible&&SysMsgBox.Confirm!=null&&NetClient.I.Commands.Count==0,"exit before confirmation");
  Check(SysMsgBox.Prompt.Contains("收束后"),"incorrect offline recovery promise");
  SysMsgBox.Confirm();Check(NetClient.I.Commands.Count==1&&NetClient.I.Commands[0]=="trialleave","wrong native route");
  dialog.NpcName="归元道使的邻居";dialog.BuildForTest();Check(dialog.Options.Count==0,"substring granted exit service");
  dialog.NpcName="妖魔结界守卫";dialog.BuildForTest();Check(dialog.Options.Count==0,"entrance gained exit service");
  Console.WriteLine("PASS native rift NPC exit dialogue: exact name, confirmation, story, existing command");
 }
}
}

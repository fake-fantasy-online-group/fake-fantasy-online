using System;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace UnityEngine {
 public struct Color{} public struct Matrix4x4{} public static class Time{public static float realtimeSinceStartup;}
 public class Settings{public Color selectionColor,cursorColor;} public class Skin{public Settings settings=new Settings();}
 public static class GUI{public static Color color,backgroundColor,contentColor;public static bool enabled;public static Matrix4x4 matrix;public static Skin skin=new Skin();}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 internal enum TitlePage{Login,Register,Recover,Appeal,ServerSelection,CharacterSelection,CharacterCreation}
 internal static class MobileClientPresentation{internal static bool Enabled=false;}
 internal class Input{internal void Close(){} internal void BeginPage(int n){}}
 internal class Creation{internal void BindNativeForIndependentRuntime(object t,Action a,Action b,Func<string> c){}}
 internal class Session {
  internal object Characters; internal bool Connected;internal string ServerHostForIndependentRuntime;internal int ServerPortForIndependentRuntime;
  internal void ConnectForAccount(Action<bool> cb){cb(true);} internal void TheftAppeal(string a,string b,string c){} internal void ResetPassword(string a,string b,string c){} internal void RegisterAccount(string a,string b,string c,string d){}
  internal void ShowLoginForAccountResult(){} internal void ReturnToCharacterSelection(){} internal void ReturnToLoginFromServerPageForIndependentRuntime(){}
 }
 internal class Host{internal Session _session=new Session();internal Action _projectNativeState;internal bool _backgroundSessionCheckRequested,_loginDiagnosticsActive;internal object _characters;internal void BeginCreateCharacter(){}internal void BeginRefreshServerStatus(){}}
 internal static class ClientRootView{internal static string SafeError(Error e)=>e.Code;}
 internal class NetClient {
  internal static NetClient I=new NetClient(); internal Action<bool> Callback;internal int Connects,Sends,Cancels;internal string Account,Password;
  internal void ConnectTitleLoginForIndependentRuntime(Action<bool> cb){Connects++;Callback=cb;}
  internal void CancelTitleLoginForIndependentRuntime(){Cancels++;}
  internal void SendTitleLoginForIndependentRuntime(string a,string p){Sends++;Account=a;Password=p;}
  internal void BindTitleServerConnectionForIndependentRuntime(Func<bool>a,Func<string>b,Func<int>c,Action d){}
 }
 // Only UI dependencies are substituted. BeginLogin, receive methods, DrawTitleGui and
 // its timeout branch compile verbatim from production; never extracted/reimplemented.
 internal sealed partial class TitleFlow {
  Host guiHost=new Host();Input mobileTitleInput=new Input();Creation creationGui=new Creation();TitlePage _page;bool loggingIn,entering,_noticeOpen,_rememberAccount,_rememberPassword;float loginAtMs;string account="",password="",netMsg="",RegisterAccount="",RegisterPassword="",RegisterSecurityCode="",RegisterInviteCode="",RecoverAccount="",RecoverPassword="",RecoverSecurityCode="";string result;
  void PublishPluginTitleResult(string s,int? code=null){result=s;}void SaveLoginPrefs(){}void CloseRegisterForm(){}void ClearRegisterFields(){}void CloseRecoverForm(){}void ClearRecoverFields(){}
  void TryLoad05Login(){}void EnsureStylesForIndependentRuntime(){}void EnsureCreationStylesForIndependentRuntime(){}UnityEngine.Matrix4x4 BeginGuiFrameForIndependentRuntime(bool b)=>new UnityEngine.Matrix4x4();void DrawCharCreate(){}void BindRosterForIndependentRuntime(object o){}void DrawCharSelect(){}void DrawServer(){}void DrawRegister(){}void DrawRecover(){}void DrawAppeal(){}void LoadLoginPrefs(){}void EnsureStyles(){}void DrawLogin(){}void BindServerPageForIndependentRuntime(Session s,Func<string> m,Action back,Action<Result> complete){}
  static int checks;static void Check(bool c,string s){if(!c)throw new Exception(s);checks++;}
  public static void Main(){
   var t=new TitleFlow();NetClient.I=new NetClient();UnityEngine.Time.realtimeSinceStartup=0;t.password="A-password";t.BeginLogin("A");Action<bool> old=NetClient.I.Callback;t.BeginLogin("duplicate");Check(NetClient.I.Connects==1,"production BeginLogin duplicate guard");
   UnityEngine.Time.realtimeSinceStartup=11;t.DrawTitleGui();Check(!t.loggingIn&&t.result=="login_timeout"&&NetClient.I.Cancels==1,"production OnGUI timeout cancels transport");
   t.password="B-password";t.BeginLogin("B");Action<bool> current=NetClient.I.Callback;old(true);Check(NetClient.I.Sends==0&&t.loggingIn,"late A callback cannot send B credentials or clear latch");current(true);Check(NetClient.I.Sends==1&&NetClient.I.Account=="B"&&NetClient.I.Password=="B-password","B captured credentials sent");
   t.ReceiveNativeTitleLoginResult(0,"OK");Check(!t.loggingIn&&t._page==TitlePage.ServerSelection,"production receipt moves page");old(false);Check(t.result=="login_succeeded","late failed callback cannot overwrite success");
   t._page=TitlePage.Login;t.BeginLogin("again");NetClient.I.Callback(false);Check(!t.loggingIn&&t.result=="connection_failed","normal connect failure releases title latch");
   t.BeginLogin("error");Action<bool> errorCallback=NetClient.I.Callback;int before=NetClient.I.Cancels;t.ReceiveNativeNetworkError("network failed");Check(!t.loggingIn&&NetClient.I.Cancels==before+1,"network error cancels active login");int sends=NetClient.I.Sends;errorCallback(true);Check(NetClient.I.Sends==sends,"network-error retired callback cannot send");
   Console.WriteLine("PASS TitleFlowContract: "+checks+" assertions (production title login + GUI timeout, UI dependencies substituted)");
  }
 }
}

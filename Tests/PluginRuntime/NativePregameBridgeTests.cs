using System;
using System.Collections.Generic;
using FakeFantasyOnline.Presentation.ReferenceAssets;

// Test doubles for the other Unity/native partials. The bridge and login dispatch below
// are production source. No test opens a socket or instantiates a Unity runtime.
namespace UnityEngine {
    public static class Mathf { public static int Max(int a,int b)=>Math.Max(a,b); public static int Clamp(int a,int b,int c)=>Math.Max(b,Math.Min(a,c)); }
    public static class Time { public static float realtimeSinceStartup; }
}
namespace FakeFantasyOnline.Application { public sealed class Placeholder {} }
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
    internal enum TitlePage { Login, Register, Recover, Appeal, ServerSelection, CharacterSelection, CharacterCreation }
    internal static class RecoveredTitleLoginSurface { public static bool NeedUpdate; }
    internal struct TestCharacter { public string CharacterId, DisplayName; public int Level,Race,Gender; }
    internal sealed class TestSession {
        public IReadOnlyList<TestCharacter> Characters = new TestCharacter[0];
        public int TitleServerCountForIndependentRuntime;
        public void ConnectForAccount(Action<bool> action) { action(true); }
        public void TheftAppeal(string a,string b,string c) {}
        public void ResetPassword(string a,string b,string c) {}
        public void RegisterAccount(string a,string b,string c,string d) {}
        public void ShowLoginForAccountResult() {}
    }
    internal sealed class TestHost { public TestSession _session = new TestSession(); public bool _backgroundSessionCheckRequested, _loginDiagnosticsActive; }
    internal sealed class NetClient {
        public static NetClient I = new NetClient();
        public Action<bool> ConnectCallback;
        public string SentAccount,SentPassword;
        public bool ThrowConnect,ThrowSend;
        public int ConnectCount,SendCount;
        public void ConnectTitleLoginForIndependentRuntime(Action<bool> callback) { ConnectCount++; if(ThrowConnect) throw new Exception("fixture"); ConnectCallback=callback; }
        public void SendTitleLoginForIndependentRuntime(string account,string password) { SendCount++; SentAccount=account;SentPassword=password; if(ThrowSend) throw new Exception("fixture"); }
    }
    internal sealed partial class TitleFlow {
        internal bool isActiveAndEnabled=true;
        private TestHost guiHost = new TestHost();
        private TitlePage _page;
        private bool loggingIn,entering,_noticeOpen,pendingDelete;
        private string account="",password="",netMsg="";
        private float loginAtMs;
        private bool _rememberAccount=true,_rememberPassword=true;
        private int SaveCount;
        private string RegisterAccount="",RegisterPassword="",RegisterSecurityCode="",RegisterInviteCode="";
        private string RecoverAccount="",RecoverPassword="",RecoverSecurityCode="";
        private void CloseRegisterForm() {} private void ClearRegisterFields() {} private void CloseRecoverForm() {} private void ClearRecoverFields() {}
        private void SaveLoginPrefs() { SaveCount++; }
        private struct SrvLine { public string name; public int directoryIndex,busy;public bool pvp; }
        private List<string> srvWorlds=new List<string>{"World"};
        private List<List<SrvLine>> srvLines=new List<List<SrvLine>>{new List<SrvLine>{new SrvLine{name="Line",directoryIndex=17,busy=1}}};
        private int srvBuiltFor,selWorld,selZone,selSlot=-1,MirrorCount,ConfirmCount,EntryCount;
        private bool Reauthenticate;
        private IReadOnlyList<TestCharacter> serverChars=new TestCharacter[0];
        private TestSession serverSession;
        private void PrepareServerPageForPluginAndGui() { serverSession=guiHost._session; }
        private void BuildSrvTree() { srvBuiltFor=guiHost._session.TitleServerCountForIndependentRuntime; }
        private int LevelOf(SrvLine line)=>line.busy-1;
        private void BindRosterForIndependentRuntime(IReadOnlyList<TestCharacter> rows) { serverChars=rows; if(selSlot<0 || selSlot>=rows.Count)selSlot=rows.Count>0?0:-1; }
        private void MirrorSlotToCharData(int slot) { MirrorCount++; }
        private void ConfirmServer() { ConfirmCount++; _page=Reauthenticate?TitlePage.Login:TitlePage.CharacterSelection; }
        private void EnterGame() { EntryCount++; entering=true; }

        private static int checks;
        private static void Check(bool pass,string label) { checks++;if(!pass)throw new Exception(label); }
        private static TitleFlow Fresh() { NetClient.I=new NetClient();RecoveredTitleLoginSurface.NeedUpdate=false;return new TitleFlow(); }
        public static void Main() {
            var title=Fresh();
            Check(title.SubmitLogin()=="native_credentials_required","empty native credentials");
            title.account="native";title.password="original";
            var first=title.ReadState(); var second=title.ReadState();
            Check(first.Revision==second.Revision,"stable observation revision");
            Check(title.SubmitCredentials(" supplied ","secret")==null,"submit credentials");
            Check(title.account=="native"&&title.password=="original","native fields restored before async completion");
            Check(title.SaveCount==0,"never saves plugin credential pair");
            Check(title.SubmitCredentials("other","other")=="title_busy","single native request");
            Check(NetClient.I.SendCount==0,"connect is asynchronous");
            NetClient.I.ConnectCallback(true);
            Check(NetClient.I.SentAccount=="supplied"&&NetClient.I.SentPassword=="secret","native callback captured exact submitted pair");
            NetClient.I.ConnectCallback(true);
            Check(NetClient.I.SentPassword==null,"captured password cleared after first send");
            title.ReceiveNativeTitleLoginResult(7,"arbitrary server text secret");
            var failure=title.ReadState();
            Check(failure.ResultCode=="login_failed"&&failure.NativeResultCode==7&&failure.HasNativeResultCode,"native numeric error");
            Check(!failure.SafeMessage.Contains("secret"),"server text redacted");
            Check(failure.Revision>first.Revision,"receipt advances revision");
            title=Fresh();title.account="old";title.password="old-password";NetClient.I.ThrowConnect=true;
            try {title.SubmitCredentials("new","new-password");throw new Exception("expected connect failure");}catch(Exception ex){Check(ex.Message=="fixture","connect failure propagated");}
            Check(title.account=="old"&&title.password=="old-password","restore even synchronous failure");
            title=Fresh();title.SubmitCredentials("user","secret");NetClient.I.ThrowSend=true;
            try { NetClient.I.ConnectCallback(true); } catch(Exception) {}
            NetClient.I.ThrowSend=false;NetClient.I.ConnectCallback(true);
            Check(NetClient.I.SentPassword==null,"captured password cleared on send exception");
            title=Fresh();title.SubmitCredentials("user","secret");NetClient.I.ConnectCallback(false);
            Check(title.ReadState().ResultCode=="connection_failed"&&!title.ReadState().LoginLoading,"connection failure result and latch");
            NetClient.I.ConnectCallback(true);Check(NetClient.I.SentPassword==null,"captured password cleared on connection failure");
            title=Fresh();
            Check(title.SubmitCredentials(new string('x',21),"pw")=="invalid_credentials","native account20 limit");
            Check(title.SubmitCredentials("user",new string('x',41))=="invalid_credentials","native password40 limit");
            Check(NetClient.I.ConnectCount==0,"invalid credentials never connect");
            title._noticeOpen=true;Check(title.SubmitLogin()=="native_modal_open","notice modal");title._noticeOpen=false;
            RecoveredTitleLoginSurface.NeedUpdate=true;Check(title.SubmitCredentials("user","pw")=="client_update_required","update gate");
            title=Fresh();title._page=TitlePage.ServerSelection;
            var servers=title.ReadState();Check(servers.Servers[0].DirectoryIndex==17,"native directory identity");
            servers.Servers[0].Name="mutated";Check(title.ReadState().Servers[0].Name=="Line","detached projection");
            Check(title.SelectServer(999)=="server_not_found","no guessed directory index");
            title.srvLines[0][0]=new SrvLine{name="Maintenance",directoryIndex=17,busy=4};
            Check(title.ConfirmSelectedServer()=="server_maintenance"&&title.ConfirmCount==0,"maintenance refusal");
            title.srvLines[0][0]=new SrvLine{name="Line",directoryIndex=17,busy=1};title.Reauthenticate=true;
            Check(title.SelectServer(17)==null&&title.ConfirmSelectedServer()==null&&title.ConfirmCount==1&&title._page==TitlePage.Login,"actual confirmation route supports relogin");
            title._page=TitlePage.CharacterSelection;
            title.guiHost._session.Characters=new[]{new TestCharacter{CharacterId="a",DisplayName="Alpha"},new TestCharacter{CharacterId="b",DisplayName="Beta"}};
            Check(title.SelectCharacter("b")==null&&title.selSlot==1&&title.MirrorCount==1,"select fresh native character by ID");
            Check(title.EnterWorld()==null&&title.EntryCount==1,"native entry method");
            Check(title.EnterWorld()=="title_busy"&&title.EntryCount==1,"no duplicate entry");
            title=Fresh();title._page=TitlePage.CharacterSelection;
            var rows=new TestCharacter[4100];for(int i=0;i<rows.Length;i++)rows[i]=new TestCharacter{CharacterId=i.ToString()};title.guiHost._session.Characters=rows;
            var bounded=title.ReadState();Check(bounded.Characters.Length==4096&&bounded.CharacterTotalCount==4100&&bounded.CharactersTruncated,"bounded roster projection");
            Check(title.ReadState().Revision==bounded.Revision,"stable capped roster revision");
            Console.WriteLine("PASS: "+checks+" native bridge execution checks");
        }
    }
}

#!/usr/bin/env python3
"""Execute exact production UI lifecycle methods with Unity/asset/network doubles.
Not a Unity Player or rendering test. Includes the previously missed Awake consumer.
"""
from pathlib import Path
import os, re, subprocess, tempfile
root=Path(__file__).resolve().parents[2]
r=root/'Assets/FakeFantasyOnline/Runtime'
p=r/'Presentation';a=p/'ReferenceAssets'
def method(path,signature):
 s=path.read_text();start=s.index(signature);brace=s.index('{',start);depth=1;i=brace+1
 while depth:
  depth += (s[i]=='{')-(s[i]=='}');i+=1
 return s[start:i]
awake=method(a/'TitleFlow.Resources.cs','private void Awake()')
bind=method(a/'TitleFlow.LoginGui.cs','internal void BindLoginInputForIndependentRuntime(')
publish=method(p/'ClientRootView.TitleResources.cs','internal Action PublishTitleOwnerForIndependentRuntime(')
login=method(a/'TitleFlow.AccountConnections.cs','private void BeginLogin(')
notice=method(a/'TitleFlow.LoginGui.cs','private void DrawNoticeButton()')
bootstrap=(r/'Bootstrap/ClientBootstrap.cs').read_text()
boot=bootstrap[bootstrap.index('            PrivateServerOptions options ='):bootstrap.index('            InitializeClient(options, installer);')]
fields=','.join(sorted(set(re.findall(r'^        (\w+) = L\(',awake,re.M))))
h=r'''
using System;using System.IO;using System.Collections;using PrivateServerOptions=FakeFantasyOnline.Protocol.PrivateServerOptions;using UnityEngine;using FakeFantasyOnline.Presentation;using FakeFantasyOnline.Presentation.ReferenceAssets;
class ClientResourcePaths{public static string InstalledRoot="unused";}
class Installer{public bool ResourceFree=true;public string DispatchUrl="https://release.example.test/dispatch.json";}
namespace UnityEngine {
 public class Application{public static string persistentDataPath,streamingAssetsPath;public static bool isMobilePlatform=false;}
 public class Texture2D{} public class GUIStyle{} public struct Rect{public Rect(float a,float b,float c,float d){}}
 public static class GUI{public static bool Button(Rect r,string s,GUIStyle g)=>true;}
 public static class Time{public static float realtimeSinceStartup=5;}
 public class MonoBehaviour {public static ClientRootView Host;public T GetComponent<T>() where T:class=>Host as T;public void StartCoroutine(IEnumerator e){int n=0;while(e.MoveNext())if(++n>5)throw new Exception("coroutine loop");}}
 public class TextAsset{public string text;}
 public static class Resources{public static T Load<T>(string s)where T:class=>null;}
 public static class JsonUtility{public static T FromJson<T>(string s)where T:new()=>new T();}
}
namespace UnityEngine.Networking {
 public class UnityWebRequest:IDisposable {public static int Gets;public static string Last;public int timeout;public enum Result{Success}public Result result;public Handler downloadHandler=new Handler();public class Handler{public string text="{}";}
 public static UnityWebRequest Get(string u){if(string.IsNullOrEmpty(u))throw new Exception("empty HTTP URL");Gets++;Last=u;return new UnityWebRequest();}public object SendWebRequest()=>null;public void Dispose(){} }
}
namespace FakeFantasyOnline.Presentation {
 public partial class ClientRootView {
 internal Uri _noticeUri; internal RecoveredTitleCharacterCreationSurface _titleCharacterCreation; internal TitleFlow _titleLogin;internal RecoveredTitleCharacterSurface _titleCharacter; internal Action _titleAwakeTailForIndependentRuntime;public bool _loginDiagnosticsActive;
 void PerformCharacterSelectionAction(){}
 __PUBLISH__
 }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 internal class RecoveredTitleCharacterCreationSurface{public void Reset(){}}
 internal class RecoveredTitleCharacterSurface{public RecoveredTitleCharacterSurface(TitleFlow t){}}
 public class Changelog{public string[] notes;}
 public static partial class NoticeBoard{public static bool Loaded;public static int Published;public static void PublishJson(string s){Published++;}}
 internal class NetClient{public static NetClient I;public int Connects,Sends;public void ConnectTitleLoginForIndependentRuntime(Action<bool> cb){Connects++;cb(true);}public void SendTitleLoginForIndependentRuntime(string a,string p){Sends++;}}
 internal sealed partial class TitleFlow:MonoBehaviour {
 ClientRootView guiHost;RecoveredTitleCharacterCreationSurface creationGui;TitleFlow pluginTitleOwner;internal string netMsg;internal Changelog _changelog;
 Texture2D __FIELDS__;
 Texture2D[] typebtn=new Texture2D[6],typebtnSel=new Texture2D[6],colorbtn=new Texture2D[9],colorbtnSel=new Texture2D[9],classBody=new Texture2D[5];
 Texture2D[,] classpic=new Texture2D[6,2];Texture2D[,,] nudeAv=new Texture2D[2,9,3],hairHead=new Texture2D[2,9,3];
 bool loggingIn,_rememberAccount,_rememberPassword,_noticeOpen,_changelogTried;float loginAtMs,_noticeScroll;string password="test";const int ButtonWidth=1,ButtonHeight=1;GUIStyle _regButtonStyle=new GUIStyle();
 Texture2D L(string s)=>new Texture2D();Texture2D LoadAvatarForIndependentRuntime(string s)=>new Texture2D();
 internal void BindHostForIndependentRuntime(Action reset,Action select,Action<string> message){}
 void SaveLoginPrefs(){}void PublishPluginTitleResult(string s){}void LoadBakedChangelog(){}
 __BIND__
 __AWAKE__
 __LOGIN__
 __NOTICE__
 internal void Run(){Awake();if(guiHost==null||creationGui==null||pluginTitleOwner!=this||btnLogin==null||btnComplete==null||btnReturn==null)throw new Exception("login UI did not initialize");BeginLogin("test",false);DrawNoticeButton();NoticeBoard.Ensure(this);}
 }
}
class Test {
 static PrivateServerOptions Boot(Installer installer){__BOOT__ return options;}
 static void Check(bool x,string s){if(!x)throw new Exception(s);}
 static void Main(){var dir=Path.Combine(Path.GetTempPath(),"title-notice-"+Guid.NewGuid());Directory.CreateDirectory(dir);UnityEngine.Application.persistentDataPath=dir;UnityEngine.Application.streamingAssetsPath=Path.Combine(dir,"absent");File.WriteAllText(Path.Combine(dir,"server.json"),"{\"host\":\"127.0.0.1\",\"port\":19000}");var options=Boot(new Installer());Directory.Delete(dir,true);Check(options.LocalServer!=null&&options.NoticeUri==null,"real local options");int tails=0;var host=new ClientRootView{_noticeUri=options.NoticeUri,_titleAwakeTailForIndependentRuntime=()=>{tails++;if(NetClient.I==null)NetClient.I=new NetClient();}};MonoBehaviour.Host=host;NoticeBoard.ConfigureEndpointForIndependentRuntime(options.NoticeUri?.AbsoluteUri);var t=new TitleFlow();t.Run();Check(tails==1&&host._titleCharacter!=null,"Awake tail/network setup/character surface not reached");Check(NetClient.I.Connects==1&&NetClient.I.Sends==1,"login action unavailable");Check(UnityEngine.Networking.UnityWebRequest.Gets==0,"local mode made announcement HTTP request");var iter=t.FetchChangelog();Check(!iter.MoveNext()&&!iter.MoveNext(),"null iterator completion");((IDisposable)iter).Dispose();
 host._noticeUri=new Uri("https://release.example.test/changelog.json");var online=new TitleFlow();online.Run();Check(tails==2,"normal Awake tail");Check(UnityEngine.Networking.UnityWebRequest.Gets==1,"normal title announcement disabled");Check(UnityEngine.Networking.UnityWebRequest.Last==host._noticeUri.AbsoluteUri,"endpoint mutated");Check(NetClient.I.Connects==2&&NetClient.I.Sends==2,"normal login unavailable");Console.WriteLine("PASS production UI Awake -> PublishTitleOwner -> BindLoginInput -> asset initialization -> completion tail -> BeginLogin; null announcement button/iterators make zero requests; remote announcement and login retained (Unity/network doubles, not Player)");}
}
'''
for key,value in [('BOOT',boot),('PUBLISH',publish),('FIELDS',fields),('BIND',bind),('AWAKE',awake),('LOGIN',login),('NOTICE',notice)]:h=h.replace('__'+key+'__',value)
fw=Path(os.environ['FFO_FRAMEWORK_DIR']);mono=os.environ['FFO_MONO'];csc=os.environ['FFO_CSC']
with tempfile.TemporaryDirectory(prefix='local-title-contract-') as d:
 d=Path(d);(d/'Test.cs').write_text(h)
 subprocess.run([mono,csc,'-nologo','-noconfig','-nostdlib+']+['-r:'+str(fw/n) for n in ['mscorlib.dll','System.dll','System.Core.dll']]+['-out:'+str(d/'Test.exe'),str(d/'Test.cs'),str(a/'TitleFlow.Changelog.cs'),str(a/'NoticeBoard.Lifecycle.cs')]+[str(r/f) for f in ['Protocol/PrivateServerOptions.cs','Foundation/BoundedJson.cs','Foundation/JsonData.cs','Application/ClientConfig.cs']],check=True)
 subprocess.run([mono,str(d/'Test.exe')],check=True,timeout=20)

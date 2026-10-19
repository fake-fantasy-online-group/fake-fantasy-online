using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using FakeFantasyOnline.Bootstrap;
using FakeFantasyOnline.Foundation;
using UnityEngine;
internal static class ClientPackageUiContract
{
    private static int checks;
    private static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly byte[] Bytes = Encoding.UTF8.GetBytes("synthetic inert package bytes, not an installer");
    private static void Check(bool value, string text) { if (!value) throw new Exception(text); checks++; }
    private static void Set(object o, string name, object value) => o.GetType().GetField(name, Hidden).SetValue(o, value);
    private static T Get<T>(object o, string name) => (T)o.GetType().GetField(name, Hidden).GetValue(o);
    private static object Invoke(object o, string name, params object[] args) => o.GetType().GetMethod(name, Hidden).Invoke(o, args);
    private static void Draw(MobileResourceInstaller page, string click = null)
    { GUI.Click = click; GUI.NextText = null; GUI.Labels.Clear(); Invoke(page, "OnGUI"); }
    private static string Platform {
        get {
#if UNITY_ANDROID
            return "android";
#elif UNITY_STANDALONE_WIN
            return "windows";
#elif UNITY_STANDALONE_OSX
            return "macos";
#else
            return "linux";
#endif
        }
    }
    private static bool Desktop => Platform == "windows" || Platform == "macos";
    private static string HandoffLabel => Platform == "android" ? "验证并准备安装" : Desktop ? "验证并准备桌面更新" : "打开安装包目录";
    private static ClientUpdatePlan Plan(bool required = true, string version = "2.0.0")
    {
        string hash;
        using (var sha=System.Security.Cryptography.SHA256.Create()) hash=BitConverter.ToString(sha.ComputeHash(Bytes)).Replace("-", "").ToLowerInvariant();
        string extension=Platform=="android"?"apk":Desktop?"zip":"AppImage";
        string json="{\"servers\":[{}],\"update\":{\"schemaVersion\":1,\"resourceManifestUrl\":\"https://example.test/res.tsv\",\"clients\":{\""+Platform+"\":{\"version\":\""+version+"\",\"minVersion\":\""+(required?"2.0.0":"1.0.0")+"\",\"artifactUrl\":\"https://example.test/client."+extension+"\",\"sha256\":\""+hash+"\",\"sizeBytes\":"+Bytes.Length+"}}}}";
        return ClientUpdatePlan.Parse(json,Platform,"1.0.0",false);
    }
    private static MobileResourceInstaller Page(bool required=true)
    {
        var page=new MobileResourceInstaller();
        Set(page,"resourceFree",true); Set(page,"awaitingChoice",true);Set(page,"updatePlan",Plan(required));Set(page,"dispatchUrl","https://example.test/dispatch.json");
        Set(page,"ready",!required);Set(page,"verifiedRoot",required?null:"/tmp/existing-resource");
        return page;
    }
    private static void Run(MobileResourceInstaller page)
    {
        var e=(IEnumerator)Invoke(page,"ProcessClientPackage",new MobileResourceBootstrapConfig{appVersion="1.0.0"},new FakeTransport());
        int iterations=0;
        while(e.MoveNext()) { if(++iterations>10000) throw new Exception("worker timed out");Thread.Sleep(1); }
    }
    private sealed class FakeTransport:IMobileResourceTransport
    {
        public void Download(Uri uri,string file,long maximum,CancellationToken token,Action<long> progress)
        { token.ThrowIfCancellationRequested();progress?.Invoke(0);File.WriteAllBytes(file,Bytes);progress?.Invoke(Bytes.Length); }
    }
    private static void TestRealDispatchLoop(string root)
    {
        Application.persistentDataPath = root;
        var config = new MobileResourceBootstrapConfig { appVersion = "1.0.0", dispatchUrl = "" };
        var plan = Plan();
        var cached = new ClientArtifactDownload(Path.Combine(root, "ClientPackages"), "1.0.0", Platform, false, new FakeTransport(), p => long.MaxValue);
        string expected = cached.Download(plan.Artifact, CancellationToken.None);
        var page = new MobileResourceInstaller();
        var loop = (IEnumerator)Invoke(page, "InstallRemote", config);
        Check(loop.MoveNext() && Get<bool>(page, "awaitingChoice"), "actual dispatch loop awaits input without network");
        Set(page, "configurationOpen", false); Set(page, "updatePlan", plan);
        Draw(page, "下载客户端");
        int iterations = 0;
        do { if (!loop.MoveNext() || ++iterations > 10000) throw new Exception("real dispatcher did not return to choice"); Thread.Sleep(1); }
        while (Get<string>(page, "verifiedClientPackage") == null || !Get<bool>(page, "awaitingChoice"));
        Check(Get<string>(page, "verifiedClientPackage") == expected, "real dispatcher routes package download and returns to idle choice");
        Check(!Get<bool>(page, "ready") && Get<CancellationTokenSource>(page, "remoteCancellation") == null, "real dispatcher releases worker and keeps mandatory gate closed");
        ClientResourcePaths.UseBundled();
        Set(page,"ready",true);Set(page,"verifiedRoot",Path.Combine(root,"resources"));
        Set(page,"verifiedDispatchUrl",Get<string>(page,"dispatchUrl").Trim());Set(page, "enterRequested", true);
        Check(loop.MoveNext() && !Get<bool>(page, "enterRequested") && ClientResourcePaths.InstalledRoot==null,
            "required minVersion blocks entry even with an otherwise ready matching resource root");
        var missingResources = ClientUpdatePlan.Parse("{\"servers\":[{}],\"update\":{\"schemaVersion\":1,\"resourceManifests\":{}}}", Platform, "1.0.0", false);
        Set(page,"updatePlan",missingResources); Set(page,"enterRequested",true);
        Check(loop.MoveNext() && !Get<bool>(page,"enterRequested") && ClientResourcePaths.InstalledRoot == null,
            "unmatched exact resource map blocks entry even with a stale ready resource root");
        Set(page,"updatePlan",plan); Set(page,"ready",false);
        string caption = HandoffLabel;
        int prepares = AndroidClientPackageInstaller.Prepares, opens = Application.Opened.Count;
        Draw(page, caption); iterations = 0;
        do { if (!loop.MoveNext() || ++iterations > 10000) throw new Exception("real dispatcher handoff did not return to choice"); Thread.Sleep(1); }
        while (Get<bool>(page, "clientPackageHandoffRequested") || Get<CancellationTokenSource>(page, "remoteCancellation") != null || !Get<bool>(page, "awaitingChoice"));
        Check(Platform == "android" ? AndroidClientPackageInstaller.Prepares == prepares + 1 : Desktop ? Get<string>(page,"desktopState") == "manual" && Application.Opened.Count == opens : Application.Opened.Count == opens + 1, "real dispatcher routes explicit cached handoff exactly once; absent desktop helper fails safely");
        (loop as IDisposable)?.Dispose();
    }
    public static void Main()
    {
        string root=Path.Combine(Path.GetTempPath(),"client-package-ui-"+Guid.NewGuid().ToString("N"));Application.persistentDataPath=root;Application.version="1.0.0";
        try
        {
            var page=Page(); Draw(page,"下载客户端");Check(Get<bool>(page,"clientPackageRequested"),"mandatory upgrade queues download");
            Check(Application.Opened.Count==0 && AndroidClientPackageInstaller.Prepares==0,"download does not open URL or start installer");
            Run(page);string path=Get<string>(page,"verifiedClientPackage");
            Check(path!=null&&File.Exists(path),"download produces verified file");
            Check(!Get<bool>(page,"ready")&&!Get<bool>(page,"enterRequested"),"download completion never bypasses required upgrade");
            Check(AndroidClientPackageInstaller.Prepares==0&&AndroidClientPackageInstaller.Begins==0,"download completion waits for explicit install click");
            string caption=HandoffLabel;
            Draw(page,caption);Check(Get<bool>(page,"clientPackageHandoffRequested"),"handoff click queues reverify");Run(page);
            if(Platform=="android")
            {
                Check(AndroidClientPackageInstaller.Prepares==1&&AndroidClientPackageInstaller.Begins==0,"handoff only prepares identity verification");
                Check(AndroidClientPackageInstaller.Path==path&&AndroidClientPackageInstaller.Version=="2.0.0","prepare binds exact verified file/version");
                AndroidClientPackageInstaller.State=AndroidClientPackageInstallState.Ready;Invoke(page,"PollAndroidPackage",true);
                Check(AndroidClientPackageInstaller.Begins==0,"ready state does not automatically begin install");
                Draw(page,"继续系统安装");Check(AndroidClientPackageInstaller.Begins==1,"explicit begin enters system staging");
                Draw(page,"继续系统安装");Check(AndroidClientPackageInstaller.Begins==1,"repeated begin cannot replay while staging");
                Draw(page,"取消安装准备");Check(AndroidClientPackageInstaller.Cancels==1,"cancel invokes system cancellation");
                AndroidClientPackageInstaller.State=AndroidClientPackageInstallState.PermissionRequired;Invoke(page,"PollAndroidPackage",true);
                Draw(page,"允许安装此来源");Check(AndroidClientPackageInstaller.Settings==1&&AndroidClientPackageInstaller.Begins==1,"permission only opens settings");
                AndroidClientPackageInstaller.State=AndroidClientPackageInstallState.Ready;Invoke(page,"PollAndroidPackage",true);
                Check(AndroidClientPackageInstaller.Begins==1,"permission return never auto installs");
                AndroidClientPackageInstaller.State=AndroidClientPackageInstallState.AwaitingConfirmation;Invoke(page,"PollAndroidPackage",true);
                Check(AndroidClientPackageInstaller.Confirmations==0,"system confirmation is not auto launched");Draw(page,"打开系统安装确认");
                Check(AndroidClientPackageInstaller.Confirmations==1,"explicit system confirmation launched");
                Draw(page,"等待系统安装结果");Draw(page,"配置更新地址");Draw(page,"修复资源");
                Check(AndroidClientPackageInstaller.Begins==1&&!Get<bool>(page,"configurationOpen")&&!Get<bool>(page,"repairRequested"),"pending result disables replay and reconfiguration");
                AndroidClientPackageInstaller.State=AndroidClientPackageInstallState.Failed;Invoke(page,"PollAndroidPackage",true);Draw(page);
                Check(Get<bool>(page,"failed")&&!Get<bool>(page,"ready"),"failed system install never declares app ready");
                Check(GUI.Labels.Exists(x=>x.text.Contains("安装未完成")&&x.color.r==1&&x.color.g<.5f),"install failure uses red safe summary");
                AndroidClientPackageInstaller.State=AndroidClientPackageInstallState.Succeeded;Invoke(page,"PollAndroidPackage",true);
                int quits=Application.Quits;Draw(page,"退出后手动重启");
                Check(Application.Quits==quits+1&&!Get<bool>(page,"ready")&&!Get<bool>(page,"enterRequested"),"reported system success still requires restart, not entry in old process");
            }
            else if (Desktop)
            {
                Check(Get<string>(page,"desktopState") == "manual" && Get<bool>(page,"failed"), "missing marked install/helper offers explicit manual fallback");
                Check(Application.Opened.Count == 0 && Application.Quits == 0, "desktop prepare failure neither opens package nor exits game");
                Draw(page,"打开安装包目录");
                Check(Application.Opened.Count == 1 && Application.Opened[0].StartsWith("file:") && !Application.Opened[0].EndsWith(".zip"), "desktop fallback opens only directory on explicit click");
            }
            else
            {
                Check(Application.Opened.Count==1,"desktop explicit handoff requests folder opening");
                Check(Application.Opened[0].StartsWith("file:")&&!Application.Opened[0].EndsWith(".AppImage"),"desktop opens directory, never package executable");
                Check(AndroidClientPackageInstaller.Prepares==0,"desktop never calls Android installer");
            }
            // A package changed after first verification must never reach platform preparation.
            Set(page,"androidPackageActive",false);Set(page,"clientPackageHandoffRequested",true);
            File.WriteAllBytes(path,new byte[Bytes.Length]);int oldPrepares=AndroidClientPackageInstaller.Prepares;int oldOpened=Application.Opened.Count;Run(page);
            Check(Get<bool>(page,"failed")&&Get<string>(page,"verifiedClientPackage")==null,"tampered cached file fails revalidation");
            Check(AndroidClientPackageInstaller.Prepares==oldPrepares&&Application.Opened.Count==oldOpened,"invalid file never reaches OS handoff");
            Check(Get<string>(page,"conciseError").Contains("客户端安装包校验失败"),"package failure identifies the installer instead of game resources");
            page=Page(false);Draw(page,"下载新客户端 v2.0.0");Check(Get<bool>(page,"clientPackageMode")&&Get<bool>(page,"clientPackageRequested"),"optional upgrade enters explicit package mode");
            Set(page,"failed",true);Draw(page,"返回当前版本");
            Check(!Get<bool>(page,"clientPackageMode")&&!Get<bool>(page,"clientPackageRequested")&&!Get<bool>(page,"clientPackageHandoffRequested")&&Get<bool>(page,"ready")&&!Get<bool>(page,"failed"),"optional update can return to compatible ready resources");
            page=Page();Set(page,"verifiedClientPackage","/tmp/fake");Set(page,"verifiedClientIdentity","stale");Draw(page,"下载客户端");
            Check(Get<bool>(page,"clientPackageRequested")&&!Get<bool>(page,"clientPackageHandoffRequested"),"changed metadata cannot use stale verified path");
            var token=new CancellationTokenSource();Set(page,"remoteCancellation",token);Set(page,"awaitingChoice",false);Set(page,"downloadBytesPerSecond",(double?)1000);Draw(page,"暂停更新");
            Check(token.IsCancellationRequested&&Get<double?>(page,"downloadBytesPerSecond")==null,"package pause cancels transfer and rate");
            token.Dispose();token=new CancellationTokenSource();Set(page,"remoteCancellation",token);Set(page,"downloadBytesPerSecond",(double?)4000);
            Invoke(page,"OnApplicationPause",true);Check(token.IsCancellationRequested&&Get<double?>(page,"downloadBytesPerSecond")==null,"background pauses download and clears rate");
            Set(page,"remoteCancellation",null);int cancelBefore=AndroidClientPackageInstaller.Cancels;Set(page,"androidPackageActive",true);
            Invoke(page,"OnApplicationPause",true);Check(AndroidClientPackageInstaller.Cancels==cancelBefore,"permission/installer activity pause does not cancel platform session");
            TestRealDispatchLoop(Path.Combine(root, "dispatcher"));
            Console.WriteLine("PASS client package UI ("+Platform+"): "+checks+" checks; actual gate, synthetic transport and platform substitutes, no Unity/device/installer execution.");
        }
        finally { if(Directory.Exists(root))Directory.Delete(root,true); }
    }
}
namespace FakeFantasyOnline.Bootstrap
{
    public enum AndroidClientPackageInstallState { Unsupported,Idle,Preparing,Ready,PermissionRequired,Staging,AwaitingConfirmation,AwaitingResult,Succeeded,Cancelled,Failed,Interrupted }
    public sealed class AndroidClientPackageInstallSnapshot {public AndroidClientPackageInstallState State;public string Message="synthetic status";public float Progress=-1;}
    public static class AndroidClientPackageInstaller
    {
        public static int Prepares,Begins,Settings,Confirmations,Cancels;public static string Path,Version;
        public static AndroidClientPackageInstallState State=AndroidClientPackageInstallState.Idle;
        public static void Prepare(string path,string version,string sha,long bytes){Prepares++;Path=path;Version=version;State=AndroidClientPackageInstallState.Preparing;}
        public static void Begin(){Begins++;State=AndroidClientPackageInstallState.Staging;}
        public static AndroidClientPackageInstallSnapshot Poll()=>new AndroidClientPackageInstallSnapshot{State=State};
        public static void OpenPermissionSettings(){Settings++;}
        public static void OpenSystemConfirmation(){Confirmations++;State=AndroidClientPackageInstallState.AwaitingResult;}
        public static void Cancel(){Cancels++;State=AndroidClientPackageInstallState.Cancelled;}
    }
}

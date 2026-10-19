using System;
using System.IO;
using FakeFantasyOnline.Foundation;
internal static class ClientUpdatePlanContract
{
    private static int count;
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); count++; }
    private static void Reject(Action action, string name)
    { try { action(); } catch (Exception e) when (e is InvalidDataException || e is FormatException) { count++; return; } throw new Exception("Accepted " + name); }
    private static string Plan(string version="1.0.0", string min="0.1.0", string extra="")
    { return "{\"servers\":[{\"host\":\"192.168.1.3\",\"port\":8080}],\"update\":{\"schemaVersion\":1,\"resourceManifestUrl\":\"https://updates.example.test/mobile-resources.tsv\",\"clients\":{\"linux\":{\"version\":\""+version+"\",\"minVersion\":\""+min+"\",\"artifactUrl\":\"https://updates.example.test/client.zip\",\"sha256\":\""+new string('a',64)+"\",\"sizeBytes\":1024}}"+extra+"}}"; }
    private static ClientUpdatePlan Read(string text, string current="0.1.0", bool http=false)
    { return ClientUpdatePlan.Parse(text,"linux",current,http); }
    public static int Main()
    {
        var p=Read(Plan()); Check(p.Available&&!p.Required,"optional upgrade");
        Check(Read(Plan("1.0.0","1.0.0")).Required,"minimum blocks");
        Check(!Read(Plan(),"1.0.0").Available,"same version");
        Check(!Read(Plan(),"2.0.0").Required&&!Read(Plan(),"2.0.0").Available,"older publication never downgrades");
        Check(ClientProductVersion.Compare("1.10.0","1.9.0")>0,"numeric SemVer");
        Check(ClientProductVersion.Compare("4294967295.0.0","2.0.0")>0,"uint32 SemVer");
        foreach(string invalid in new[]{"1.0","v1.0.0","01.0.0","1.0.0-rc.1","1.0.0+build","1.0.-1","4294967296.0.0"})
            Reject(()=>Read(Plan(invalid)),"bad version "+invalid);
        Reject(()=>Read(Plan("1.0.0","2.0.0")),"min after latest");
        Reject(()=>Read("{\"servers\":[]}"),"missing update and servers");
        Reject(()=>Read(Plan().Replace("\"schemaVersion\":1","\"schemaVersion\":2")),"schema");
        Reject(()=>Read(Plan().Replace("\"schemaVersion\":1","\"schemaVersion\":1,\"schemaVersion\":1")),"duplicate key");
        Reject(()=>Read(Plan().Replace("\"sizeBytes\":1024","\"sizeBytes\":0")),"zero artifact");
        Reject(()=>Read(Plan().Replace("\"sizeBytes\":1024","\"sizeBytes\":1024.0")),"fraction artifact");
        Reject(()=>Read(Plan(extra:",\"resourceManifestSizeBytes\":100")),"unpaired size");
        Reject(()=>Read(Plan(extra:",\"resourceManifestSha256\":\""+new string('a',64)+"\"")),"unpaired hash");
        p=Read(Plan(extra:",\"resourceManifestSizeBytes\":100,\"resourceManifestSha256\":\""+new string('a',64)+"\""));
        Check(p.ResourceManifestSizeBytes==100,"paired pins");
        foreach(string endpoint in new[]{"http://example.test/a","https://localhost./a","https://a.localhost/a","https://127.0.0.1/a","https://example.test:0/a","https://user:pass@example.test/a","https://example.test/a?secret=x","https://example.test/a#frag","file:///tmp/a","https://10.01.1.2/a","https://[fe80::1%25eth0]/a"})
            Reject(()=>Read(Plan().Replace("https://updates.example.test/mobile-resources.tsv",endpoint),http:true),"unsafe URL");
        p=Read(Plan().Replace("https://updates.example.test/mobile-resources.tsv","http://192.168.1.3/a"),http:true);
        Check(p.ResourceManifestUrl.StartsWith("http:"),"explicit LAN http");
        Reject(()=>Read(Plan().Replace("https://updates.example.test/mobile-resources.tsv","http://192.168.1.3/a")),"production HTTP");
        ClientResourcePaths.UseBundled(); Check(ClientResourcePaths.Resolve("/builtin",false)=="/builtin","explicit desktop bundled");
        ClientResourcePaths.Activate(Path.GetFullPath("installed"));
        Check(ClientResourcePaths.Resolve("/builtin",false)==Path.GetFullPath("installed"),"desktop installed root");
        Check(ClientResourcePaths.Resolve("jar://builtin",true)==Path.GetFullPath("installed"),"android installed root");
        ClientUpdateSession.Publish(p);Check(ClientUpdateSession.UnifiedGate,"session");ClientUpdateSession.Reset();Check(ClientUpdateSession.Plan==null,"reset");
        string dir=Path.Combine(Path.GetTempPath(),"update-address-"+Guid.NewGuid().ToString("N"));
        try
        {
            Check(ClientUpdateAddressStore.Read(dir)=="","missing saved address");
            ClientUpdateAddressStore.Save(dir,"https://updates.example.test/dispatch.json",false);
            Check(ClientUpdateAddressStore.Read(dir)=="https://updates.example.test/dispatch.json","saved address");
            ClientUpdateAddressStore.Save(dir,"https://updates.example.test/v2.json",false);
            Check(ClientUpdateAddressStore.Read(dir)=="https://updates.example.test/v2.json","atomic replacement");
            Reject(()=>ClientUpdateAddressStore.Save(dir,"https://user:secret@example.test/a",false),"credentials not persisted");
            Check(ClientUpdateAddressStore.Read(dir)=="https://updates.example.test/v2.json","old config preserved");
            Reject(()=>ClientUpdateAddressStore.Save(dir,"https://updates.example.test/"+new string('中',1400),false),"byte limit");
            Check(Directory.GetFiles(dir).Length==1,"no completed temp files");
        }
        finally { if(Directory.Exists(dir)) Directory.Delete(dir,true); }
        var rate = new ClientDownloadRate();
        rate.Observe("a", 1000000, 1, true); Check(!rate.BytesPerSecond(1).HasValue,"range baseline not downloaded");
        rate.Observe("a", 1001024, 1.5, true); Check(rate.BytesPerSecond(1.5)==2048,"real byte delta / elapsed");
        rate.Observe("a", 1002048, 2, true); Check(rate.BytesPerSecond(2)==2048,"rolling transfer sample");
        Check(rate.BytesPerSecond(4)==0,"stalled network zero speed");
        rate.Observe("b", 5000000, 4, true); Check(!rate.BytesPerSecond(4).HasValue,"new file resets rate");
        rate.Observe("b", 5001024, 4.5, true); Check(rate.BytesPerSecond(4.5)==2048,"new file baseline excludes old data");
        rate.Observe("b", 0, 5, true); Check(!rate.BytesPerSecond(5).HasValue,"200 restart resets offset");
        rate.Observe("b", 1024, 5.5, true); Check(rate.BytesPerSecond(5.5)==2048,"restart measures only fresh bytes");
        rate.Observe("b", 999999, 6, false); Check(!rate.BytesPerSecond(6).HasValue,"hash/copy not download rate");
        rate.Observe("c", 0, 7, true);rate.Observe("c", 4096, 7.5, true);rate.Reset();
        Check(!rate.BytesPerSecond(7.5).HasValue,"pause/failure/address reset");
        rate.Observe("c", 0, 8, true);rate.Observe("c", 1024, 7, true);
        Check(!rate.BytesPerSecond(7).HasValue,"monotonic clock rewind resets");
        Console.WriteLine("PASS unified update contract ("+count+" assertions; no Unity runtime)");return 0;
    }
}

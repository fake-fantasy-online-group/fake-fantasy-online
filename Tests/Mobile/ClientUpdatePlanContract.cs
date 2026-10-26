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

    private static string ResourceEntry(string url="https://updates.example.test/android/1.0.0/manifest.tsv", string hash=null, long size=123)
    { return "{\"manifestUrl\":\""+url+"\",\"sha256\":\""+(hash ?? new string('B',64))+"\",\"sizeBytes\":"+size+"}"; }
    private static string MappedPlan(string map, bool legacy=true, string minimum="1.0.0", string platform="android")
    {
        string source=Plan("2.0.0",minimum).Replace("\"linux\":", "\""+platform+"\":");
        if (!legacy) source=source.Replace("\"resourceManifestUrl\":\"https://updates.example.test/mobile-resources.tsv\",", "");
        return source.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"resourceManifests\":"+map);
    }
    private static void ResourceManifestMaps()
    {
        string entry=ResourceEntry();
        string single="{\"android\":{\"1.0.0\":"+entry+"}}";
        var all = new System.Text.StringBuilder("{");
        foreach (string platform in new[]{"android","macos","windows","linux"})
        {
            if (all.Length>1) all.Append(',');
            all.Append("\"").Append(platform).Append("\":{\"1.0.0\":")
                .Append(ResourceEntry("https://updates.example.test/"+platform+"/1.0.0/manifest.tsv"))
                .Append(",\"1.1.0\":")
                .Append(ResourceEntry("https://updates.example.test/"+platform+"/1.1.0/manifest.tsv",new string('c',64),456)).Append('}');
        }
        all.Append('}');
        foreach (string platform in new[]{"android","macos","windows","linux"})
        {
            foreach (string version in new[]{"1.0.0","1.1.0"})
            {
                var plan=ClientUpdatePlan.Parse(MappedPlan(all.ToString(),platform:platform),platform,version,false);
                Check(plan.HasCompatibleResources,"exact map has resources "+platform+" "+version);
                Check(plan.ResourceManifestUrl=="https://updates.example.test/"+platform+"/"+version+"/manifest.tsv","exact map URL "+platform+" "+version);
                Check(plan.ResourceManifestSha256==new string(version=="1.0.0"?'b':'c',64),"mapped hash normalized");
                Check(plan.ResourceManifestSizeBytes==(version=="1.0.0"?123:456),"mapped size selected");
            }
            // Missing resources must preserve the mandatory package upgrade gate.
            var required=ClientUpdatePlan.Parse(MappedPlan(single,minimum:"2.0.0",platform:platform),platform,"0.1.0",false);
            Check(required.Required && required.Available && required.Artifact.Version=="2.0.0","missing resource retains mandatory package "+platform);
            Check(!required.HasCompatibleResources && required.ResourceManifestUrl==null && required.ResourceManifestSha256==null && required.ResourceManifestSizeBytes==0,"no legacy/other platform/version fallback "+platform);
        }
        Check(ClientUpdatePlan.Parse(MappedPlan(single,false),"android","1.0.0",false).HasCompatibleResources,"map-only publication");
        Check(ClientUpdatePlan.Parse(MappedPlan(single).Replace("\"resourceManifestUrl\":\"https://updates.example.test/mobile-resources.tsv\"", "\"resourceManifestUrl\":\"https://updates.example.test/mobile-resources.tsv\",\"resourceManifestSha256\":\""+new string('d',64)+"\",\"resourceManifestSizeBytes\":456"),"android","1.0.0",false).ResourceManifestSha256==new string('b',64),"map pins override valid legacy pins");
        foreach (string map in new[]{"{}","{\"android\":{}}",single})
        {
            var plan=ClientUpdatePlan.Parse(MappedPlan(map),"android","1.2.0",false);
            Check(!plan.HasCompatibleResources && plan.ResourceManifestUrl==null,"empty or missing exact version never falls back");
            Check(plan.Available && !plan.Required,"optional artifact preserved without resources");
        }
        Check(!ClientUpdatePlan.Parse(MappedPlan(single),"macos","1.0.0",false).HasCompatibleResources,"same version on wrong platform cannot match");
        Check(!ClientUpdatePlan.Parse(MappedPlan(single),"android","2.0.1",false).Available,"unpublished newer app never downgrades");
        Check(Read(Plan()).HasCompatibleResources,"legacy unpinned resource regression");
        Reject(()=>Read(Plan().Replace("\"resourceManifestUrl\":\"https://updates.example.test/mobile-resources.tsv\",","")),"missing legacy URL without map");
        foreach (string map in new[]{"null","[]","true","{\"android\":null}","{\"android\":[]}","{\"Android\":{}}","{\"any\":{}}","{\"android\":{\"1.0.0\":null}}","{\"android\":{\"1.0.0\":[]}}","{\"android\":{},\"android\":{}}","{\"android\":{\"1.0.0\":"+entry+",\"1.0.0\":"+entry+"}}"})
            Reject(()=>ClientUpdatePlan.Parse(MappedPlan(map),"android","1.0.0",false),"invalid map shape/key "+map);
        foreach (string version in new[]{"*","latest","v1.0.0","1.0","01.0.0","1.0.0-beta","1.0.0+meta","4294967296.0.0"})
            Reject(()=>ClientUpdatePlan.Parse(MappedPlan("{\"android\":{\""+version+"\":"+entry+"}}"),"android","1.0.0",false),"invalid exact version "+version);
        foreach (string bad in new[]{"{}",entry.Replace("\"manifestUrl\":", "\"ManifestUrl\":"),entry.Replace("\"sha256\":", "\"SHA256\":"),entry.Replace("\"sizeBytes\":123", "\"sizeBytes\":123,\"sizeBytes\":123"),entry.Replace("\"sizeBytes\":123", "\"sizeBytes\":0"),entry.Replace("\"sizeBytes\":123", "\"sizeBytes\":-1"),entry.Replace("\"sizeBytes\":123", "\"sizeBytes\":123.0"),entry.Replace("\"sizeBytes\":123", "\"sizeBytes\":1e2"),entry.Replace("\"sizeBytes\":123", "\"sizeBytes\":null"),entry.Replace("\"sizeBytes\":123", "\"sizeBytes\":8388609"),entry.Replace("\"sizeBytes\":123", "\"sizeBytes\":9223372036854775808"),entry.Replace(",\"sizeBytes\":123", ""),entry.Replace("\"sha256\":\""+new string('B',64)+"\",", ""),entry.Replace(new string('B',64),new string('z',64)),entry.Replace("\"sizeBytes\":123", "\"sizeBytes\":123,\"execute\":true")})
            Reject(()=>ClientUpdatePlan.Parse(MappedPlan("{\"android\":{\"1.0.0\":"+bad+"}}"),"android","1.0.0",false),"invalid mapped entry");
        foreach (string endpoint in new[]{"http://example.test/a","https://127.1/a","https://example.test/a?token=x","https://user:pass@example.test/a","https://example.test/a#x","file:///a","https://example.test/"+new string('x',4096)})
            Reject(()=>ClientUpdatePlan.Parse(MappedPlan(single.Replace("https://updates.example.test/android/1.0.0/manifest.tsv",endpoint)),"android","1.0.0",true),"unsafe mapped URL");
        // Canonical loopback is intentionally allowed only for explicit development
        // builds (Documentation/class-set-aura-visuals.md, private bootstrap policy).
        foreach (string endpoint in new[] { "https://localhost/a", "http://127.0.0.1/a", "http://[::1]/a" })
        {
            string local = MappedPlan(single.Replace("https://updates.example.test/android/1.0.0/manifest.tsv", endpoint));
            Reject(() => ClientUpdatePlan.Parse(local, "android", "1.0.0", false), "production loopback map");
            Check(ClientUpdatePlan.Parse(local, "android", "1.0.0", true).HasCompatibleResources, "explicit development loopback map");
        }
        string lan=MappedPlan(single.Replace("https://updates.example.test/android/1.0.0/manifest.tsv","http://192.168.1.3/a"));
        Reject(()=>ClientUpdatePlan.Parse(lan,"android","1.0.0",false),"mapped LAN HTTP requires opt-in");
        Check(ClientUpdatePlan.Parse(lan,"android","1.0.0",true).HasCompatibleResources,"mapped LAN HTTP opt-in");
        Check(ClientUpdatePlan.Parse(MappedPlan(single.Replace("\"sizeBytes\":123","\"sizeBytes\":8388608")),"android","1.0.0",false).ResourceManifestSizeBytes==8388608,"map 8 MiB bound inclusive");
        Reject(()=>ClientUpdatePlan.Parse(MappedPlan(single.Replace(new string('B',64),new string('z',64))),"windows","1.0.0",false),"invalid nonselected platform still rejected");
        Reject(()=>ClientUpdatePlan.Parse(MappedPlan(single).Replace("https://updates.example.test/mobile-resources.tsv","https://localhost/a"),"android","1.0.0",false),"unused legacy URL still validated");
        Reject(()=>ClientUpdatePlan.Parse(MappedPlan(single)+new string(' ',ClientUpdatePlan.MaxDispatchBytes),"android","1.0.0",false),"mapped dispatch remains bounded");
    }

    private static string Archive(string url="https://updates.example.test/first-install.tar", string hash=null, long size=4096)
    { return "{\"url\":\""+url+"\",\"format\":\"tar\",\"sizeBytes\":"+size+",\"sha256\":\""+(hash ?? new string('D',64))+"\",\"rootPrefix\":\"Assets/StreamingAssets/\"}"; }
    private static string WithArchive(string archive)
    { return ResourceEntry().Replace("\"sizeBytes\":123", "\"sizeBytes\":123,\"archive\":"+archive); }
    private static ClientUpdatePlan ReadArchive(string archive, string platform="android", string version="1.0.0", bool http=false)
    { return ClientUpdatePlan.Parse(MappedPlan("{\"android\":{\"1.0.0\":"+WithArchive(archive)+"}}"),platform,version,http); }
    private static void ResourceArchives()
    {
        string archive=Archive();
        Check(Read(Plan()).ResourceArchive==null,"legacy dispatch has no archive");
        Check(ClientUpdatePlan.Parse(MappedPlan("{\"android\":{\"1.0.0\":"+ResourceEntry()+"}}"),"android","1.0.0",false).ResourceArchive==null,"old mapped dispatch has no archive");
        var plan=ReadArchive(archive);
        Check(plan.ResourceArchive!=null,"new mapped entry selects optional archive");
        Check(plan.ResourceArchive.Url=="https://updates.example.test/first-install.tar","archive URL");
        Check(plan.ResourceArchive.Format=="tar","archive format");
        Check(plan.ResourceArchive.SizeBytes==4096,"archive transfer size");
        Check(plan.ResourceArchive.Sha256==new string('d',64),"archive hash normalized");
        Check(plan.ResourceArchive.RootPrefix=="Assets/StreamingAssets/","fixed archive root");
        Check(plan.ResourceManifestSha256==new string('b',64) && plan.ResourceManifestSizeBytes==123,"archive never replaces manifest pins");
        Check(ReadArchive(archive,version:"1.1.0").ResourceArchive==null,"archive never falls back across app versions");
        Check(ReadArchive(archive,platform:"windows").ResourceArchive==null,"archive never falls back across platforms");
        var required=ReadArchive(archive,version:"0.1.0");
        Check(required.Required && required.Available && required.ResourceArchive==null,"missing resources keeps package gate without archive");
        Check(ReadArchive(Archive(size:MobileResourceArchive.MaxArchiveBytes)).ResourceArchive.SizeBytes==MobileResourceArchive.MaxArchiveBytes,"archive 64 GiB inclusive bound");
        Check(ReadArchive(Archive(size:1)).ResourceArchive.SizeBytes==1,"positive archive metadata size; actual tar validated separately");

        var map=new System.Text.StringBuilder("{");
        foreach (string platform in new[]{"android","windows","macos","linux"})
        {
            if (map.Length>1) map.Append(',');
            map.Append('"').Append(platform).Append("\":{\"1.0.0\":").Append(WithArchive(Archive("https://updates.example.test/"+platform+"/first.tar")))
                .Append(",\"1.1.0\":").Append(WithArchive(Archive("https://updates.example.test/"+platform+"/next.tar",new string('e',64)))).Append('}');
        }
        map.Append('}');
        foreach (string platform in new[]{"android","windows","macos","linux"})
            foreach (string version in new[]{"1.0.0","1.1.0"})
            {
                var selected=ClientUpdatePlan.Parse(MappedPlan(map.ToString()),platform,version,false);
                Check(selected.ResourceArchive.Url=="https://updates.example.test/"+platform+"/"+(version=="1.0.0"?"first":"next")+".tar","archive exact platform/version binding "+platform+" "+version);
                Check(selected.ResourceArchive.Sha256==new string(version=="1.0.0"?'d':'e',64),"archive selected hash");
            }
        string sibling="{\"android\":{\"1.0.0\":"+WithArchive(archive)+",\"1.1.0\":"+ResourceEntry()+"}}";
        Check(ClientUpdatePlan.Parse(MappedPlan(sibling),"android","1.1.0",false).ResourceArchive==null,"entry without archive never inherits sibling transport");

        foreach (string bad in new[]{
            "null","[]","true","\"tar\"","{}",
            archive.Replace("\"url\":","\"Url\":"),
            archive.Replace("\"format\":\"tar\",",""),
            archive.Replace("\"rootPrefix\":\"Assets/StreamingAssets/\"","\"rootPrefix\":null"),
            archive.Replace("\"sizeBytes\":4096,",""),
            archive.Replace("\"sha256\":\""+new string('D',64)+"\",",""),
            archive.Replace("\"url\":\"https://updates.example.test/first-install.tar\",",""),
            archive.Replace(",\"rootPrefix\":\"Assets/StreamingAssets/\"",""),
            archive.Replace("\"sizeBytes\":4096","\"sizeBytes\":0"),
            archive.Replace("\"sizeBytes\":4096","\"sizeBytes\":-1"),
            archive.Replace("\"sizeBytes\":4096","\"sizeBytes\":4096.0"),
            archive.Replace("\"sizeBytes\":4096","\"sizeBytes\":4e3"),
            archive.Replace("\"sizeBytes\":4096","\"sizeBytes\":null"),
            archive.Replace("\"sizeBytes\":4096","\"sizeBytes\":\"4096\""),
            archive.Replace("\"sizeBytes\":4096","\"sizeBytes\":true"),
            archive.Replace("\"sizeBytes\":4096","\"sizeBytes\":68719476737"),
            archive.Replace("\"sizeBytes\":4096","\"sizeBytes\":9223372036854775808"),
            archive.Replace("\"sizeBytes\":4096","\"sizeBytes\":4096,\"sizeBytes\":4096"),
            archive.Replace("\"sizeBytes\":4096","\"sizeBytes\":4096,\"execute\":true"),
            archive.Replace("\"sizeBytes\":4096","\"sizeBytes\":4096,\"entries\":[]"),
            archive.Replace("\"sizeBytes\":4096","\"sizeBytes\":4096,\"manifestSha256\":\""+new string('b',64)+"\""),
            archive.Replace(new string('D',64),new string('z',64)),
            archive.Replace(new string('D',64),new string('a',63)),
            archive.Replace("\"sha256\":\""+new string('D',64)+"\"","\"sha256\":null")
        }) Reject(()=>ReadArchive(bad),"malformed archive descriptor");
        foreach (string format in new[]{"zip","tar.gz","TAR",""," tar"})
            Reject(()=>ReadArchive(archive.Replace("\"format\":\"tar\"","\"format\":\""+format+"\"")),"unsupported archive format "+format);
        foreach (string prefix in new[]{"","Assets/StreamingAssets","assets/StreamingAssets/","/Assets/StreamingAssets/","../","Assets/StreamingAssets/../","Assets/StreamingAssets//","Assets/StreamingAssets/extra/","Assets/StreamingAssets/ "})
            Reject(()=>ReadArchive(archive.Replace("Assets/StreamingAssets/",prefix)),"unsafe or unsupported archive prefix "+prefix);
        foreach (string endpoint in new[]{"http://example.test/a","https://127.1/a","https://example.test/a?token=x","https://user:pass@example.test/a","https://example.test/a#x","file:///a","https://example.test/"+new string('x',4096)})
            Reject(()=>ReadArchive(Archive(endpoint),http:true),"unsafe archive endpoint");
        Reject(() => ReadArchive(Archive("https://localhost/a")), "production loopback archive");
        Check(ReadArchive(Archive("https://localhost/a"), http:true).ResourceArchive != null, "explicit development loopback archive");
        string lan=Archive("http://192.168.1.3/first-install.tar");
        Reject(()=>ReadArchive(lan),"archive LAN HTTP requires explicit development opt-in");
        Check(ReadArchive(lan,http:true).ResourceArchive.Url.StartsWith("http:"),"archive development LAN HTTP opt-in");
        Reject(()=>ReadArchive(archive.Replace("\"format\":\"tar\"","\"format\":\"zip\""),platform:"windows"),"nonselected archive still validated");
        Reject(()=>ClientUpdatePlan.Parse(MappedPlan("{\"android\":{\"1.0.0\":"+WithArchive(archive).Replace("\"sha256\":\""+new string('B',64)+"\",","")+"}}"),"android","1.0.0",false),"archive cannot substitute for required manifest hash");
        Reject(()=>ReadArchive(archive+new string(' ',ClientUpdatePlan.MaxDispatchBytes)),"archive dispatch remains byte bounded");
        // New clients remain backward compatible with old dispatches. Publishing
        // the nested archive field to old strict clients needs a rollout plan;
        // it is not an unknown field those already shipped clients can ignore.
    }

    public static int Main()
    {
        ResourceManifestMaps();
        ResourceArchives();
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
        foreach(string endpoint in new[]{"http://example.test/a","https://localhost./a","https://a.localhost/a","https://example.test:0/a","https://user:pass@example.test/a","https://example.test/a?secret=x","https://example.test/a#frag","file:///tmp/a","https://10.01.1.2/a","https://[fe80::1%25eth0]/a"})
            Reject(()=>Read(Plan().Replace("https://updates.example.test/mobile-resources.tsv",endpoint),http:true),"unsafe URL");
        Reject(() => Read(Plan().Replace("https://updates.example.test/mobile-resources.tsv", "https://127.0.0.1/a")), "production loopback legacy resource");
        Check(Read(Plan().Replace("https://updates.example.test/mobile-resources.tsv", "https://127.0.0.1/a"), http:true).HasCompatibleResources, "explicit development loopback legacy resource");
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

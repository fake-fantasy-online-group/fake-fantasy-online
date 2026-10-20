using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using FakeFantasyOnline.Foundation;

public static class ResourceVerificationPerformanceContract
{
    private static int checks;
    private static void Assert(bool b,string label) { if(!b)throw new Exception(label); checks++; }
    private static void Reject(Action a,string label) { try{a();}catch(Exception e) when(e is IOException || e is InvalidDataException || e is OperationCanceledException){checks++;return;}throw new Exception(label); }
    private static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
    private static readonly byte[] A=Encoding.UTF8.GetBytes("first bytes"),B=Encoding.UTF8.GetBytes("other bytes"),C=new byte[1024*1024];
    private static readonly string[] Paths={"gameassets/a.json","gameassets/b.json","data/c.pkg"};
    private static string Manifest(byte[] b=null,bool reorder=false,string source="https://cdn.example.com/")
    {
        var data=new[]{A,b??B,C}; var s=new StringBuilder("FFO-MOBILE-1\n@client\tandroid\t1.0.0\n");
        foreach(int i in reorder?new[]{2,0,1}:new[]{0,1,2})s.Append("r").Append(i).Append(".bin\t").Append(Paths[i]).Append('\t').Append(data[i].Length).Append('\t').Append(Hash(data[i])).Append('\n');
        return s.ToString();
    }
    private static void Files(string root){for(int i=0;i<3;i++){string p=Path.Combine(root,Paths[i]);Directory.CreateDirectory(Path.GetDirectoryName(p));File.WriteAllBytes(p,new[]{A,B,C}[i]);}}
    private static long VerifyAll(MobileResourceVerification v){long n=0;for(int i=0;i<3;i++){long last=0;Assert(v.Verify(i,b=>last=b),"valid file");n+=last;}v.CommitReceipt();return n;}
    private sealed class Http:IMobileResourceTransport
    {
        public string Text=Manifest();public int Fail=-1;public List<string> Requests=new List<string>();public byte[] Changed;
        public void Download(Uri u,string file,long max,CancellationToken ct,Action<long> progress)
        {
            Requests.Add(u.AbsolutePath);if(u.AbsolutePath.EndsWith("manifest.tsv")){File.WriteAllText(file,Text);return;}
            int i=int.Parse(u.AbsolutePath.Substring(2,1));if(i==Fail)throw new IOException("fixture interrupted");
            var b=i==0?A:i==1?(Changed??B):C;using(var f=File.Create(file))MobileResourceHttpTransport.CopyBounded(new MemoryStream(b),f,max,ct,progress);
        }
    }
    private static MobileRemoteResourceInstall Installer(string root,Http http){return new MobileRemoteResourceInstall(root,"1.0.0",false,http,"android",p=>long.MaxValue/4);}
    public static string Run(string root)
    {
        checks=0;
        const string oldDefault="https://old.example.com/dispatch.json",newDefault="https://new.example.com/dispatch.json";
        Assert(ClientUpdateAddressStore.MigrateKnownDefault("  "+oldDefault+"  ",oldDefault,newDefault)==newDefault,"retired default migrates");
        Assert(ClientUpdateAddressStore.MigrateKnownDefault("https://custom.example.com/dispatch.json",oldDefault,newDefault)=="https://custom.example.com/dispatch.json","custom address preserved");
        Assert(ClientUpdateAddressStore.MigrateKnownDefault("",oldDefault,newDefault)=="","empty address still uses build default");
        Directory.CreateDirectory(root);string files=Path.Combine(root,"files"),receipt=Path.Combine(root,"receipt");Files(files);var m=MobileResourceManifest.Parse(Manifest());
        var watch=Stopwatch.StartNew();Assert(VerifyAll(new MobileResourceVerification(m,files,receipt))==A.Length+B.Length+C.Length,"first hash reads all");long first=watch.ElapsedMilliseconds;
        watch.Restart();long stamp=File.GetLastWriteTimeUtc(receipt).Ticks;var warm=new MobileResourceVerification(m,files,receipt);Assert(warm.CachedFiles==3 && warm.BytesToHash==0,"all warm records hit");Assert(VerifyAll(warm)==0,"warm payload reads zero");Assert(File.GetLastWriteTimeUtc(receipt).Ticks==stamp,"warm receipt not rewritten");long cached=watch.ElapsedMilliseconds;
        File.SetLastWriteTimeUtc(Path.Combine(files,Paths[0]),DateTime.UtcNow.AddSeconds(3));var changed=new MobileResourceVerification(m,files,receipt);Assert(changed.NeedsHash(0)&&!changed.NeedsHash(1)&&!changed.NeedsHash(2),"only timestamp-changed file invalidated");Assert(VerifyAll(changed)==A.Length,"timestamp-only change hashes one file");
        byte[] corrupt=(byte[])B.Clone();corrupt[0]^=1;File.WriteAllBytes(Path.Combine(files,Paths[1]),corrupt);File.SetLastWriteTimeUtc(Path.Combine(files,Paths[1]),DateTime.UtcNow.AddSeconds(5));var broken=new MobileResourceVerification(m,files,receipt);Assert(!broken.Verify(1),"same-size modified file rejected");Reject(()=>broken.CommitReceipt(),"corrupt file cannot activate");File.WriteAllBytes(Path.Combine(files,Paths[1]),B);VerifyAll(new MobileResourceVerification(m,files,receipt));
        string good=File.ReadAllText(receipt);var lines=good.Split('\n');var body=new StringBuilder(lines[0]).Append('\n');foreach(string line in lines)if(line.StartsWith(Paths[0]+"\t")||line.StartsWith(Paths[2]+"\t"))body.Append(line).Append('\n');string text=body.ToString();File.WriteAllText(receipt,text+Hash(Encoding.UTF8.GetBytes(text))+"\n");var missing=new MobileResourceVerification(m,files,receipt);Assert(missing.CachedFiles==2&&missing.NeedsHash(1),"one missing record invalidates only that file");Assert(VerifyAll(missing)==B.Length,"one missing record rehashes once");
        var reordered=MobileResourceManifest.Parse(Manifest(null,true));Assert(reordered.Version!=m.Version,"fixture changes manifest identity");var compatible=new MobileResourceVerification(reordered,files,receipt);Assert(compatible.CachedFiles==3&&VerifyAll(compatible)==0,"unrelated manifest reorder retains all per-file records");
        byte[] b2=Encoding.UTF8.GetBytes("NEWER bytes");File.WriteAllBytes(Path.Combine(files,Paths[1]),b2);var updated=new MobileResourceVerification(MobileResourceManifest.Parse(Manifest(b2)),files,receipt);Assert(updated.CachedFiles==2&&updated.NeedsHash(1),"new expected hash affects only corresponding file");Assert(VerifyAll(updated)==b2.Length,"new expected hash hashes only changed bytes");
        string prefixRoot=Path.Combine(root,"interrupted");Files(prefixRoot);string prefixReceipt=Path.Combine(root,"partial-receipt");var prefix=new MobileResourceVerification(m,prefixRoot,prefixReceipt);Assert(prefix.Verify(0),"verified prefix file");prefix.CheckpointReceipt();File.WriteAllText(prefixReceipt+".part","truncated replacement");var resumed=new MobileResourceVerification(m,prefixRoot,prefixReceipt);Assert(resumed.CachedFiles==1&&!resumed.NeedsHash(0),"interruption retains completed successful prefix");VerifyAll(resumed);
        var legacyBody=new StringBuilder("FFO-VERIFIED-1\n").Append(m.Version).Append('\n');
        for(int i=0;i<3;i++)legacyBody.Append(m.Entries[i].Sha256).Append('\t').Append(m.Entries[i].Length).Append('\t').Append(File.GetLastWriteTimeUtc(Path.Combine(prefixRoot,Paths[i])).Ticks).Append('\n');
        string legacyText=legacyBody.ToString();File.WriteAllText(prefixReceipt,legacyText+Hash(Encoding.UTF8.GetBytes(legacyText))+"\n");
        var migrated=new MobileResourceVerification(m,prefixRoot,prefixReceipt);Assert(migrated.CachedFiles==3 && VerifyAll(migrated)==0,"matching legacy receipt migrates without payload hashing");Assert(File.ReadAllText(prefixReceipt).StartsWith("FFO-VERIFIED-2\n"),"legacy migration writes atomic v2 receipt");
        File.SetLastWriteTimeUtc(Path.Combine(prefixRoot,Paths[2]),DateTime.UtcNow.AddSeconds(5));
        var interruptedHash=new MobileResourceVerification(m,prefixRoot,prefixReceipt);Reject(()=>interruptedHash.Verify(2,n=>{if(n>0)throw new OperationCanceledException();}),"interrupted hash is not a success");interruptedHash.CheckpointReceipt();Assert(new MobileResourceVerification(m,prefixRoot,prefixReceipt).NeedsHash(2),"interrupted file must be rehashed, not cached");
        string downloads=Path.Combine(root,"download");var h=new Http{Fail=1};Reject(()=>Installer(downloads,h).Install("https://cdn.example.com/manifest.tsv",CancellationToken.None,null),"interrupted download cannot activate");h=new Http();long repeated=0;string active=Installer(downloads,h).Install("https://cdn.example.com/manifest.tsv",CancellationToken.None,(phase,label,done,total)=>{if(phase=="Checking staged resources")repeated+=done;});Assert(!h.Requests.Contains("/r0.bin"),"completed staging payload not redownloaded");Assert(repeated==0,"successful staging prefix not rehashed");
        h=new Http{Text=Manifest(b2),Changed=b2};long reusedHashes=0;string newer=Installer(downloads,h).Install("https://cdn.example.com/manifest.tsv",CancellationToken.None,(phase,label,done,total)=>{if(phase=="Verifying reused resource")reusedHashes+=done;});Assert(newer!=active,"upgrade commits separate generation");Assert(!h.Requests.Contains("/r0.bin")&&!h.Requests.Contains("/r2.bin")&&h.Requests.Contains("/r1.bin"),"upgrade downloads changed file only");Assert(reusedHashes==0,"trusted unchanged upgrade content not rehashed");Assert(MobileResourceManifest.HashFile(Path.Combine(newer,Paths[0]))==Hash(A)&&MobileResourceManifest.HashFile(Path.Combine(newer,Paths[2]))==Hash(C),"copied trusted content remains exact");
        TestRepairBypassesCachedReuse(Path.Combine(root,"repair"));
        string result="PASS per-file receipt and resource performance contracts ("+checks+" checks; first_ms="+first+",warm_ms="+cached+",warm_hash_bytes=0)";return result;
    }

    private static void TestRepairBypassesCachedReuse(string root)
    {
        foreach (bool upgrade in new[] { false, true })
        {
            string storage = Path.Combine(root, upgrade ? "upgrade" : "same-manifest");
            string active = Installer(storage, new Http()).Install("https://cdn.example.com/manifest.tsv", CancellationToken.None, null);
            string source = Path.Combine(active, Paths[0]);
            DateTime stamp = File.GetLastWriteTimeUtc(source);
            byte[] corrupt = (byte[])A.Clone();
            corrupt[0] ^= 1;
            File.WriteAllBytes(source, corrupt);
            File.SetLastWriteTimeUtc(source, stamp);
            Assert(new MobileResourceVerification(MobileResourceManifest.Parse(Manifest()), active,
                Path.Combine(active, ".resource-verified")).CachedFiles == 3,
                "fixture has same-size corruption hidden from the metadata receipt");

            var http = new Http { Text = Manifest(null, upgrade) };
            string repaired = Installer(storage, http).Install("https://cdn.example.com/manifest.tsv",
                CancellationToken.None, null, fullVerification: true);
            Assert(repaired != active, "explicit repair replaces the corrupt active generation");
            Assert(http.Requests.Contains("/r0.bin"), "explicit repair downloads corrupt cached reuse source");
            Assert(!http.Requests.Contains("/r1.bin") && !http.Requests.Contains("/r2.bin"),
                "explicit repair still reuses other source files after hashing them");
            Assert(MobileResourceManifest.HashFile(Path.Combine(repaired, Paths[0])) == Hash(A),
                "repair cannot activate same-size same-timestamp corruption");
            Assert(Installer(storage, new Http()).UseInstalled(CancellationToken.None, null, true) == repaired,
                "repaired generation passes an independent full-byte verification");
        }
    }

    public static void Main(){string root=Path.Combine(Path.GetTempPath(),"ffo-perf-"+Guid.NewGuid().ToString("N"));try{Console.WriteLine(Run(root));}finally{Directory.Delete(root,true);}}
}

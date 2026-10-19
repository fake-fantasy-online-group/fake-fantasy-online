using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.DesktopUpdater;

internal static class DesktopUpdaterContract
{
    private static int checks;
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    private static void Reject(Action action, string name) { try { action(); } catch (Exception) { checks++; return; } throw new Exception("Expected rejection: " + name); }
    private sealed class Platform : IDesktopPlatform
    {
        public bool RejectTrust, RejectLaunch; public int Trusts, Launches;
        public void SetPermissions(string path, bool executable) { }
        public void ValidateTrust(string root, DesktopInstallIdentity id) { Trusts++; if (RejectTrust) throw new IOException("trust denied"); }
        public void StartApplication(string root, DesktopInstallIdentity id) { Launches++; if (RejectLaunch) throw new IOException("launch denied"); }
    }
    private sealed class Fixture : IDisposable
    {
        public readonly string Root, Install, Data, Job;
        public DesktopUpdateRequest Request;
        public Fixture(string platform = "windows")
        {
            Root = Path.Combine(Path.GetTempPath(), "desktop-updater-test-" + Guid.NewGuid().ToString("N"));
            Install = Path.Combine(Root, platform == "macos" ? "game.app" : "game"); Data = Path.Combine(Root, "userdata");
            Job = Path.Combine(Data, "DesktopUpdates", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Install); Directory.CreateDirectory(Job); File.WriteAllText(Path.Combine(Data, "settings"), "keep");
            string marker = DesktopInstallIdentity.MarkerPath(Install, platform); Directory.CreateDirectory(Path.GetDirectoryName(marker));
            File.WriteAllText(marker, Marker(platform, "1.0.0"));
            File.WriteAllText(Path.Combine(Install, "user-original-file"), "original");
            using (var process = Process.GetCurrentProcess()) Request = new DesktopUpdateRequest {
                InstallRoot = Install, PersistentRoot = Data, JobDirectory = Job, PackagePath = Path.Combine(Data, "package.zip"),
                Platform = platform, CurrentVersion = "1.0.0", Version = "1.0.1", ParentProcessId = process.Id,
                ParentStartTicks = process.StartTime.ToUniversalTime().Ticks
            };
        }
        public void Zip(Action<ZipArchive> edit = null, string version = "1.0.1", string pin = "")
        {
            using (var output = File.Create(Request.PackagePath))
            using (var zip = new ZipArchive(output, ZipArchiveMode.Create))
            {
                string top = Path.GetFileName(Install) + "/";
                Add(zip, top + (Request.Platform == "macos" ? "Contents/Resources/" : "") + DesktopInstallIdentity.FileName, Marker(Request.Platform, version, pin));
                Add(zip, top + (Request.Platform == "macos" ? "Contents/MacOS/game" : "game.exe"), "new executable fixture");
                Add(zip, top + "runtime/data", "new runtime fixture"); edit?.Invoke(zip);
            }
            Bind();
        }
        public void Bind()
        {
            Request.SizeBytes = new FileInfo(Request.PackagePath).Length;
            using (var sha = SHA256.Create()) using (var input = File.OpenRead(Request.PackagePath))
                Request.Sha256 = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
        }
        public void Dispose() { if (Directory.Exists(Root)) Directory.Delete(Root, true); }
    }
    private static string Marker(string platform, string version, string pin = "")
    {
        return BoundedJson.Serialize(new Dictionary<string, object> { {"schemaVersion",1}, {"platform",platform}, {"version",version},
            {"installId","com.example.game"}, {"executable",platform == "macos" ? "Contents/MacOS/game" : "game.exe"}, {"macTeamId",""}, {"windowsSignerSha256",pin} });
    }
    private static void Add(ZipArchive zip, string path, string text, int attrs = 0)
    { var entry = zip.CreateEntry(path); entry.ExternalAttributes = attrs; using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(text); }
    private static void InvalidArchive(Action<ZipArchive> edit, string name)
    {
        using (var f = new Fixture()) { f.Zip(edit); using (var engine = new UpdateEngine(f.Request, new Platform())) Reject(() => engine.Prepare(CancellationToken.None), name);
            Check(File.ReadAllText(Path.Combine(f.Install, "user-original-file")) == "original", name + " leaves old app"); }
    }
    private static void PackagerFixture(string package)
    {
        string root = Path.Combine(Path.GetTempPath(), "desktop-packager-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            Dictionary<string,object> marker = null; string name = null;
            using (var input = File.OpenRead(package)) using (var zip = new ZipArchive(input,ZipArchiveMode.Read))
            foreach (var entry in zip.Entries)
                if (entry.FullName.EndsWith("/" + DesktopInstallIdentity.FileName, StringComparison.Ordinal))
                { name = entry.FullName.Split('/')[0]; using (var reader = new StreamReader(entry.Open())) marker = (Dictionary<string,object>)BoundedJson.Parse(reader.ReadToEnd()); }
            string platform = (string)marker["platform"], version = (string)marker["version"];
            marker["version"] = "1.0.0";
            string install = Path.Combine(root,name), data = Path.Combine(root,"data"), job = Path.Combine(data,"DesktopUpdates",Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(job); string oldMarker = DesktopInstallIdentity.MarkerPath(install,platform);
            Directory.CreateDirectory(Path.GetDirectoryName(oldMarker)); File.WriteAllText(oldMarker,BoundedJson.Serialize(marker));
            string copy = Path.Combine(data,"package.zip"); File.Copy(package,copy); string hash;
            using (var sha = SHA256.Create()) using (var stream = File.OpenRead(copy)) hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");
            DesktopUpdateRequest request;
            using (var process = Process.GetCurrentProcess()) request = new DesktopUpdateRequest {
                InstallRoot=install, PersistentRoot=data, JobDirectory=job, PackagePath=copy, Platform=platform, CurrentVersion="1.0.0", Version=version,
                SizeBytes=new FileInfo(copy).Length, Sha256=hash, ParentProcessId=process.Id, ParentStartTicks=process.StartTime.ToUniversalTime().Ticks
            };
            using (var engine = new UpdateEngine(request,new Platform())) { engine.Prepare(CancellationToken.None); engine.Commit(); }
            Check(DesktopInstallIdentity.Read(install,platform).Version == version,"actual Python packager -> C# ZIP updater " + platform);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root,true); }
    }
    public static int Main(string[] args)
    {
        foreach (string platform in new[] { "windows", "macos" })
        using (var f = new Fixture(platform))
        {
            f.Zip(); f.Request.Save(); var request = DesktopUpdateRequest.Read(f.Request.RequestPath);
            Check(request.Sha256 == f.Request.Sha256 && request.ParentStartTicks == f.Request.ParentStartTicks, "request round trip " + platform);
            var os = new Platform(); string backup;
            using (var engine = new UpdateEngine(request, os))
            {
                engine.Prepare(CancellationToken.None);
                Check(DesktopInstallIdentity.Read(f.Install, platform).Version == "1.0.0", "prepare never replaces live app");
                backup = engine.BackupPath; engine.Commit();
                Check(DesktopInstallIdentity.Read(f.Install, platform).Version == "1.0.1", "new app committed");
                Check(File.ReadAllText(Path.Combine(backup, "user-original-file")) == "original", "old application files retained");
                Check(File.ReadAllText(Path.Combine(f.Data, "settings")) == "keep", "user settings retained");
                Check(os.Trusts == 3 && os.Launches == 1, "trust rechecked at prepare/commit/installed, launch once");
            }
            UpdateEngine.Recover(f.Request);
            Check(DesktopInstallIdentity.Read(f.Install, platform).Version == "1.0.0", "explicit journal rollback restores old app");
            Check(Directory.Exists(f.Install + ".update-failed-" + Path.GetFileName(f.Job)), "rollback retains newer app");
        }
        foreach (string point in new[] { "old-renamed", "new-renamed" })
        using (var f = new Fixture())
        {
            f.Zip(); using (var engine = new UpdateEngine(f.Request, new Platform(), p => { if (p == point) throw new IOException("injected failure"); }))
            { engine.Prepare(CancellationToken.None); Reject(engine.Commit, "rollback at " + point); }
            Check(DesktopInstallIdentity.Read(f.Install, "windows").Version == "1.0.0", "old app restored after " + point);
        }
        using (var f = new Fixture())
        {
            f.Zip(); using (var engine = new UpdateEngine(f.Request, new Platform { RejectTrust = true })) Reject(() => engine.Prepare(CancellationToken.None), "OS trust failure");
            Check(File.Exists(Path.Combine(f.Install,"user-original-file")), "trust rejection preserves current app");
        }
        using (var f = new Fixture())
        {
            f.Zip(); using (var engine = new UpdateEngine(f.Request, new Platform { RejectLaunch = true }))
            { engine.Prepare(CancellationToken.None); Reject(engine.Commit, "OS launch failure rolls back"); }
            Check(DesktopInstallIdentity.Read(f.Install,"windows").Version == "1.0.0", "launch failure old app restored");
        }
        using (var f = new Fixture())
        {
            f.Zip(); string backup;
            using (var engine = new UpdateEngine(f.Request,new Platform())) { engine.Prepare(CancellationToken.None); backup=engine.BackupPath; }
            Directory.Move(f.Install,backup); // Simulate power/process loss between the two directory renames.
            Check(!Directory.Exists(f.Install),"crash fixture has missing application path");
            UpdateEngine.Recover(f.Request);
            Check(DesktopInstallIdentity.Read(f.Install,"windows").Version == "1.0.0","journal restores missing application after interrupted rename");
        }
        using (var f = new Fixture())
        {
            f.Zip(); using (var engine = new UpdateEngine(f.Request,new Platform(),null,_=>0))
                Reject(()=>engine.Prepare(CancellationToken.None),"insufficient storage rejected before extraction");
            Check(DesktopInstallIdentity.Read(f.Install,"windows").Version == "1.0.0","storage rejection preserves current application");
        }
        foreach (string malformed in new[]{"zip64","trailing","truncated"})
        using (var f = new Fixture())
        {
            f.Zip(); byte[] data=File.ReadAllBytes(f.Request.PackagePath);
            if(malformed=="zip64") { for(int i=data.Length-14;i<data.Length-10;i++) data[i]=255; }
            if(malformed=="trailing") Array.Resize(ref data,data.Length+1);
            if(malformed=="truncated") Array.Resize(ref data,data.Length-1);
            File.WriteAllBytes(f.Request.PackagePath,data); f.Bind();
            using(var engine=new UpdateEngine(f.Request,new Platform())) Reject(()=>engine.Prepare(CancellationToken.None),"reject noncanonical ZIP " + malformed);
        }
        foreach (string attack in new[] { "changed", "extra", "missing" })
        using (var f = new Fixture())
        {
            f.Zip(); using (var engine = new UpdateEngine(f.Request, new Platform()))
            {
                engine.Prepare(CancellationToken.None); string staged = Path.Combine(f.Install + ".update-stage-" + Path.GetFileName(f.Job),"game");
                if (attack == "changed") File.WriteAllText(Path.Combine(staged,"game.exe"), "changed");
                if (attack == "extra") File.WriteAllText(Path.Combine(staged,"extra.dll"), "extra");
                if (attack == "missing") File.Delete(Path.Combine(staged,"game.exe"));
                Reject(engine.Commit, "staged package " + attack + " rejected");
            }
            Check(DesktopInstallIdentity.Read(f.Install,"windows").Version == "1.0.0", "stage tamper leaves old app");
        }
        using (var f = new Fixture())
        {
            f.Zip(); f.Request.Sha256 = new string('a',64);
            using (var engine = new UpdateEngine(f.Request, new Platform())) Reject(() => engine.Prepare(CancellationToken.None), "full hash revalidation");
        }
        using (var f = new Fixture())
        {
            f.Zip(); f.Request.SizeBytes++;
            using (var engine = new UpdateEngine(f.Request, new Platform())) Reject(() => engine.Prepare(CancellationToken.None), "length revalidation");
        }
        using (var f = new Fixture())
        {
            f.Zip(version:"1.0.2"); using (var engine = new UpdateEngine(f.Request, new Platform())) Reject(() => engine.Prepare(CancellationToken.None), "dispatch version mismatch");
        }
        using (var f = new Fixture())
        {
            f.Zip(pin:new string('a',64)); using (var engine = new UpdateEngine(f.Request, new Platform())) Reject(() => engine.Prepare(CancellationToken.None), "signing policy substitution");
        }
        using (var f = new Fixture())
        {
            f.Zip(); var token = new CancellationToken(true);
            using (var engine = new UpdateEngine(f.Request, new Platform())) Reject(() => engine.Prepare(token), "cancellation");
            Check(DesktopInstallIdentity.Read(f.Install,"windows").Version == "1.0.0", "cancel preserves old app");
        }
        using (var f = new Fixture())
        {
            f.Zip(); f.Request.PersistentRoot = f.Install; Reject(f.Request.Validate, "user data overlap");
        }
        using (var f = new Fixture())
        {
            var alive = typeof(FakeFantasyOnline.DesktopUpdater.Program).GetMethod("ParentAlive",BindingFlags.NonPublic|BindingFlags.Static);
            Check((bool)alive.Invoke(null,new object[]{f.Request}), "helper recognizes live original parent");
            f.Request.ParentStartTicks -= 1;
            Check((bool)alive.Invoke(null,new object[]{f.Request}), "subsecond runtime precision cannot skip waiting for parent");
            f.Request.ParentProcessId = int.MaxValue;
            Check(!(bool)alive.Invoke(null,new object[]{f.Request}), "missing parent process is exited");
        }
        InvalidArchive(z => Add(z,"../escape", "bad"),"path traversal");
        InvalidArchive(z => Add(z,"/escape", "bad"),"absolute path");
        InvalidArchive(z => Add(z,"other/evil.exe", "bad"),"different application root");
        InvalidArchive(z => Add(z,"game/game.exe", "bad"),"duplicate path");
        InvalidArchive(z => Add(z,"game/GAME.exe", "bad"),"case collision");
        InvalidArchive(z => Add(z,"game/RUNTIME/other", "bad"),"ancestor case collision");
        InvalidArchive(z => Add(z,"game/runtime", "bad"),"file directory collision");
        InvalidArchive(z => Add(z,"game/CON.txt", "bad"),"Windows device path");
        InvalidArchive(z => Add(z,"game/a:evil", "bad"),"Windows alternate stream");
        InvalidArchive(z => Add(z,"game/a\\evil", "bad"),"backslash path");
        InvalidArchive(z => Add(z,"game/link", "runtime/data", unchecked((int)0xa1ff0000)),"archive symlink");
        InvalidArchive(z => Add(z,"game/fifo", "", unchecked((int)0x11ff0000)),"archive special file");
        InvalidArchive(z => Add(z,"game/.DS_Store", "bad"),"packaging metadata");
        foreach (string package in args) PackagerFixture(package);
        Console.WriteLine("PASS desktop updater: " + checks + " checks (real ZIP/filesystem/hash/transaction code; OS trust and launching substituted). No Unity/macOS/Windows execution claimed.");
        return 0;
    }
}

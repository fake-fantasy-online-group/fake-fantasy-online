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
    private static readonly List<string> liveResourceFailures = new List<string>();
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    private static void Reject(Action action, string name) { try { action(); } catch (Exception) { checks++; return; } throw new Exception("Expected rejection: " + name); }
    private sealed class FailedLog : TextWriter
    {
        public override Encoding Encoding { get { return Encoding.UTF8; } }
        public override void Write(char value) { throw new IOException("stderr unavailable"); }
    }
    private sealed class Platform : IDesktopPlatform
    {
        public bool RejectTrust, RejectLaunch, UncertainLaunch, RunningAtRollback; public int Trusts, Launches, RejectTrustAt;
        public void SetPermissions(string path, bool executable) { }
        public void ValidateTrust(string root, DesktopInstallIdentity id) { Trusts++; if (RejectTrust || Trusts == RejectTrustAt) throw new IOException("trust denied"); }
        public void StartApplication(string root, DesktopInstallIdentity id) {
            Launches++;
            if (UncertainLaunch) throw new DesktopLaunchUncertainException("launch still pending");
            if (RejectLaunch) throw new IOException("launch denied");
        }
        public void RequireApplicationStopped(string root, DesktopInstallIdentity id)
        { if (RunningAtRollback) throw new DesktopLaunchUncertainException("new application running"); }
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
    private static string Resources(string root) { return Path.Combine(root, "DownloadedResources"); }
    private static void SeedResources(string root)
    {
        Directory.CreateDirectory(Path.Combine(Resources(root), "Remote"));
        File.WriteAllText(Path.Combine(Resources(root), "Remote", "asset.bundle"), "downloaded asset");
        Directory.CreateDirectory(Path.Combine(Resources(root), "Remote.pending-download"));
        File.WriteAllText(Path.Combine(Resources(root), "Remote.pending-download", "pending.bundle"), "pending asset");
        File.WriteAllText(Path.Combine(Resources(root), "download-state.json"), "download state");
        File.WriteAllText(Path.Combine(Resources(root), "user-note"), "keep resource neighbor");
    }
    private static void CheckResources(string root, string name)
    {
        Check(File.ReadAllText(Path.Combine(Resources(root), "Remote", "asset.bundle")) == "downloaded asset", name + " asset");
        Check(File.ReadAllText(Path.Combine(Resources(root), "Remote.pending-download", "pending.bundle")) == "pending asset", name + " pending download");
        Check(File.ReadAllText(Path.Combine(Resources(root), "download-state.json")) == "download state", name + " download state");
        Check(File.ReadAllText(Path.Combine(Resources(root), "user-note")) == "keep resource neighbor", name + " unrelated resource neighbor");
    }
    private static void CheckLiveResources(Fixture fixture, string name)
    {
        if (!Directory.Exists(fixture.Install)) return;
        // Record outside Reject's exception-based fault injection, so rollback cannot
        // conceal a temporarily exposed executable whose resource tree is elsewhere.
        try { CheckResources(fixture.Install, "launchable application keeps resources at " + name); }
        catch (Exception exception) { liveResourceFailures.Add(name + ": " + exception.Message); }
    }
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
    private static void TrustModes()
    {
        string root = Path.Combine(Path.GetTempPath(), "nonexistent-trust-fixture-" + Guid.NewGuid().ToString("N"));
        var mac = new DesktopInstallIdentity { Platform = "macos", Version = "1.0.1", InstallId = "com.example.game",
            Executable = "Contents/MacOS/game", MacTeamId = "", WindowsSignerSha256 = "" };
        var calls = new List<string>();
        string team = "", identifier = mac.InstallId, version = mac.Version;
        bool corrupt = false, rejectAssessment = false;
        var platform = new DesktopPlatform((command, arguments) => {
            calls.Add(command + " " + arguments);
            if (command.EndsWith("codesign") && arguments.StartsWith("--verify") && corrupt) throw new IOException("invalid code signature");
            if (command.EndsWith("codesign") && arguments.StartsWith("-d")) return "Identifier=" + identifier + "\nTeamIdentifier=" + team + "\n";
            if (command.EndsWith("PlistBuddy")) return version;
            if (command.EndsWith("spctl") && rejectAssessment) throw new IOException("Gatekeeper rejected package");
            return "";
        });
        platform.ValidateTrust(root, mac);
        Check(calls.Exists(x => x.Contains("codesign --verify --deep --strict")) && calls.Exists(x => x.Contains("xattr -w com.apple.quarantine")), "ad-hoc Mac retains integrity and quarantine");
        Check(!calls.Exists(x => x.Contains("spctl")), "empty Mac team does not demand Developer ID assessment");
        corrupt = true; Reject(() => platform.ValidateTrust(root, mac), "corrupt ad-hoc signature still rejected"); corrupt = false;
        identifier = "com.other.game"; Reject(() => platform.ValidateTrust(root, mac), "ad-hoc identifier still checked"); identifier = mac.InstallId;
        version = "9.0.0"; Reject(() => platform.ValidateTrust(root, mac), "ad-hoc bundle version still checked"); version = mac.Version;
        mac.MacTeamId = team = "ABCDE12345"; calls.Clear(); platform.ValidateTrust(root, mac);
        Check(calls.Exists(x => x.Contains("spctl --assess --type execute")), "signed Mac retains OS assessment");
        team = "OTHER12345"; Reject(() => platform.ValidateTrust(root, mac), "signed Mac team mismatch rejected"); team = mac.MacTeamId;
        rejectAssessment = true; Reject(() => platform.ValidateTrust(root, mac), "signed Mac OS assessment rejection enforced");
        int publisherChecks = 0; string expectedPin = new string('a', 64); bool rejectPublisher = false;
        var win = new DesktopInstallIdentity { Platform = "windows", Version = "1.0.1", InstallId = "com.example.game",
            Executable = "game.exe", MacTeamId = "", WindowsSignerSha256 = "" };
        version = win.Version;
        var windows = new DesktopPlatform(verifyWindowsPublisher: (path, pin) => {
            publisherChecks++; Check(pin == expectedPin && path == Path.Combine(root, "game.exe"), "signed Windows validation receives exact pin and executable");
            if (rejectPublisher) throw new IOException("publisher verification rejected");
        }, productVersion: path => version);
        windows.ValidateTrust(root, win); Check(publisherChecks == 0, "unsigned Windows does not require Authenticode");
        version = "9.0.0"; Reject(() => windows.ValidateTrust(root, win), "unsigned Windows version mismatch rejected"); version = win.Version;
        win.WindowsSignerSha256 = expectedPin; windows.ValidateTrust(root, win); Check(publisherChecks == 1, "signed Windows publisher validation retained");
        rejectPublisher = true; Reject(() => windows.ValidateTrust(root, win), "signed Windows validation failure remains fatal");
        foreach (string os in new[] { "windows", "macos" })
        using (var f = new Fixture(os))
        {
            var installed = DesktopInstallIdentity.Read(f.Install, os);
            var next = DesktopInstallIdentity.Read(f.Install, os); next.Version = "1.0.1";
            if (os == "windows") installed.WindowsSignerSha256 = expectedPin; else installed.MacTeamId = "ABCDE12345";
            Reject(() => installed.RequireUpdate(next, "1.0.1"), os + " signed policy cannot downgrade to empty");
            next.WindowsSignerSha256 = installed.WindowsSignerSha256; next.MacTeamId = installed.MacTeamId;
            installed.RequireUpdate(next, "1.0.1"); Check(true, os + " matching signed policy accepted");
        }
    }
    private static void LaunchConfirmation()
    {
        double seconds = 0;
        DesktopPlatform.ConfirmLaunch(() => seconds < 1 ? 0 : 1, () => seconds += .25, () => TimeSpan.FromSeconds(seconds));
        Check(seconds == 4, "launch waits for actual process and three seconds of survival");
        seconds = 0;
        DesktopPlatform.ConfirmLaunch(() => seconds < 29 ? 0 : 1, () => seconds += .25, () => TimeSpan.FromSeconds(seconds));
        Check(seconds == 32, "late process receives the complete survival window");
        string root = Path.Combine(Path.GetTempPath(), "launch-identity-fixture");
        Check(DesktopPlatform.MatchesExecutable(Path.Combine(root, "game.exe"), Path.Combine(root, "game.exe"), "windows"), "matching executable path accepted");
        Check(!DesktopPlatform.MatchesExecutable(Path.Combine(root, "other", "game.exe"), Path.Combine(root, "game.exe"), "windows"), "same process name from another directory cannot confirm launch");
        seconds = 0;
        Reject(() => DesktopPlatform.ConfirmLaunch(() => seconds < 1 ? 1 : -1, () => seconds += .25,
            () => TimeSpan.FromSeconds(seconds)), "immediate process exit is launch failure");
        seconds = 0; bool uncertain = false;
        try { DesktopPlatform.ConfirmLaunch(() => 0, () => seconds += 1, () => TimeSpan.FromSeconds(seconds)); }
        catch (DesktopLaunchUncertainException) { uncertain = true; }
        Check(uncertain && seconds == 30, "accepted launch with no process is uncertain, not successful or safely stopped");
        uncertain = false;
        try { DesktopPlatform.ConfirmLaunch(() => { throw new InvalidOperationException("inspection failed"); }, () => {}, () => TimeSpan.Zero); }
        catch (DesktopLaunchUncertainException) { uncertain = true; }
        Check(uncertain, "inspection failure cannot cause blind rollback");
    }
    private static void ExpectUncertain(Action action, string name)
    {
        bool uncertain = false;
        try { action(); } catch (DesktopLaunchUncertainException) { uncertain = true; }
        Check(uncertain, name);
    }
    private static void ProcessInspection()
    {
        Check(DesktopUpdateFiles.ProcessModulePath(null) == null, "null macOS MainModule means unknown path");
        Check(!DesktopPlatform.MatchesExecutable(null, "/game", "macos"), "null module cannot confirm launch");
        Check(!DesktopPlatform.MatchesExecutable("", "/game", "macos"), "empty file name cannot confirm launch");
        Check(!DesktopPlatform.MatchesExecutable(" ", "/game", "macos"), "blank file name cannot confirm launch");
        foreach (string platform in new[] { "macos", "windows" })
        using (var f = new Fixture(platform))
        {
            var identity = DesktopInstallIdentity.Read(f.Install, platform);
            foreach (string unknown in new string[] { null, "", " " })
            {
                ExpectUncertain(() => DesktopPlatform.MatchesObservedProcess(unknown, () => "game", f.Install, identity, false),
                    "unknown same-name app blocks replacement " + platform);
                ExpectUncertain(() => DesktopPlatform.MatchesObservedProcess(unknown, () => "game", f.Install, identity, true),
                    "unknown same-name app cannot confirm launch " + platform);
                Check(!DesktopPlatform.MatchesObservedProcess(unknown, () => "unrelated-system-process", f.Install, identity, false),
                    "unknown unrelated named system process is not an app " + platform);
            }
            foreach (string unknownName in new string[] { null, "", " " })
                ExpectUncertain(() => DesktopPlatform.MatchesObservedProcess(null, () => unknownName, f.Install, identity, false),
                    "missing process name is not proof of unrelated process " + platform);
            ExpectUncertain(() => DesktopPlatform.MatchesObservedProcess(null,
                () => { throw new System.ComponentModel.Win32Exception("denied"); }, f.Install, identity, false),
                "unreadable name blocks replacement " + platform);
            if (platform == "macos")
            {
                identity.Executable = "Contents/MacOS/FFOUpdaterFixture";
                ExpectUncertain(() => DesktopPlatform.MatchesObservedProcess(null, () => "FFOUpdaterFixture".Substring(0, 15), f.Install, identity, false),
                    "macOS 15-character process name still blocks replacement");
            }
            // Exercise the real production scan over this OS's processes, not IDesktopPlatform stubs.
            Program.RequireApplicationStopped(f.Request);
            Check(true, "real process enumeration allows a stopped isolated installation " + platform);
            string backup = f.Install + ".update-backup-" + Path.GetFileName(f.Job);
            Directory.Move(f.Install, backup);
            Program.RequireApplicationStopped(f.Request);
            Check(true, "pre-recovery scan works when only the renamed old app remains " + platform);
        }
        using (var process = Process.GetCurrentProcess())
        using (var f = new Fixture())
        {
            string actual = DesktopUpdateFiles.ProcessModulePath(process.MainModule);
            Check(!string.IsNullOrEmpty(actual), "real current process module yields executable path");
            var identity = new DesktopInstallIdentity { Platform = "windows", Executable = Path.GetFileName(actual) };
            ExpectUncertain(() => new DesktopPlatform().RequireApplicationStopped(Path.GetDirectoryName(actual), identity),
                "real process enumeration refuses a running application directory");
            f.Request.ParentStartTicks -= TimeSpan.TicksPerSecond * 10;
            Check(Program.ParentAlive(f.Request, p => null), "live PID with mismatched time and null module remains alive");
            Check(Program.ParentAlive(f.Request, p => ""), "live PID with empty path remains alive");
            Check(Program.ParentAlive(f.Request, p => { throw new System.ComponentModel.Win32Exception("denied"); }),
                "live PID with inaccessible module remains alive");
            Check(Program.ParentAlive(f.Request, p => { throw new InvalidOperationException("racing query"); }),
                "unconfirmed parent exit is retried, not declared stopped");
            Check(!Program.ParentAlive(f.Request, p => actual), "known reused PID outside installation can be ignored");
        }
    }
    public static int Main(string[] args)
    {
        TrustModes(); LaunchConfirmation(); ProcessInspection();
        foreach (string platform in new[] { "windows", "macos" })
        using (var f = new Fixture(platform))
        {
            SeedResources(f.Install);
            f.Zip(); f.Request.Save(); var request = DesktopUpdateRequest.Read(f.Request.RequestPath);
            Check(request.Sha256 == f.Request.Sha256 && request.ParentStartTicks == f.Request.ParentStartTicks, "request round trip " + platform);
            var os = new Platform(); string backup;
            using (var engine = new UpdateEngine(request, os, point => { if (platform == "windows") CheckLiveResources(f, point); }))
            {
                engine.Prepare(CancellationToken.None);
                Check(DesktopInstallIdentity.Read(f.Install, platform).Version == "1.0.0", "prepare never replaces live app");
                backup = engine.BackupPath; engine.Commit();
                Check(DesktopInstallIdentity.Read(f.Install, platform).Version == "1.0.1", "new app committed");
                Check(File.ReadAllText(Path.Combine(backup, "user-original-file")) == "original", "old application files retained");
                Check(File.ReadAllText(Path.Combine(f.Data, "settings")) == "keep", "user settings retained");
                Check(os.Trusts == 3 && os.Launches == 1, "trust rechecked at prepare/commit/installed, launch once");
                CheckResources(platform == "windows" ? f.Install : backup, "commit resources " + platform);
                Check(!Directory.Exists(Resources(platform == "windows" ? backup : f.Install)), "resources have one owner " + platform);
                if (platform == "windows") File.WriteAllText(Path.Combine(Resources(f.Install), "post-update.bundle"), "new download");
            }
            UpdateEngine.Recover(f.Request, point => { if (platform == "windows") CheckLiveResources(f, point); });
            Check(DesktopInstallIdentity.Read(f.Install, platform).Version == "1.0.0", "explicit journal rollback restores old app");
            Check(Directory.Exists(f.Install + ".update-failed-" + Path.GetFileName(f.Job)), "rollback retains newer app");
            CheckResources(f.Install, "recovered resources " + platform);
            if (platform == "windows")
                Check(File.ReadAllText(Path.Combine(Resources(f.Install), "post-update.bundle")) == "new download", "recovery preserves post-update downloads");
        }
        foreach (string point in new[] { "old-renamed", "resources-staged", "new-renamed" })
        using (var f = new Fixture())
        {
            SeedResources(f.Install);
            f.Zip(); using (var engine = new UpdateEngine(f.Request, new Platform(), p =>
            { CheckLiveResources(f, p); if (p == point) throw new IOException("injected failure"); }))
            { engine.Prepare(CancellationToken.None); Reject(engine.Commit, "rollback at " + point); }
            Check(DesktopInstallIdentity.Read(f.Install, "windows").Version == "1.0.0", "old app restored after " + point);
            CheckResources(f.Install, "rollback after " + point);
        }
        using (var f = new Fixture())
        {
            f.Zip(); using (var engine = new UpdateEngine(f.Request, new Platform { RejectTrust = true })) Reject(() => engine.Prepare(CancellationToken.None), "OS trust failure");
            Check(File.Exists(Path.Combine(f.Install,"user-original-file")), "trust rejection preserves current app");
        }
        using (var f = new Fixture())
        {
            SeedResources(f.Install);
            f.Zip(); using (var engine = new UpdateEngine(f.Request, new Platform { RejectLaunch = true }))
            { engine.Prepare(CancellationToken.None); Reject(engine.Commit, "OS launch failure rolls back"); }
            Check(DesktopInstallIdentity.Read(f.Install,"windows").Version == "1.0.0", "launch failure old app restored");
            CheckResources(f.Install, "launch failure preserves resources");
        }
        using (var f = new Fixture())
        {
            SeedResources(f.Install);
            f.Zip(); using (var engine = new UpdateEngine(f.Request, new Platform { RejectTrustAt = 3 }))
            { engine.Prepare(CancellationToken.None); Reject(engine.Commit, "installed trust failure rolls back"); }
            Check(DesktopInstallIdentity.Read(f.Install,"windows").Version == "1.0.0", "installed trust failure old app restored");
            CheckResources(f.Install, "installed trust failure preserves resources");
        }
        foreach (string os in new[] { "windows", "macos" })
        foreach (bool running in new[] { false, true })
        using (var f = new Fixture(os))
        {
            SeedResources(f.Install); f.Zip(); string backup;
            var platform = new Platform { UncertainLaunch = !running, RejectLaunch = running, RunningAtRollback = running };
            using (var engine = new UpdateEngine(f.Request, platform))
            { engine.Prepare(CancellationToken.None); backup = engine.BackupPath; Reject(engine.Commit, "uncertain/running launch retained " + os); }
            Check(DesktopInstallIdentity.Read(f.Install, os).Version == "1.0.1", "possibly running app was not moved " + os);
            Check(DesktopInstallIdentity.Read(backup, os).Version == "1.0.0", "old backup retained for recovery " + os);
            Check(!File.Exists(Path.Combine(f.Job, "installed")), "uncertain launch has no success receipt " + os);
            Check(!Directory.Exists(f.Install + ".update-failed-" + Path.GetFileName(f.Job)), "no live-root rename on uncertainty " + os);
            CheckResources(os == "windows" ? f.Install : backup, "uncertain launch resource owner " + os);
        }
        foreach (bool failedLog in new[] { false, true })
        using (var f = new Fixture())
        {
            f.Zip();
            using (var engine = new UpdateEngine(f.Request, new Platform(), point => {
                if (point == "new-renamed") Directory.CreateDirectory(Path.Combine(f.Job, "installed"));
            }))
            {
                engine.Prepare(CancellationToken.None); var previousLog = Console.Error;
                try { if (failedLog) Console.SetError(new FailedLog()); engine.Commit(); }
                finally { Console.SetError(previousLog); }
            }
            Check(DesktopInstallIdentity.Read(f.Install, "windows").Version == "1.0.1", "receipt failure after confirmed launch cannot roll back running app");
        }
        foreach (string point in new[] { "old-renamed", "resources-staged", "new-renamed", "recovery-displaced", "recovery-resources-restored" })
        using (var f = new Fixture())
        {
            SeedResources(f.Install);
            f.Zip(); string backup;
            using (var engine = new UpdateEngine(f.Request,new Platform())) { engine.Prepare(CancellationToken.None); backup=engine.BackupPath; }
            // Simulate process/power loss at each rename boundary, without Commit's exception rollback.
            Directory.Move(f.Install,backup);
            CheckLiveResources(f, "crash old-renamed");
            if (point != "old-renamed")
            {
                string staged = Path.Combine(f.Install + ".update-stage-" + Path.GetFileName(f.Job), "game");
                Directory.Move(Resources(backup), Resources(staged));
                CheckLiveResources(f, "crash resources-staged");
                if (point != "resources-staged") Directory.Move(staged, f.Install);
                CheckLiveResources(f, "crash new-renamed");
            }
            if (point == "recovery-displaced" || point == "recovery-resources-restored")
            {
                Directory.Move(f.Install, f.Install + ".update-failed-" + Path.GetFileName(f.Job));
                CheckLiveResources(f, "crash recovery-displaced");
            }
            if (point == "recovery-resources-restored")
                Directory.Move(Resources(f.Install + ".update-failed-" + Path.GetFileName(f.Job)), Resources(backup));
            UpdateEngine.Recover(f.Request, p => CheckLiveResources(f, p));
            Check(DesktopInstallIdentity.Read(f.Install,"windows").Version == "1.0.0","journal restores old application after " + point);
            CheckResources(f.Install, "crash recovery after " + point);
            Check(File.ReadAllText(Path.Combine(f.Install, "user-original-file")) == "original", "crash recovery retains unrelated old files");
        }
        foreach (string point in new[] { "rollback-displaced", "rollback-resources-restored", "rollback-restored" })
        using (var f = new Fixture())
        {
            SeedResources(f.Install); f.Zip(); string backup;
            using (var engine = new UpdateEngine(f.Request, new Platform { RejectLaunch = true }, p =>
            {
                CheckLiveResources(f, p);
                if (p == point) throw new IOException("injected process loss during rollback");
            }))
            {
                engine.Prepare(CancellationToken.None); backup = engine.BackupPath;
                Reject(engine.Commit, "rollback interrupted after " + point);
            }
            if (Directory.Exists(backup)) UpdateEngine.Recover(f.Request, p => CheckLiveResources(f, p));
            CheckResources(f.Install, "recover interrupted " + point);
            Check(DesktopInstallIdentity.Read(f.Install, "windows").Version == "1.0.0", "old app restored after interrupted " + point);
        }
        foreach (string point in new[] { "recovery-displaced", "recovery-resources-restored", "recovery-restored" })
        using (var f = new Fixture())
        {
            SeedResources(f.Install); f.Zip(); string backup;
            using (var engine = new UpdateEngine(f.Request, new Platform()))
            { engine.Prepare(CancellationToken.None); backup = engine.BackupPath; engine.Commit(); }
            Reject(() => UpdateEngine.Recover(f.Request, p =>
            {
                CheckLiveResources(f, p);
                if (p == point) throw new IOException("injected process loss during recovery");
            }), "recovery interrupted after " + point);
            if (Directory.Exists(backup)) UpdateEngine.Recover(f.Request, p => CheckLiveResources(f, p));
            CheckResources(f.Install, "repeat interrupted " + point);
            Check(DesktopInstallIdentity.Read(f.Install, "windows").Version == "1.0.0", "old app restored after interrupted " + point);
        }
        using (var f = new Fixture())
        {
            SeedResources(f.Install); f.Zip();
            // Allow rename but forbid a second managed read: resources must never be copied or hashed.
            using (var resource = new FileStream(Path.Combine(Resources(f.Install), "Remote", "asset.bundle"),
                FileMode.Open, FileAccess.Read, FileShare.Delete))
            using (var engine = new UpdateEngine(f.Request, new Platform()))
            { engine.Prepare(CancellationToken.None); engine.Commit(); }
            CheckResources(f.Install, "resources renamed without file reads");
        }
        using (var f = new Fixture())
        {
            SeedResources(f.Install); f.Zip(); string backup;
            using (var engine = new UpdateEngine(f.Request, new Platform()))
            { engine.Prepare(CancellationToken.None); backup = engine.BackupPath; engine.Commit(); }
            SeedResources(backup);
            File.WriteAllText(Path.Combine(Resources(backup), "user-conflict"), "must not merge or delete");
            Reject(() => UpdateEngine.Recover(f.Request), "conflicting resource trees rejected without merging");
            Check(DesktopInstallIdentity.Read(f.Install, "windows").Version == "1.0.1", "resource conflict leaves live app unchanged");
            CheckResources(f.Install, "resource conflict preserves live tree");
            CheckResources(backup, "resource conflict preserves backup tree");
            Check(File.ReadAllText(Path.Combine(Resources(backup), "user-conflict")) == "must not merge or delete", "resource conflict preserves unrelated file");
        }
        using (var f = new Fixture())
        {
            SeedResources(f.Install); f.Zip();
            string displaced = f.Install + ".update-failed-" + Path.GetFileName(f.Job), backup;
            using (var engine = new UpdateEngine(f.Request, new Platform(), point =>
            {
                CheckLiveResources(f, point);
                if (point != "new-renamed") return;
                Directory.CreateDirectory(displaced);
                File.WriteAllText(Path.Combine(displaced, "user-conflict"), "keep");
                throw new IOException("injected failure with blocked rollback");
            }))
            {
                engine.Prepare(CancellationToken.None); backup = engine.BackupPath;
                Reject(engine.Commit, "blocked rollback retains recoverable transaction");
            }
            CheckResources(f.Install, "failed displacement leaves resources in live application");
            Check(!Directory.Exists(Resources(backup)), "failed displacement never extracts live resources");
            Check(DesktopInstallIdentity.Read(f.Install, "windows").Version == "1.0.1", "failed rollback retains new application");
            Reject(() => UpdateEngine.Recover(f.Request), "recovery refuses unrelated displaced directory");
            Check(File.ReadAllText(Path.Combine(displaced, "user-conflict")) == "keep", "failed rollback never removes conflict");
            Directory.Move(displaced, Path.Combine(f.Root, "user-retained-conflict"));
            UpdateEngine.Recover(f.Request);
            CheckResources(f.Install, "recovery completes after rollback conflict moved aside");
            Check(DesktopInstallIdentity.Read(f.Install, "windows").Version == "1.0.0", "recovery completes failed rollback");
        }
        using (var f = new Fixture())
        {
            f.Zip();
            using (var engine = new UpdateEngine(f.Request, new Platform()))
            { engine.Prepare(CancellationToken.None); engine.Commit(); }
            SeedResources(f.Install); // First resource download may happen only after this update.
            UpdateEngine.Recover(f.Request);
            CheckResources(f.Install, "recovery retains downloads first created by updated application");
        }
        using (var f = new Fixture())
        {
            f.Zip();
            using (var engine = new UpdateEngine(f.Request, new Platform()))
            {
                engine.Prepare(CancellationToken.None);
                Directory.CreateDirectory(Path.Combine(f.Install + ".update-stage-" + Path.GetFileName(f.Job), "game", "DownloadedResources"));
                Reject(engine.Commit, "empty reserved directory injected after prepare");
            }
            Check(DesktopInstallIdentity.Read(f.Install, "windows").Version == "1.0.0", "reserved stage collision leaves old app");
        }
        using (var f = new Fixture())
        {
            File.WriteAllText(Resources(f.Install), "unrelated file");
            f.Zip();
            using (var engine = new UpdateEngine(f.Request, new Platform()))
            { engine.Prepare(CancellationToken.None); Reject(engine.Commit, "resource path file collision"); }
            Check(File.ReadAllText(Resources(f.Install)) == "unrelated file", "resource path file collision never deletes user file");
        }
        using (var f = new Fixture("macos"))
        {
            f.Zip(z => Add(z, "game.app/DownloadedResources/package-file", "ordinary macOS package file"));
            using (var engine = new UpdateEngine(f.Request, new Platform()))
            { engine.Prepare(CancellationToken.None); engine.Commit(); }
            Check(File.ReadAllText(Path.Combine(Resources(f.Install), "package-file")) == "ordinary macOS package file", "macOS package behavior unchanged");
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
            var alive = typeof(FakeFantasyOnline.DesktopUpdater.Program).GetMethod("ParentAlive",BindingFlags.NonPublic|BindingFlags.Static, null, new[] { typeof(DesktopUpdateRequest) }, null);
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
        InvalidArchive(z => Add(z,"game/DownloadedResources", "bad"),"reserved resource file");
        InvalidArchive(z => Add(z,"game/DownloadedResources/", ""),"reserved resource directory");
        InvalidArchive(z => Add(z,"game/DownloadedResources/Remote/asset.bundle", "bad"),"reserved resource descendants");
        InvalidArchive(z => Add(z,"game/dOwNlOaDeDrEsOuRcEs/Remote/asset.bundle", "bad"),"case-insensitive reserved resources");
        foreach (string package in args) PackagerFixture(package);
        Check(liveResourceFailures.Count == 0, "Every launchable application must retain its resource tree: " + string.Join("; ", liveResourceFailures));
        Console.WriteLine("PASS desktop updater: " + checks + " checks (real ZIP/filesystem/hash/transaction code; OS trust and launching substituted). No Unity/macOS/Windows execution claimed.");
        return 0;
    }
}

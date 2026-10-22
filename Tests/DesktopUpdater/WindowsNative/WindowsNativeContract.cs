using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using FakeFantasyOnline.DesktopUpdater;
using FakeFantasyOnline.Foundation;

internal static class WindowsNativeContract
{
    private static int passed, failed;
    private static string oldBuild, newBuild, helper;
    private const string Executable = "FixtureGame.exe";
    private const string WrongPin = "0000000000000000000000000000000000000000000000000000000000000000";

    private static int Main(string[] args)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || args.Length != 4)
            throw new InvalidOperationException("Requires Windows and paths to old/new PE fixtures, production helper, and signed dotnet host.");
        oldBuild = Path.GetFullPath(args[0]); newBuild = Path.GetFullPath(args[1]); helper = Path.GetFullPath(args[2]);
        using (var identity = WindowsIdentity.GetCurrent())
        {
            bool admin = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            Console.WriteLine("Windows: " + Environment.OSVersion + "; architecture: " + RuntimeInformation.ProcessArchitecture + "; administrator token: " + admin);
            if (!admin) throw new InvalidOperationException("This hosted-runner suite explicitly requires the admin-refusal case; it does not claim standard-user CLI success.");
        }
        Check(FileVersionInfo.GetVersionInfo(Path.Combine(oldBuild, Executable)).ProductVersion == "1.0.0", "old PE product version");
        Check(FileVersionInfo.GetVersionInfo(Path.Combine(newBuild, Executable)).ProductVersion == "1.0.1", "new PE product version");
        Test("unsigned replacement, real launch survival, resources and MOTW", SuccessfulUpdate);
        Test("real early-exit launch rolls back without an installed receipt", EarlyExitRollback);
        Test("unsigned PE cannot satisfy pinned Authenticode policy", RejectUnsignedPin);
        Test("signed PE rejects wrong publisher pin after native trust check", () => RejectWrongSignedPin(Path.GetFullPath(args[3])));
        Test("real PE version mismatch is rejected before replacement", RejectVersionMismatch);
        Test("production CLI refuses administrator token without preparing", RejectElevatedCli);
        Console.WriteLine("Native Windows results: " + passed + " passed, " + failed + " failed.");
        Console.WriteLine("Scope: production engine/platform Windows APIs and actual CLI admin refusal; NOT standard-user CLI success, interactive SmartScreen, Unity/game health, or macOS validation.");
        return failed == 0 ? 0 : 1;
    }

    private static void Test(string name, Action test)
    {
        Console.WriteLine("RUN: " + name);
        try { test(); passed++; Console.WriteLine("PASS: " + name); }
        catch (Exception error) { failed++; Console.Error.WriteLine("FAIL: " + name + "\n" + error); }
    }
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Assertion failed: " + message); }
    private static T Rejected<T>(Action action, string expectedMessage) where T : Exception
    {
        try { action(); }
        catch (T error)
        {
            Check(error.Message.Contains(expectedMessage, StringComparison.Ordinal), "rejection reason: " + error.Message);
            return error;
        }
        throw new InvalidOperationException("Expected " + typeof(T).Name + ": " + expectedMessage);
    }

    private static void SuccessfulUpdate()
    {
        using (var f = new Fixture("live"))
        using (var engine = new UpdateEngine(f.Request, new DesktopPlatform()))
        {
            engine.Prepare(CancellationToken.None);
            Check(DesktopInstallIdentity.Read(f.Install, "windows").Version == "1.0.0", "prepare leaves installed version unchanged");
            CheckMotw(f.Candidate);
            var clock = Stopwatch.StartNew();
            engine.Commit();
            Check(clock.Elapsed >= TimeSpan.FromSeconds(3), "real three-second survival check ran");
            Check(DesktopInstallIdentity.Read(f.Install, "windows").Version == "1.0.1", "new version installed");
            Check(FileVersionInfo.GetVersionInfo(Path.Combine(f.Install, Executable)).ProductVersion == "1.0.1", "installed PE product version");
            Check(File.Exists(Path.Combine(f.Job, "installed")), "success receipt written after launch");
            Check(DesktopInstallIdentity.Read(engine.BackupPath, "windows").Version == "1.0.0", "old version retained");
            Check(File.Exists(Path.Combine(engine.BackupPath, "old-only.txt")), "original file retained in backup");
            CheckMotw(f.Install);
            f.CheckResources(f.Install);
            Check(!Directory.Exists(Path.Combine(engine.BackupPath, "DownloadedResources")), "resources moved, not copied");
            f.CheckPersistentData();
            using (var process = Process.GetProcessById(int.Parse(File.ReadAllText(Path.Combine(f.Install, "fixture.pid")))))
            {
                Check(!process.HasExited, "confirmed process still alive");
                Check(DesktopPlatform.MatchesExecutable(process.MainModule.FileName, Path.Combine(f.Install, Executable), "windows"), "real process matches installed PE");
                File.WriteAllText(Path.Combine(f.Install, "fixture.stop"), "stop");
                Check(process.WaitForExit(10000), "fixture exits cleanly on stop");
            }
            new DesktopPlatform().RequireApplicationStopped(f.Install, DesktopInstallIdentity.Read(f.Install, "windows"));
        }
    }

    private static void EarlyExitRollback()
    {
        using (var f = new Fixture("exit"))
        using (var engine = new UpdateEngine(f.Request, new DesktopPlatform()))
        {
            engine.Prepare(CancellationToken.None);
            // This exception must come from the real process exiting, not a mocked launch failure.
            Rejected<IOException>(() => engine.Commit(), "exited during startup");
            Check(DesktopInstallIdentity.Read(f.Install, "windows").Version == "1.0.0", "old version restored");
            Check(File.ReadAllText(Path.Combine(f.Install, "old-only.txt")) == "original", "original files restored");
            Check(FileVersionInfo.GetVersionInfo(Path.Combine(f.Install, Executable)).ProductVersion == "1.0.0", "old executable restored");
            Check(!File.Exists(Path.Combine(f.Job, "installed")), "early exit never reports installed");
            Check(File.Exists(Path.Combine(f.Job, "transaction.json")), "recovery journal retained");
            string failedRoot = f.Install + ".update-failed-" + Path.GetFileName(f.Job);
            Check(DesktopInstallIdentity.Read(failedRoot, "windows").Version == "1.0.1", "failed candidate retained");
            CheckMotw(failedRoot);
            f.CheckResources(f.Install);
            Check(!Directory.Exists(Path.Combine(failedRoot, "DownloadedResources")), "resources returned from failed candidate");
            f.CheckPersistentData();
        }
    }

    private static void RejectUnsignedPin()
    {
        using (var f = new Fixture("live", WrongPin))
        using (var engine = new UpdateEngine(f.Request, new DesktopPlatform()))
        {
            Rejected<InvalidDataException>(() => engine.Prepare(CancellationToken.None), "Authenticode trust verification failed");
            Check(DesktopInstallIdentity.Read(f.Install, "windows").Version == "1.0.0", "signed policy rejection leaves old version");
            Check(!File.Exists(Path.Combine(f.Job, "transaction.json")), "no prepared transaction after trust rejection");
            f.CheckResources(f.Install);
        }
    }

    private static void RejectWrongSignedPin(string signedHost)
    {
        Check(File.Exists(signedHost), "signed .NET host exists");
        var identity = new DesktopInstallIdentity {
            Platform = "windows", Executable = Path.GetFileName(signedHost),
            Version = FileVersionInfo.GetVersionInfo(signedHost).ProductVersion,
            MacTeamId = "", WindowsSignerSha256 = WrongPin
        };
        // Exact reason proves WinVerifyTrust succeeded and the real leaf-certificate pin was compared.
        Rejected<InvalidDataException>(() => new DesktopPlatform().ValidateTrust(Path.GetDirectoryName(signedHost), identity),
            "publisher certificate does not match");
    }

    private static void RejectVersionMismatch()
    {
        using (var f = new Fixture("live", "", oldBuild))
        using (var engine = new UpdateEngine(f.Request, new DesktopPlatform()))
        {
            Rejected<InvalidDataException>(() => engine.Prepare(CancellationToken.None), "product version does not match");
            Check(DesktopInstallIdentity.Read(f.Install, "windows").Version == "1.0.0", "PE version mismatch leaves old version");
            Check(!File.Exists(Path.Combine(f.Job, "transaction.json")), "no prepared transaction after version mismatch");
        }
    }

    private static void RejectElevatedCli()
    {
        using (var f = new Fixture("live"))
        {
            f.Request.Save();
            var start = new ProcessStartInfo(helper) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add("--request"); start.ArgumentList.Add(f.Request.RequestPath);
            using (var process = Process.Start(start))
            {
                var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(30000)) { process.Kill(); throw new TimeoutException("Production helper did not refuse the administrator token."); }
                string output = stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult();
                Check(process.ExitCode == 1, "production CLI rejects elevated invocation");
                Check(output.Contains("The updater never operates elevated.", StringComparison.Ordinal), "CLI refuses specifically because elevated");
                Check(File.ReadAllText(f.Request.StatusPath).StartsWith("failed\nRun the game as a standard user.", StringComparison.Ordinal), "refusal status saved");
                Console.WriteLine("Actual CLI exit code: " + process.ExitCode + "; reason: administrator token rejected.");
            }
            Check(!File.Exists(f.Install + ".update.lock"), "CLI guard runs before UpdateEngine construction");
            Check(!Directory.Exists(f.Stage), "CLI never stages an elevated update");
            Check(DesktopInstallIdentity.Read(f.Install, "windows").Version == "1.0.0", "CLI refusal leaves install untouched");
            Check(!File.Exists(Path.Combine(f.Job, "installed")), "CLI refusal does not report success");
            f.CheckResources(f.Install); f.CheckPersistentData();
        }
    }

    private static void CheckMotw(string root)
    {
        foreach (string name in new[] { Executable, "FixtureGame.dll", DesktopInstallIdentity.FileName, "startup-mode.txt" })
            Check(File.ReadAllText(Path.Combine(root, name) + ":Zone.Identifier").Contains("ZoneId=3", StringComparison.Ordinal), "MOTW retained: " + name);
    }
    private static string Marker(string version, string pin)
    {
        return BoundedJson.Serialize(new Dictionary<string, object> {
            { "schemaVersion", 1 }, { "platform", "windows" }, { "version", version },
            { "installId", "com.fakefantasyonline.native-test" }, { "executable", Executable },
            { "macTeamId", "" }, { "windowsSignerSha256", pin }
        });
    }
    private sealed class Fixture : IDisposable
    {
        public readonly string Root, Install, Data, Job, Stage, Candidate;
        public readonly DesktopUpdateRequest Request;
        public Fixture(string mode, string pin = "", string packageBuild = null)
        {
            Root = Path.Combine(Path.GetTempPath(), "ffo-native-case-" + Guid.NewGuid().ToString("N"));
            Install = Path.Combine(Root, "game"); Data = Path.Combine(Root, "user-data");
            Job = Path.Combine(Data, "DesktopUpdates", Guid.NewGuid().ToString("N"));
            Stage = Install + ".update-stage-" + Path.GetFileName(Job); Candidate = Path.Combine(Stage, "game");
            Directory.CreateDirectory(Install); Directory.CreateDirectory(Job);
            foreach (string source in Directory.GetFiles(oldBuild)) File.Copy(source, Path.Combine(Install, Path.GetFileName(source)));
            File.WriteAllText(Path.Combine(Install, DesktopInstallIdentity.FileName), Marker("1.0.0", pin));
            File.WriteAllText(Path.Combine(Install, "old-only.txt"), "original");
            File.WriteAllText(Path.Combine(Install, "startup-mode.txt"), "live");
            Directory.CreateDirectory(Path.Combine(Install, "DownloadedResources", "pending"));
            File.WriteAllText(Path.Combine(Install, "DownloadedResources", "asset.bundle"), "downloaded asset");
            File.WriteAllText(Path.Combine(Install, "DownloadedResources", "pending", "part"), "unfinished download");
            File.WriteAllText(Path.Combine(Data, "settings.json"), "user settings");
            string package = Path.Combine(Data, "package.zip");
            using (var archive = ZipFile.Open(package, ZipArchiveMode.Create))
            {
                foreach (string source in Directory.GetFiles(packageBuild ?? newBuild))
                    archive.CreateEntryFromFile(source, "game/" + Path.GetFileName(source));
                Add(archive, "game/" + DesktopInstallIdentity.FileName, Marker("1.0.1", pin));
                Add(archive, "game/startup-mode.txt", mode);
            }
            using (var process = Process.GetCurrentProcess())
            using (var sha = SHA256.Create())
            using (var input = File.OpenRead(package))
                Request = new DesktopUpdateRequest {
                    InstallRoot = Install, PersistentRoot = Data, JobDirectory = Job, PackagePath = package,
                    Platform = "windows", CurrentVersion = "1.0.0", Version = "1.0.1", ParentProcessId = process.Id,
                    ParentStartTicks = process.StartTime.ToUniversalTime().Ticks, SizeBytes = input.Length,
                    Sha256 = Convert.ToHexString(sha.ComputeHash(input)).ToLowerInvariant()
                };
        }
        private static void Add(ZipArchive archive, string path, string text)
        { using (var writer = new StreamWriter(archive.CreateEntry(path).Open())) writer.Write(text); }
        public void CheckResources(string root)
        {
            Check(File.ReadAllText(Path.Combine(root, "DownloadedResources", "asset.bundle")) == "downloaded asset", "downloaded resource preserved");
            Check(File.ReadAllText(Path.Combine(root, "DownloadedResources", "pending", "part")) == "unfinished download", "partial download preserved");
        }
        public void CheckPersistentData()
        { Check(File.ReadAllText(Path.Combine(Data, "settings.json")) == "user settings", "persistent user data preserved"); }
        public void Dispose()
        {
            // Never stop unrelated programs; only disposable fixture executables under this exact root.
            foreach (var process in Process.GetProcesses())
            using (process)
            {
                try
                {
                    if (!process.HasExited && DesktopUpdateFiles.Below(Root, Path.GetFullPath(process.MainModule.FileName)))
                    { process.Kill(); process.WaitForExit(10000); }
                }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
            }
            for (int attempt = 0; ; attempt++)
            {
                try { Directory.Delete(Root, true); break; }
                catch (IOException) when (attempt < 10) { Thread.Sleep(200); }
                catch (UnauthorizedAccessException) when (attempt < 10) { Thread.Sleep(200); }
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.DesktopUpdater
{
internal sealed class DesktopLaunchUncertainException : IOException
{
    public DesktopLaunchUncertainException(string message) : base(message) { }
}
internal sealed class DesktopPlatform : IDesktopPlatform
{
    private static bool Windows { get { return RuntimeInformation.IsOSPlatform(OSPlatform.Windows); } }
    private readonly Func<string, string, string> run;
    private readonly Action<string, string> verifyWindowsPublisher;
    private readonly Func<string, string> productVersion;
    internal DesktopPlatform(Func<string, string, string> run = null,
        Action<string, string> verifyWindowsPublisher = null, Func<string, string> productVersion = null)
    {
        this.run = run ?? Run;
        this.verifyWindowsPublisher = verifyWindowsPublisher ?? VerifyWindowsPublisher;
        this.productVersion = productVersion ?? (path => FileVersionInfo.GetVersionInfo(path).ProductVersion);
    }
    [DllImport("libc")] private static extern uint geteuid();
    public static void RequireUnprivileged()
    {
        if (Windows)
        {
            using (var identity = WindowsIdentity.GetCurrent())
                if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                    throw new InvalidOperationException("Run the game as a standard user. The updater never operates elevated.");
        }
        else if (geteuid() == 0) throw new InvalidOperationException("The updater refuses to run as root.");
    }
    public void SetPermissions(string path, bool executable)
    {
        if (Windows)
        {
            // Internet provenance is retained for ShellExecute / SmartScreen. Never strip MOTW.
            File.WriteAllText(path + ":Zone.Identifier", "[ZoneTransfer]\r\nZoneId=3\r\n");
        }
        else run("/bin/chmod", (executable ? "700 " : "600 ") + DesktopUpdateFiles.QuoteArgument(path));
    }
    public void ValidateTrust(string root, DesktopInstallIdentity identity)
    {
        string executable = Path.Combine(root, identity.Executable);
        DesktopUpdateFiles.NoLinks(executable);
        if (identity.Platform == "macos")
        {
            // Mark the new app as downloaded. Gatekeeper remains authoritative; never remove quarantine.
            string quarantine = "0083;" + DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString("x") + ";FFODesktopUpdater;" + Guid.NewGuid().ToString();
            run("/usr/bin/xattr", "-w com.apple.quarantine " + DesktopUpdateFiles.QuoteArgument(quarantine) + " " + DesktopUpdateFiles.QuoteArgument(root));
            // Ad-hoc signing still supplies the Mac bundle's code integrity; it is not a paid publisher identity.
            run("/usr/bin/codesign", "--verify --deep --strict " + DesktopUpdateFiles.QuoteArgument(root));
            string signing = run("/usr/bin/codesign", "-d --verbose=4 " + DesktopUpdateFiles.QuoteArgument(root));
            if (!ContainsLine(signing, "Identifier=" + identity.InstallId)) throw new InvalidDataException("macOS application identifier does not match the installed app.");
            if (identity.MacTeamId.Length != 0 && !ContainsLine(signing, "TeamIdentifier=" + identity.MacTeamId))
                throw new InvalidDataException("macOS signing team does not match the installed signing policy.");
            string version = run("/usr/libexec/PlistBuddy", "-c " + DesktopUpdateFiles.QuoteArgument("Print :CFBundleShortVersionString") + " " + DesktopUpdateFiles.QuoteArgument(Path.Combine(root, "Contents/Info.plist"))).Trim();
            if (version != identity.Version) throw new InvalidDataException("macOS bundle version does not match dispatch.");
            // The installed marker is compared with the package before this method. A package/dispatch
            // cannot opt an existing signed installation out of its publisher policy.
            if (identity.MacTeamId.Length != 0)
                run("/usr/sbin/spctl", "--assess --type execute --verbose=4 " + DesktopUpdateFiles.QuoteArgument(root));
        }
        else
        {
            if (identity.WindowsSignerSha256.Length != 0)
                verifyWindowsPublisher(executable, identity.WindowsSignerSha256);
            string version = productVersion(executable);
            if (version != identity.Version) throw new InvalidDataException("Windows executable product version does not match dispatch.");
        }
    }
    private static void VerifyWindowsPublisher(string executable, string expectedSigner)
    {
        VerifyAuthenticode(executable);
        using (var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(executable)))
        using (var sha = SHA256.Create())
            if (!BitConverter.ToString(sha.ComputeHash(certificate.RawData)).Replace("-", "").Equals(expectedSigner, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Windows publisher certificate does not match the installed signing policy.");
    }
    public void StartApplication(string root, DesktopInstallIdentity identity)
    {
        // LaunchServices / ShellExecute retain OS policy checks. No shell script, runas, PowerShell bypass or direct downloaded-code load.
        string executable = Path.GetFullPath(Path.Combine(root, identity.Executable));
        Process application = null;
        bool accepted = false;
        try
        {
            if (identity.Platform == "macos") run("/usr/bin/open", "-n " + DesktopUpdateFiles.QuoteArgument(root));
            else application = Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true, WorkingDirectory = root });
            accepted = true;
            var clock = Stopwatch.StartNew();
            long startTicks = 0;
            ConfirmLaunch(() => {
                if (application == null) application = FindApplication(root, identity, exactExecutable: true);
                if (application == null) return 0; // An accepted request is not proof that an application started.
                application.Refresh();
                if (application.HasExited) return -1;
                if (!MatchesExecutable(DesktopUpdateFiles.ProcessModulePath(application.MainModule), executable, identity.Platform))
                    throw new DesktopLaunchUncertainException("The OS returned a different application process." + RecoveryHint);
                long currentStart = application.StartTime.ToUniversalTime().Ticks;
                if (startTicks != 0 && currentStart != startTicks) return -1; // Never mistake a reused PID for this launch.
                startTicks = currentStart;
                return 1;
            }, () => Thread.Sleep(200), () => clock.Elapsed);
        }
        catch (DesktopLaunchUncertainException) { throw; }
        catch (System.ComponentModel.Win32Exception error)
        {
            if (accepted) throw new DesktopLaunchUncertainException("Cannot determine whether the new application is running: " + error.Message + RecoveryHint);
            throw;
        }
        catch (InvalidOperationException error)
        { throw new DesktopLaunchUncertainException("Cannot determine the new application's launch state: " + error.Message + RecoveryHint); }
        catch (TimeoutException error)
        { throw new DesktopLaunchUncertainException("The OS launch request did not finish: " + error.Message + RecoveryHint); }
        finally { if (application != null) application.Dispose(); }
    }
    private const string RecoveryHint = " Both versions are retained. Dismiss any pending system launch dialog, close all copies, then use --recover with this job's request.json.";
    // Probe: 0 = no process yet, 1 = observed process still running, -1 = that process exited.
    // The short survival window catches immediate startup crashes, not full game/login health.
    internal static void ConfirmLaunch(Func<int> probe, Action pause, Func<TimeSpan> elapsed)
    {
        TimeSpan? observed = null;
        while (true)
        {
            int state;
            try { state = probe(); }
            catch (DesktopLaunchUncertainException) { throw; }
            catch (Exception error)
            { throw new DesktopLaunchUncertainException("Cannot inspect the new application's launch state: " + error.Message + RecoveryHint); }
            if (state < 0) throw new IOException("The new application exited during startup; restoring the previous version.");
            TimeSpan now = elapsed();
            if (state > 0)
            {
                if (!observed.HasValue) observed = now;
                if (now - observed.Value >= TimeSpan.FromSeconds(3)) return;
            }
            if (!observed.HasValue && now >= TimeSpan.FromSeconds(30))
                throw new DesktopLaunchUncertainException("The OS accepted the launch request, but application startup was not confirmed." + RecoveryHint);
            pause();
        }
    }
    public void RequireApplicationStopped(string root, DesktopInstallIdentity identity)
    {
        using (var application = FindApplication(root, identity, exactExecutable: false))
            if (application != null)
                throw new DesktopLaunchUncertainException("An application process is still running; installation, recovery or rollback was not attempted." + RecoveryHint);
    }
    internal static bool MatchesExecutable(string actual, string expected, string platform)
    { return !string.IsNullOrWhiteSpace(actual) && string.Equals(Path.GetFullPath(actual), Path.GetFullPath(expected),
        platform == "windows" ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal); }
    internal static bool MatchesObservedProcess(string path, Func<string> readName, string root,
        DesktopInstallIdentity identity, bool exactExecutable)
    {
        string expected = Path.GetFullPath(Path.Combine(root, identity.Executable));
        if (!string.IsNullOrWhiteSpace(path))
            return exactExecutable ? MatchesExecutable(path, expected, identity.Platform) : DesktopUpdateFiles.Below(root, path);
        string actual;
        try { actual = readName(); }
        catch (System.ComponentModel.Win32Exception)
        { throw new DesktopLaunchUncertainException("A process cannot be identified." + RecoveryHint); }
        string name = Path.GetFileNameWithoutExtension(expected);
        if (string.IsNullOrWhiteSpace(actual) || actual.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            (identity.Platform == "macos" && actual.Length == 15 && name.StartsWith(actual, StringComparison.OrdinalIgnoreCase)))
            throw new DesktopLaunchUncertainException("A possible application process cannot be inspected." + RecoveryHint);
        // An unrelated named system process with no readable module does not belong to this app.
        return false;
    }
    private static Process FindApplication(string root, DesktopInstallIdentity identity, bool exactExecutable)
    {
        var processes = Process.GetProcesses();
        Process match = null;
        try
        {
            foreach (var process in processes)
            {
                try
                {
                    if (process.HasExited) continue;
                    string path;
                    try { path = DesktopUpdateFiles.ProcessModulePath(process.MainModule); }
                    catch (System.ComponentModel.Win32Exception) { path = null; }
                    if (MatchesObservedProcess(path, () => process.ProcessName, root, identity, exactExecutable))
                    { match = process; break; }
                }
                catch (InvalidOperationException)
                {
                    bool exited = false;
                    try { process.Refresh(); exited = process.HasExited; }
                    catch (InvalidOperationException) { }
                    catch (System.ComponentModel.Win32Exception) { }
                    if (!exited) throw new DesktopLaunchUncertainException("A process could not be inspected or confirmed exited." + RecoveryHint);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // HasExited can also be inaccessible; apply the same fail-closed candidate check.
                    if (MatchesObservedProcess(null, () => process.ProcessName, root, identity, exactExecutable))
                    { match = process; break; }
                }
            }
            return match;
        }
        finally { foreach (var process in processes) if (process != match) process.Dispose(); }
    }
    private static bool ContainsLine(string output, string line)
    { foreach (string text in output.Split('\n')) if (text.TrimEnd('\r') == line) return true; return false; }
    private static string Run(string executable, string arguments)
    {
        using (var process = Process.Start(new ProcessStartInfo(executable, arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }))
        {
            if (process == null) throw new IOException("Cannot start system validation tool.");
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(120000)) { process.Kill(); throw new TimeoutException("System tool timed out: " + Path.GetFileName(executable)); }
            string result = stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult();
            if (process.ExitCode != 0) throw new IOException(Path.GetFileName(executable) + " rejected the package: " + result.Trim());
            return result;
        }
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WinTrustFileInfo { public uint Size; [MarshalAs(UnmanagedType.LPWStr)] public string Path; public IntPtr File, Subject; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WinTrustData
    {
        public uint Size; public IntPtr Policy, Sip; public uint UI, Revocation, Choice; public IntPtr File;
        public uint StateAction; public IntPtr State, Url; public uint Flags, Context; public IntPtr SignatureSettings;
    }
    [DllImport("wintrust.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int WinVerifyTrust(IntPtr window, [In] ref Guid action, [In] ref WinTrustData data);
    private static void VerifyAuthenticode(string path)
    {
        var file = new WinTrustFileInfo { Size = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo)), Path = path };
        IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustFileInfo)));
        var action = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
        var data = new WinTrustData { Size = (uint)Marshal.SizeOf(typeof(WinTrustData)), UI = 2, Revocation = 1,
            Choice = 1, File = pointer, StateAction = 1, Flags = 0x80 };
        try
        {
            Marshal.StructureToPtr(file, pointer, false);
            int status = WinVerifyTrust(new IntPtr(-1), ref action, ref data);
            if (status != 0) throw new InvalidDataException("Windows Authenticode trust verification failed (0x" + status.ToString("x8") + ").");
        }
        finally
        {
            data.StateAction = 2; WinVerifyTrust(new IntPtr(-1), ref action, ref data);
            Marshal.DestroyStructure(pointer, typeof(WinTrustFileInfo)); Marshal.FreeHGlobal(pointer);
        }
    }
}
}

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.DesktopUpdater
{
internal sealed class DesktopPlatform : IDesktopPlatform
{
    private static bool Windows { get { return RuntimeInformation.IsOSPlatform(OSPlatform.Windows); } }
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
        else Run("/bin/chmod", (executable ? "700 " : "600 ") + DesktopUpdateFiles.QuoteArgument(path));
    }
    public void ValidateTrust(string root, DesktopInstallIdentity identity)
    {
        string executable = Path.Combine(root, identity.Executable);
        DesktopUpdateFiles.NoLinks(executable);
        if (identity.Platform == "macos")
        {
            // Mark the new app as downloaded. Gatekeeper remains authoritative; never remove quarantine.
            string quarantine = "0083;" + DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString("x") + ";FFODesktopUpdater;" + Guid.NewGuid().ToString();
            Run("/usr/bin/xattr", "-w com.apple.quarantine " + DesktopUpdateFiles.QuoteArgument(quarantine) + " " + DesktopUpdateFiles.QuoteArgument(root));
            Run("/usr/bin/codesign", "--verify --deep --strict " + DesktopUpdateFiles.QuoteArgument(root));
            string signing = Run("/usr/bin/codesign", "-d --verbose=4 " + DesktopUpdateFiles.QuoteArgument(root));
            if (!ContainsLine(signing, "Identifier=" + identity.InstallId)) throw new InvalidDataException("macOS application identifier does not match the installed app.");
            if (identity.MacTeamId.Length != 0 && !ContainsLine(signing, "TeamIdentifier=" + identity.MacTeamId))
                throw new InvalidDataException("macOS signing team does not match the installed signing policy.");
            string version = Run("/usr/libexec/PlistBuddy", "-c " + DesktopUpdateFiles.QuoteArgument("Print :CFBundleShortVersionString") + " " + DesktopUpdateFiles.QuoteArgument(Path.Combine(root, "Contents/Info.plist"))).Trim();
            if (version != identity.Version) throw new InvalidDataException("macOS bundle version does not match dispatch.");
            Run("/usr/sbin/spctl", "--assess --type execute --verbose=4 " + DesktopUpdateFiles.QuoteArgument(root));
        }
        else
        {
            VerifyAuthenticode(executable);
            if (identity.WindowsSignerSha256.Length != 0)
            {
                using (var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(executable)))
                using (var sha = SHA256.Create())
                    if (!BitConverter.ToString(sha.ComputeHash(certificate.RawData)).Replace("-", "").Equals(identity.WindowsSignerSha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Windows publisher certificate does not match the installed signing policy.");
            }
            string version = FileVersionInfo.GetVersionInfo(executable).ProductVersion;
            if (version != identity.Version) throw new InvalidDataException("Windows executable product version does not match dispatch.");
        }
    }
    public void StartApplication(string root, DesktopInstallIdentity identity)
    {
        // LaunchServices / ShellExecute retain OS policy checks. No shell script, runas, PowerShell bypass or direct downloaded-code load.
        if (identity.Platform == "macos") Run("/usr/bin/open", "-n " + DesktopUpdateFiles.QuoteArgument(root));
        else
        {
            using (var process = Process.Start(new ProcessStartInfo(Path.Combine(root, identity.Executable)) { UseShellExecute = true, WorkingDirectory = root }))
                if (process == null) throw new IOException("Windows did not accept the application launch.");
        }
    }
    private static bool ContainsLine(string output, string line)
    { foreach (string text in output.Split('\n')) if (text.TrimEnd('\r') == line) return true; return false; }
    private static string Run(string executable, string arguments)
    {
        using (var process = Process.Start(new ProcessStartInfo(executable, arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }))
        {
            if (process == null) throw new IOException("Cannot start system validation tool.");
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(120000)) { process.Kill(); throw new IOException("System trust validation timed out."); }
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

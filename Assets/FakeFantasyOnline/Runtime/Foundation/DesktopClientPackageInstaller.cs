using System;
using System.Diagnostics;
using System.IO;

namespace FakeFantasyOnline.Foundation
{
/// <summary>Starts only the trusted helper bundled with this app; never a downloaded executable.</summary>
public sealed class DesktopClientPackageInstaller : IDisposable
{
    private readonly DesktopUpdateRequest request;
    private readonly Process helper;
    public string DirectoryPath { get { return request.JobDirectory; } }
    private DesktopClientPackageInstaller(DesktopUpdateRequest request, Process helper)
    { this.request = request; this.helper = helper; }
    public static DesktopClientPackageInstaller Prepare(string installRoot, string streamingAssets, string persistentRoot,
        string package, string platform, string version, ClientUpdatePlan.ClientArtifact artifact)
    {
        var identity = DesktopInstallIdentity.Read(installRoot, platform);
        if (identity.Version != version) throw new InvalidDataException("Desktop install marker does not match the running app.");
        string executable = Path.GetFullPath(Path.Combine(installRoot, identity.Executable));
        using (var process = Process.GetCurrentProcess())
        {
            if (!string.Equals(DesktopUpdateFiles.ProcessModulePath(process.MainModule), executable,
                platform == "windows" ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                throw new InvalidDataException("This process is not the marked desktop executable.");
            string helperName = platform == "windows" ? "DesktopUpdater.exe" :
                System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ?
                "DesktopUpdater-osx-arm64" : "DesktopUpdater-osx-x64";
            string source = Path.Combine(streamingAssets, "DesktopUpdater", helperName);
            DesktopUpdateFiles.NoLinks(source);
            string directory = Path.Combine(persistentRoot, "DesktopUpdates", Guid.NewGuid().ToString("N"));
            DesktopUpdateFiles.NoLinks(directory); Directory.CreateDirectory(directory);
            var request = new DesktopUpdateRequest { InstallRoot = Path.GetFullPath(installRoot), PersistentRoot = Path.GetFullPath(persistentRoot),
                JobDirectory = directory, PackagePath = Path.GetFullPath(package), Platform = platform, CurrentVersion = version,
                Version = artifact.Version, Sha256 = artifact.Sha256, SizeBytes = artifact.SizeBytes,
                ParentProcessId = process.Id, ParentStartTicks = process.StartTime.ToUniversalTime().Ticks };
            request.Save();
            string target = Path.Combine(directory, helperName);
            File.Copy(source, target, false);
            if (platform == "macos")
            {
                using (var chmod = Process.Start(new ProcessStartInfo("/bin/chmod", "700 " + DesktopUpdateFiles.QuoteArgument(target)) { UseShellExecute = false, CreateNoWindow = true }))
                { if (chmod == null || !chmod.WaitForExit(10000) || chmod.ExitCode != 0) throw new IOException("Unable to prepare packaged updater permissions."); }
            }
            var started = Process.Start(new ProcessStartInfo(target, "--request " + DesktopUpdateFiles.QuoteArgument(request.RequestPath)) {
                UseShellExecute = false, WorkingDirectory = directory, CreateNoWindow = true
            });
            if (started == null) throw new IOException("The operating system could not start the bundled updater.");
            return new DesktopClientPackageInstaller(request, started);
        }
    }
    public string Poll()
    {
        DesktopUpdateFiles.NoLinks(request.StatusPath);
        if (File.Exists(request.StatusPath))
        {
            if (new FileInfo(request.StatusPath).Length > 8192) throw new InvalidDataException("Invalid updater status.");
            string status = File.ReadAllText(request.StatusPath);
            if (!helper.HasExited || status.StartsWith("failed\n") || status == "cancelled" || status == "installed") return status;
        }
        return helper.HasExited ? "failed\nThe desktop updater stopped. The current app and retained backup have not been deleted." : "preparing";
    }
    public void Commit()
    {
        if (Poll() != "ready") throw new InvalidOperationException("The desktop update is not prepared.");
        DesktopUpdateFiles.WriteNew(request.CommitPath, "install-after-exit\n");
    }
    public void Cancel()
    {
        if (!File.Exists(request.CancelPath) && !File.Exists(request.CommitPath)) DesktopUpdateFiles.WriteNew(request.CancelPath, "cancel\n");
    }
    public void Dispose() { helper.Dispose(); }
}
}

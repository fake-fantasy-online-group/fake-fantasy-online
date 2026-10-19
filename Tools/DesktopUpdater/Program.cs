using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.DesktopUpdater
{
internal static class Program
{
    private static int Main(string[] args)
    {
        DesktopUpdateRequest request = null;
        try
        {
            if (args.Length != 2 || (args[0] != "--request" && args[0] != "--recover"))
                throw new ArgumentException("Usage: DesktopUpdater --request <request.json> | --recover <request.json> (quit the game first for recovery)");
            request = DesktopUpdateRequest.Read(Path.GetFullPath(args[1]));
            bool windows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
            bool mac = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);
            if ((request.Platform == "windows" && !windows) || (request.Platform == "macos" && !mac))
                throw new PlatformNotSupportedException("The updater does not match this operating system.");
            if (args[0] == "--recover")
            {
                if (ParentAlive(request)) throw new IOException("Quit the original game process before recovering.");
                DesktopPlatform.RequireUnprivileged();
                RequireApplicationStopped(request);
                UpdateEngine.Recover(request); Status(request, "recovered"); return 0;
            }
            // Never request elevation or run as an elevated helper. Protected installs require manual installation.
            DesktopPlatform.RequireUnprivileged();
            Status(request, "preparing");
            using (var engine = new UpdateEngine(request, new DesktopPlatform()))
            using (var cancellation = new CancellationTokenSource())
            using (var timer = new Timer(_ => { if (File.Exists(request.CancelPath)) cancellation.Cancel(); }, null, 0, 200))
            {
                engine.Prepare(cancellation.Token); Status(request, "ready");
                var waiting = Stopwatch.StartNew();
                while (!File.Exists(request.CommitPath))
                {
                    cancellation.Token.ThrowIfCancellationRequested();
                    if (!ParentAlive(request)) throw new OperationCanceledException("The game closed without confirming installation.");
                    if (waiting.Elapsed > TimeSpan.FromMinutes(30)) throw new OperationCanceledException("Update confirmation expired; prepare the package again.");
                    Thread.Sleep(200);
                }
                cancellation.Token.ThrowIfCancellationRequested();
                DesktopUpdateFiles.NoLinks(request.CommitPath);
                if (new FileInfo(request.CommitPath).Length != 19 || File.ReadAllText(request.CommitPath) != "install-after-exit\n") throw new InvalidDataException("Invalid update confirmation.");
                Status(request, "waiting-for-exit"); waiting.Restart();
                while (ParentAlive(request))
                {
                    if (waiting.Elapsed > TimeSpan.FromMinutes(5)) throw new IOException("The game did not exit. No application files were replaced.");
                    Thread.Sleep(200);
                }
                RequireApplicationStopped(request);
                Status(request, "installing"); engine.Commit(); Status(request, "installed"); return 0;
            }
        }
        catch (OperationCanceledException exception)
        { if (request != null) SafeStatus(request, "cancelled"); Console.Error.WriteLine(exception.Message); return 2; }
        catch (Exception exception)
        {
            if (request != null) SafeStatus(request, "failed\n" + exception.Message + "\nIf the application cannot start, quit it and run this helper with --recover and the same request.json. Retained applications are never deleted.");
            Console.Error.WriteLine(exception); return 1;
        }
    }
    private static void RequireApplicationStopped(DesktopUpdateRequest request)
    {
        // Covers a second copy started while the original game was preparing the update.
        foreach (var process in Process.GetProcesses())
        using (process)
        {
            string executable;
            try { if (process.HasExited) continue; executable = process.MainModule.FileName; }
            catch (System.ComponentModel.Win32Exception) { continue; } // Other users/system processes cannot be inspected.
            catch (InvalidOperationException) { continue; }
            if (DesktopUpdateFiles.Below(request.InstallRoot, Path.GetFullPath(executable)))
                throw new IOException("Close every copy of this application before installation or recovery.");
        }
    }
    private static bool ParentAlive(DesktopUpdateRequest request)
    {
        try
        {
            using (var process = Process.GetProcessById(request.ParentProcessId))
            {
                if (process.HasExited) return false;
                long delta = Math.Abs(process.StartTime.ToUniversalTime().Ticks - request.ParentStartTicks);
                // Unity/Mono and .NET can expose different subsecond precision on macOS.
                // A live same-PID process within that precision window must be waited for.
                if (delta <= TimeSpan.TicksPerSecond * 2) return true;
                string executable = Path.GetFullPath(process.MainModule.FileName);
                // A reused PID running another copy of this app is also a reason to wait.
                return DesktopUpdateFiles.Below(request.InstallRoot, executable);
            }
        }
        catch (ArgumentException) { return false; }
    }
    private static void SafeStatus(DesktopUpdateRequest request, string value)
    { try { Status(request, value); } catch (Exception exception) { Console.Error.WriteLine("Status could not be written: " + exception.Message); } }
    private static void Status(DesktopUpdateRequest request, string value)
    {
        if (value.Length > 2000) value = value.Substring(0, 2000);
        DesktopUpdateFiles.NoLinks(request.StatusPath);
        string temporary = request.StatusPath + "." + Guid.NewGuid().ToString("N");
        DesktopUpdateFiles.WriteNew(temporary, value);
        if (File.Exists(request.StatusPath)) File.Replace(temporary, request.StatusPath, null);
        else File.Move(temporary, request.StatusPath);
    }
}
}

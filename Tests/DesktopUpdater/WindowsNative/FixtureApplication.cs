using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

// A real unsigned Windows PE apphost, built twice with distinct product versions.
// No network, GUI, user configuration or installation is involved.
internal static class FixtureApplication
{
    private static int Main()
    {
        string root = AppContext.BaseDirectory;
        File.WriteAllText(Path.Combine(root, "fixture.pid"), Environment.ProcessId.ToString());
        if (File.ReadAllText(Path.Combine(root, "startup-mode.txt")).Trim() == "exit") return 23;
        var lifetime = Stopwatch.StartNew();
        while (!File.Exists(Path.Combine(root, "fixture.stop")) && lifetime.Elapsed < TimeSpan.FromMinutes(2))
            Thread.Sleep(100);
        return 0;
    }
}

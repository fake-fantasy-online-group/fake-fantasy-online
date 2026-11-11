using System;
using System.IO;
using FakeFantasyOnline.Protocol;

// Engine-free contract for the debug-only server.json store (compiled with -define:FFO_DEBUG_SERVER_SWITCH).
public static class DebugServerSwitchStoreContract
{
    static int checks;
    static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    static void Reject(Action action, string message)
    {
        try { action(); } catch (InvalidDataException) { checks++; return; } catch (ArgumentException) { checks++; return; }
        throw new Exception("Expected rejection: " + message);
    }
    public static void Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "ffo-debug-switch-" + Guid.NewGuid().ToString("N"));
        string user = Path.Combine(root, "user"), bundle = Path.Combine(root, "bundle");
        string file = Path.Combine(user, DebugServerSwitchStore.FileName);
        Directory.CreateDirectory(bundle);
        try
        {
            Check(DebugServerSwitchStore.DescribeNext(user, bundle) == "线上", "no file means online");
            Check(!DebugServerSwitchStore.SelectOnline(user), "online when already online");
            DebugServerSwitchStore.SelectLocal(user, DebugServerSwitchStore.DefaultLocalHost, DebugServerSwitchStore.DefaultLocalPort);
            Check(File.ReadAllText(file) == "{\"host\":\"127.0.0.1\",\"port\":19000}\n", "existing server.json format");
            var local = PrivateServerOptions.FromLocalServerFiles(user, bundle);
            Check(local.LocalServer.host == "127.0.0.1" && local.LocalServer.port == 19000 && local.DispatchUri == null, "startup reads choice");
            Check(DebugServerSwitchStore.Describe(local) == "本地 127.0.0.1:19000", "running local");
            Check(DebugServerSwitchStore.DescribeNext(user, bundle) == "本地 127.0.0.1:19000", "next local");
            DebugServerSwitchStore.SelectLocal(user, "localhost", 19001);
            Check(PrivateServerOptions.FromLocalServerFiles(user, bundle).LocalServer.port == 19001, "replace previous choice");
            foreach (var bad in new[] { Tuple.Create("0.0.0.0", 19000), Tuple.Create("http://example.test", 19000),
                Tuple.Create("127.0.0.1", 0), Tuple.Create("127.0.0.1", 65536), Tuple.Create("", 19000), Tuple.Create(" 127.0.0.1", 19000),
                Tuple.Create("a\",\"port\":1,\"x\":\"", 19000), Tuple.Create("a\nb", 19000) })
            {
                Reject(() => DebugServerSwitchStore.SelectLocal(user, bad.Item1, bad.Item2), bad.Item1 + ":" + bad.Item2);
                Check(PrivateServerOptions.FromLocalServerFiles(user, bundle).LocalServer.port == 19001, "invalid choice keeps previous file");
            }
            Check(Directory.GetFiles(user).Length == 1, "no temporary files remain");
            Check(DebugServerSwitchStore.SelectOnline(user) && !File.Exists(file), "online deletes server.json");
            Check(PrivateServerOptions.FromLocalServerFiles(user, bundle) == null, "startup falls back to dispatch");
            var online = PrivateServerOptions.MobileConfigured("https://dispatch.example.test/dispatch.json");
            Check(DebugServerSwitchStore.Describe(online) == "线上 dispatch.example.test", "running online");
            File.WriteAllText(file, "{\"host\":\"127.0.0.1\"}");
            Reject(() => DebugServerSwitchStore.DescribeNext(user, bundle), "invalid saved file is reported");
            DebugServerSwitchStore.SelectLocal(user, "127.0.0.1", 19000);
            Check(DebugServerSwitchStore.DescribeNext(user, bundle) == "本地 127.0.0.1:19000", "local choice repairs invalid file");
        }
        finally { Directory.Delete(root, true); }
        Console.WriteLine("PASS debug server switch store: " + checks + " assertions (server.json write/replace/delete, startup validation reused)");
    }
}

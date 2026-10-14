using System;
using System.IO;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Protocol;

internal static class MobileResourceContract
{
    private static int checks;
    private static void Assert(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    private static void Reject(Action action, string name)
    { try { action(); } catch (ArgumentException) { checks++; return; } catch (InvalidDataException) { checks++; return; } throw new Exception(name); }
    public static void Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "ffo-mobile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string source = Path.Combine(root, "source");
            File.WriteAllText(source, "package fixture");
            string row = "ReferenceClient/packages/a.pkg\tdata/a.pkg\t" + new FileInfo(source).Length + "\t" + MobileResourceManifest.HashFile(source) + "\n";
            var manifest = MobileResourceManifest.Parse("FFO-MOBILE-1\n" + row);
            Assert(manifest.Entries.Count == 1, "one entry");
            Assert(manifest.Version == MobileResourceManifest.Parse("FFO-MOBILE-1\n" + row).Version, "stable version");
            var entry = manifest.Entries[0];
            Assert(MobileResourceManifest.Verify(source, entry), "valid checksum");
            string target = Path.Combine(root, "installed");
            File.Copy(source, target + ".part");
            MobileResourceManifest.CommitFile(target + ".part", target, entry);
            Assert(File.Exists(target) && !File.Exists(target + ".part"), "commit verified file");
            File.WriteAllText(target + ".part", "corrupt");
            Reject(() => MobileResourceManifest.CommitFile(target + ".part", target, entry), "reject partial transfer");
            Assert(MobileResourceManifest.Verify(target, entry), "previous file preserved on failed validation");
            File.WriteAllText(target, "corrupted package");
            Assert(!MobileResourceManifest.Verify(target, entry), "detect corrupt installed bytes");
            foreach (string path in new[] { "../escape", "a/../../b", "/absolute", "jar:file:x", "a\\b", "a//b", "a/%2e%2e/b", "a?query", "a/./b", "a/", "a.pkg.part" })
                Reject(() => MobileResourceManifest.ValidateRelativePath(path), "reject unsafe path " + path);
            Reject(() => MobileResourceManifest.Parse("FFO-MOBILE-1\n" + row + row), "duplicate destination");
            Reject(() => MobileResourceManifest.Parse("FFO-MOBILE-1\n"), "empty manifest");
            Reject(() => MobileResourceManifest.Parse("wrong\n" + row), "schema version");
            Assert(ClientResourcePaths.Resolve("desktop/path", false) == "desktop/path", "desktop root unchanged");
            bool blocked = false; try { ClientResourcePaths.Resolve("jar:file:///apk!/assets", true); } catch (InvalidOperationException) { blocked = true; }
            Assert(blocked, "mobile cannot open APK before install");
            ClientResourcePaths.Activate(root);
            Assert(ClientResourcePaths.Resolve("jar:file:///apk!/assets", true) == root, "mobile installed root");
            foreach (string url in new[] { "http://127.0.0.1:8088/dispatch.json", "http://localhost/", "http://[::1]/", "http://0.0.0.0/", "ftp://example.com/", "http://u:p@example.com/", "invalid" })
                Reject(() => PrivateServerOptions.MobileConfigured(url), "reject mobile endpoint " + url);
            Assert(PrivateServerOptions.MobileConfigured("https://game.example.com/dispatch.json").DispatchUri.Host == "game.example.com", "explicit mobile endpoint");
            Assert(PrivateServerOptions.LocalDefault().DispatchUri.IsLoopback, "desktop local default preserved");
            Console.WriteLine("PASS mobile resource/platform contracts (" + checks + " checks)");
        }
        finally { Directory.Delete(root, true); }
    }
}

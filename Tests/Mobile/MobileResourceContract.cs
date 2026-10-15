using System;
using System.IO;
using System.Security.Cryptography;
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
            var offline = MobileResourceManifest.Parse("FFO-MOBILE-OFFLINE-1\n" + row);
            Assert(offline.Offline && !manifest.Offline, "explicit transport modes");
            Assert(offline.Version != manifest.Version, "transport mode belongs to version");
            string offlineRoot = Path.Combine(root, "offline");
            Reject(() => offline.VerifyDirectory(offlineRoot), "incomplete offline transfer blocked");
            Directory.CreateDirectory(Path.Combine(offlineRoot, "data"));
            File.Copy(source, Path.Combine(offlineRoot, "data/a.pkg"));
            offline.VerifyDirectory(offlineRoot); checks++;
            File.WriteAllText(Path.Combine(offlineRoot, "data/a.pkg"), "broken");
            Reject(() => offline.VerifyDirectory(offlineRoot), "corrupt offline transfer blocked");
            File.Copy(source, Path.Combine(offlineRoot, "data/a.pkg"), true);
            offline.VerifyDirectory(offlineRoot); checks++;
            string stagingRoot = Path.Combine(root, "StreamingAssets");
            string backup = Path.Combine(root, "Temp/original");
            Directory.CreateDirectory(stagingRoot);
            File.WriteAllText(Path.Combine(stagingRoot, "original"), "preserve");
            using (new MobileStreamingAssetsStage(stagingRoot, backup, "FFO-MOBILE-OFFLINE-1\n" + row))
            {
                Assert(!File.Exists(Path.Combine(stagingRoot, "original")), "giant source excluded from stage");
                Assert(File.Exists(Path.Combine(backup, "original")), "original retained outside build assets");
                Assert(Directory.GetFiles(stagingRoot).Length == 1, "only manifest bundled offline");
            }
            Assert(File.ReadAllText(Path.Combine(stagingRoot, "original")) == "preserve", "identical source restored");
            Assert(!Directory.Exists(backup), "backup restored not deleted");
            Directory.CreateDirectory(backup);
            bool staleBlocked = false;
            try { new MobileStreamingAssetsStage(stagingRoot, backup, "FFO-MOBILE-OFFLINE-1\n" + row); }
            catch (IOException) { staleBlocked = true; }
            Assert(staleBlocked && File.Exists(Path.Combine(stagingRoot, "original")), "interrupted stage refuses destructive retry");
            Directory.Delete(backup);

            Assert(manifest.Version == MobileResourceManifest.Parse("FFO-MOBILE-1\n" + row).Version, "stable version");
            var entry = manifest.Entries[0];
            string large = Path.Combine(root, "large");
            var payload = new byte[700000];
            for (int i = 0; i < payload.Length; i++) payload[i] = (byte)(i % 251);
            File.WriteAllBytes(large, payload);
            long previous = -1;
            int callbacks = 0;
            string streamed = MobileResourceManifest.HashFile(large, bytes =>
            {
                Assert(bytes >= previous && bytes <= payload.Length, "monotonic actual hashed bytes");
                previous = bytes; callbacks++;
            });
            using (var sha = SHA256.Create())
                Assert(streamed == BitConverter.ToString(sha.ComputeHash(payload)).Replace("-", "").ToLowerInvariant(), "streaming hash matches SHA256");
            Assert(previous == payload.Length && callbacks >= 4, "progress within large files and exact completion");
            long reported = -1;
            Assert(MobileResourceManifest.Verify(source, entry, bytes => reported = bytes) && reported == entry.Length,
                "verification reports actual bytes");
            reported = -1;
            Assert(!MobileResourceManifest.Verify(Path.Combine(root, "absent"), entry, bytes => reported = bytes) && reported == -1,
                "missing file never invents progress");
            File.WriteAllBytes(large, new byte[0]);
            reported = -1;
            Assert(MobileResourceManifest.HashFile(large, bytes => reported = bytes) ==
                "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" && reported == 0,
                "empty resource has correct hash and zero progress");
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

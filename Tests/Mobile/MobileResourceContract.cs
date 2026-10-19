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
            TestReceipts(root);
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
    private static void TestReceipts(string root)
    {
        string installed = Path.Combine(root, "receipt-files");
        Directory.CreateDirectory(installed);
        string a = Path.Combine(installed, "a.pkg"), b = Path.Combine(installed, "table.json");
        File.WriteAllText(a, "package bytes"); File.WriteAllText(b, "table bytes");
        Func<string> manifestText = () => "FFO-MOBILE-OFFLINE-1\n" +
            "a.pkg\ta.pkg\t" + new FileInfo(a).Length + "\t" + MobileResourceManifest.HashFile(a) + "\n" +
            "table.json\ttable.json\t" + new FileInfo(b).Length + "\t" + MobileResourceManifest.HashFile(b) + "\n";
        var manifest = MobileResourceManifest.Parse(manifestText());
        string receipt = Path.Combine(root, "receipt");
        var first = new MobileResourceVerification(manifest, installed, receipt);
        Assert(first.CachedFiles == 0 && first.NeedsHash(0) && first.NeedsHash(1), "first install hashes packages AND nonpackages");
        Reject(() => first.CommitReceipt(), "cannot write receipt before verification");
        long actual = 0;
        Assert(first.Verify(0, bytes => actual = bytes) && actual == new FileInfo(a).Length, "cold hash byte progress");
        Assert(first.Verify(1), "cold nonpackage verification");
        first.CommitReceipt();
        string goodReceipt = File.ReadAllText(receipt);
        var warm = new MobileResourceVerification(manifest, installed, receipt);
        actual = -1;
        Assert(warm.CachedFiles == 2 && warm.BytesToHash == 0 && !warm.NeedsHash(0), "warm startup has zero hash bytes");
        Assert(new MobileResourceVerification(manifest, installed, receipt, false).CachedFiles == 0,
            "explicit full verification bypasses metadata-only receipts");
        Assert(warm.Verify(0, bytes => actual = bytes) && actual == -1, "warm verification never reads payload");
        warm.CommitReceipt();
        Assert(File.ReadAllText(receipt) == goodReceipt, "atomic replacement preserves complete receipt");
        File.WriteAllText(receipt + ".part", "interrupted");
        Assert(new MobileResourceVerification(manifest, installed, receipt).CachedFiles == 2, "partial receipt never overrides previous valid receipt");
        File.Delete(receipt);
        Assert(new MobileResourceVerification(manifest, installed, receipt).CachedFiles == 0, "partial first receipt is not success");
        File.WriteAllText(receipt, goodReceipt);
        foreach (string broken in new[] { "", "junk", goodReceipt.Substring(0, goodReceipt.Length / 2), goodReceipt.Replace("FFO-VERIFIED-1", "FFO-VERIFIED-9"), goodReceipt.Replace("a", "b") })
        {
            File.WriteAllText(receipt, broken);
            Assert(new MobileResourceVerification(manifest, installed, receipt).CachedFiles == 0, "malformed receipt requires full verification");
        }
        File.WriteAllText(receipt, goodReceipt);
        File.SetLastWriteTimeUtc(a, File.GetLastWriteTimeUtc(a).AddSeconds(5));
        var changed = new MobileResourceVerification(manifest, installed, receipt);
        Assert(changed.NeedsHash(0) && !changed.NeedsHash(1) && changed.CachedFiles == 1, "mtime change only rehashes changed file");
        Assert(changed.Verify(0), "same content changed timestamp passes hash");
        changed.CommitReceipt();
        goodReceipt = File.ReadAllText(receipt);
        File.WriteAllText(a, "Package bytes"); // Same length; different content and mtime.
        File.SetLastWriteTimeUtc(a, File.GetLastWriteTimeUtc(a).AddSeconds(5));
        var corrupt = new MobileResourceVerification(manifest, installed, receipt);
        Assert(!corrupt.Verify(0), "same length changed timestamp corruption rejected");
        Reject(() => corrupt.CommitReceipt(), "failed verification never saves success");
        Assert(File.ReadAllText(receipt) == goodReceipt, "failed check preserves old receipt only");
        var updatedManifest = MobileResourceManifest.Parse(manifestText());
        Assert(new MobileResourceVerification(updatedManifest, installed, receipt).CachedFiles == 0, "changed embedded manifest invalidates whole receipt");
        File.WriteAllText(a, "short");
        var truncated = new MobileResourceVerification(manifest, installed, receipt);
        Assert(truncated.NeedsHash(0) && !truncated.Verify(0), "length changes reject cached file");
        File.Delete(a);
        var missing = new MobileResourceVerification(manifest, installed, receipt);
        Assert(missing.NeedsHash(0) && !missing.Verify(0), "missing file rejected despite cache");
        string temporary = a + ".part";
        File.WriteAllText(temporary, "package bytes");
        actual = -1;
        missing.CommitFile(0, temporary, bytes => actual = bytes);
        Assert(actual == manifest.Entries[0].Length && !File.Exists(temporary), "installed file verified once with byte progress");
        missing.CommitReceipt();
        var raced = new MobileResourceVerification(manifest, installed, receipt);
        File.SetLastWriteTimeUtc(b, File.GetLastWriteTimeUtc(b).AddSeconds(5));
        Reject(() => raced.CommitReceipt(), "metadata changed after scan blocks activation");
        var duringHash = new MobileResourceVerification(manifest, installed, receipt);
        Assert(!duringHash.Verify(1, bytes => { if (bytes > 0) File.SetLastWriteTimeUtc(b, File.GetLastWriteTimeUtc(b).AddSeconds(5)); }),
            "change while hashing cannot be cached");
        File.WriteAllText(receipt, goodReceipt.Replace(manifest.Version, new string('0', 64)));
        Assert(new MobileResourceVerification(manifest, installed, receipt).CachedFiles == 0, "stale fingerprint rejected");
    }

}

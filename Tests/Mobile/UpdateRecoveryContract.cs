using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using FakeFantasyOnline.Foundation;

internal static class UpdateRecoveryContract
{
    private static int checks;
    private const string Url = "https://cdn.example.com/release/resources.tsv";
    private static readonly byte[] A = Encoding.UTF8.GetBytes("package number one"), B = Encoding.UTF8.GetBytes("package number two"), C = Encoding.UTF8.GetBytes("changed package two");
    private static string Hash(byte[] bytes) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    private static string Row(string path, byte[] bytes) { return path + "\t" + path + "\t" + bytes.Length + "\t" + Hash(bytes) + "\n"; }
    private static string Manifest(byte[] second = null) { return "FFO-MOBILE-1\n@client\tandroid\t1.0.0\n" + Row("data/a.pkg", A) + Row("data/b.pkg", second ?? B); }
    private static void Assert(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    private static void Reject(Action action, string name)
    { try { action(); } catch (Exception e) when (e is InvalidDataException || e is IOException || e is ArgumentException || e is OperationCanceledException) { checks++; return; } throw new Exception(name); }
    private sealed class FakeTransport : IMobileResourceResumableTransport
    {
        public readonly Dictionary<string, byte[]> Bodies = new Dictionary<string, byte[]>();
        public readonly List<string> Downloads = new List<string>();
        public readonly List<long> Offsets = new List<long>();
        public string InterruptPath;
        public int InterruptAfter = 4;
        public CancellationTokenSource CancelAfterDownload;
        public FakeTransport(string manifest = null, string url = Url)
        {
            Bodies[url] = Encoding.UTF8.GetBytes(manifest ?? Manifest());
            Bodies[new Uri(new Uri(url), "data/a.pkg").AbsoluteUri] = A;
            Bodies[new Uri(new Uri(url), "data/b.pkg").AbsoluteUri] = B;
        }
        public void Download(Uri uri, string path, long maximum, CancellationToken token, Action<long> progress)
        {
            Downloads.Add(uri.AbsoluteUri);
            using (var input = new MemoryStream(Bodies[uri.AbsoluteUri]))
            using (var output = File.Create(path)) MobileResourceHttpTransport.CopyBounded(input, output, maximum, token, progress);
        }
        public void DownloadResumable(Uri uri, string path, long expected, long offset, CancellationToken token, Action<long> progress)
        {
            Downloads.Add(uri.AbsoluteUri); Offsets.Add(offset);
            byte[] bytes = Bodies[uri.AbsoluteUri];
            using (var output = new FileStream(path, offset == 0 ? FileMode.Create : FileMode.Append))
            {
                int count = uri.AbsolutePath == InterruptPath ? Math.Min(InterruptAfter, bytes.Length - (int)offset) : bytes.Length - (int)offset;
                output.Write(bytes, (int)offset, count); output.Flush(true); progress?.Invoke(offset + count);
                if (uri.AbsolutePath == InterruptPath) throw new IOException("simulated connection failure");
            }
            CancelAfterDownload?.Cancel();
        }
    }
    private static MobileRemoteResourceInstall Installer(string root, FakeTransport http, Func<string, long> space = null, string platform = "android")
    { return new MobileRemoteResourceInstall(root, "1.0.0", false, http, platform, space ?? (path => long.MaxValue), maxConcurrentDownloads: 1); }
    private static string Stage(string root, string manifest = null) { return Path.Combine(root, MobileResourceManifest.Parse(manifest ?? Manifest()).Version + ".staging"); }
    private static string Marker(string root) { return File.ReadAllText(Path.Combine(root, "active-resource-set.txt")); }

    public static void Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "ffo-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            TestFastAndRepair(Path.Combine(root, "fast"));
            TestCrossManifest(Path.Combine(root, "reuse"));
            TestIdentity(Path.Combine(root, "identity"));
            TestSpace(Path.Combine(root, "space"));
            TestResume(Path.Combine(root, "resume"));
            TestResumeBinding(Path.Combine(root, "binding"));
            TestCompletePartial(Path.Combine(root, "complete"));
            TestCorruptResume(Path.Combine(root, "corrupt"));
            TestStagingReceipt(Path.Combine(root, "receipt"));
            TestAncestorLink(Path.Combine(root, "link"));
            TestCancellationBeforeActivation(Path.Combine(root, "cancel"));
            Console.WriteLine("PASS update recovery contracts (" + checks + " checks; simulated transfers and temporary directories only)");
        }
        finally { Directory.Delete(root, true); }
    }

    private static void TestFastAndRepair(string root)
    {
        string active = Installer(root, new FakeTransport()).Install(Url, CancellationToken.None, null);
        var warm = new FakeTransport(); long hashed = 0;
        string second = Installer(root, warm).Install(Url, CancellationToken.None,
            (phase, detail, done, total) => { if (phase == "Checking installed resources") hashed += done; });
        Assert(second == active && warm.Downloads.Count == 1 && hashed == 0, "warm start only fetches manifest and reads receipt/metadata");
        var phases = new HashSet<string>();
        Installer(root, new FakeTransport()).UseInstalled(CancellationToken.None,
            (phase, detail, done, total) => { if (phase == "Checking installed resources" && done > 0) phases.Add(detail); }, true);
        Assert(phases.Count == 2, "manual verification hashes every installed entry");
        string file = Path.Combine(active, "data/a.pkg"); DateTime stamp = File.GetLastWriteTimeUtc(file);
        File.WriteAllBytes(file, new byte[A.Length]); File.SetLastWriteTimeUtc(file, stamp);
        Assert(Installer(root, new FakeTransport()).UseInstalled(CancellationToken.None, null) == active, "receipt cache is explicitly metadata-only and not tamper-proof");
        Reject(() => Installer(root, new FakeTransport()).UseInstalled(CancellationToken.None, null, true), "manual repair detects equal-length equal-timestamp corruption");
        var repair = new FakeTransport();
        string fixedRoot = Installer(root, repair).Install(Url, CancellationToken.None, null, 0, null, true);
        Assert(fixedRoot != active && Directory.Exists(active), "repair stages a new generation without modifying old payload");
        Assert(repair.Downloads.Count == 2 && repair.Downloads[1].EndsWith("data/a.pkg"), "repair downloads only corrupt content, reuses other fully verified content");
        Assert(MobileResourceManifest.HashFile(Path.Combine(fixedRoot, "data/a.pkg")) == Hash(A), "repaired package hash matches manifest");
    }

    private static void TestCrossManifest(string root)
    {
        string old = Installer(root, new FakeTransport()).Install(Url, CancellationToken.None, null);
        string renamed = Manifest().Replace("data/a.pkg", "data/renamed.pkg");
        var reuse = new FakeTransport(renamed);
        string next = Installer(root, reuse).Install(Url, CancellationToken.None, null);
        Assert(reuse.Downloads.Count == 1 && next != old, "same SHA and length reuse across manifest and path changes without payload HTTP");
        Assert(File.Exists(Path.Combine(next, "data/renamed.pkg")) && Directory.Exists(old), "reused data lives in independent final generation, old root remains");
        string changed = Manifest(C); var delta = new FakeTransport(changed); delta.Bodies[new Uri(new Uri(Url), "data/b.pkg").AbsoluteUri] = C;
        string last = Installer(root, delta).Install(Url, CancellationToken.None, null);
        Assert(delta.Downloads.Count == 2 && delta.Downloads[1].EndsWith("data/b.pkg"), "only changed content downloads across manifests");
        Assert(Directory.Exists(next) && File.ReadAllBytes(Path.Combine(last, "data/b.pkg")).Length == C.Length, "previous generation remains after successful update");
    }

    private static void TestIdentity(string root)
    {
        byte[] manifest = Encoding.UTF8.GetBytes(Manifest());
        var wrong = new FakeTransport();
        Reject(() => Installer(root, wrong).Install(Url, CancellationToken.None, null, manifest.Length, new string('0', 64)), "dispatch hash mismatch stops install");
        Assert(wrong.Downloads.Count == 1 && !File.Exists(Path.Combine(root, "active-resource-set.txt")), "mismatched manifest never requests payload or activates");
        Reject(() => Installer(root, new FakeTransport()).Install(Url, CancellationToken.None, null, manifest.Length + 1, Hash(manifest)), "dispatch byte length mismatch stops install");
        Reject(() => Installer(root, new FakeTransport()).Install(Url, CancellationToken.None, null, 0, Hash(manifest)), "manifest pins must be paired");
        string installed = Installer(root, new FakeTransport()).Install(Url, CancellationToken.None, null, manifest.Length, Hash(manifest));
        Assert(Directory.Exists(installed), "correct raw UTF8 manifest pins install");
        foreach (string platform in new[] { "windows", "macos", "linux" })
        {
            string text = Manifest().Replace("android", platform);
            MobileRemoteResourceInstall.ValidateManifest(MobileResourceManifest.Parse(text), "1.0.0", platform); checks++;
            Reject(() => MobileRemoteResourceInstall.ValidateManifest(MobileResourceManifest.Parse(text), "1.0.0"), "platforms cannot silently cross-match");
        }
        Reject(() => MobileRemoteResourceInstall.ValidateManifest(MobileResourceManifest.Parse(Manifest()), "1.0.0", "any"), "unsupported wildcard platform rejected");
    }

    private static void TestSpace(string root)
    {
        var denied = new FakeTransport();
        Reject(() => Installer(root, denied, path => 0).Install(Url, CancellationToken.None, null), "no-space preflight blocks before payload transfer");
        Assert(denied.Downloads.Count == 1 && !File.Exists(Path.Combine(root, "active-resource-set.txt")), "space failure cannot activate");
        string stage = Stage(root); Directory.CreateDirectory(Path.Combine(stage, "data"));
        File.WriteAllBytes(Path.Combine(stage, "data/a.pkg"), A);
        long onlyRemaining = MobileRemoteResourceInstall.RequiredFreeBytes(B.Length, Encoding.UTF8.GetByteCount(Manifest()), 2);
        var partial = new FakeTransport();
        string active = Installer(root, partial, path => onlyRemaining).Install(Url, CancellationToken.None, null);
        Assert(partial.Downloads.Count == 2 && Directory.Exists(active), "verified complete staging excluded from space requirement on retry");
        string marker = Marker(root); var update = new FakeTransport(Manifest(C));
        Reject(() => Installer(root, update, path => 0).Install(Url, CancellationToken.None, null), "insufficient update space rejected");
        Assert(Marker(root) == marker && Directory.Exists(active), "space failure preserves active marker and payload");
    }

    private static void TestResume(string root)
    {
        var interrupted = new FakeTransport { InterruptPath = "/release/data/a.pkg" };
        Reject(() => Installer(root, interrupted).Install(Url, CancellationToken.None, null), "interrupted download stops");
        string part = Path.Combine(Stage(root), "data/a.pkg.part");
        Assert(new FileInfo(part).Length == 4 && File.Exists(part + ".resume"), "partial bytes and bound identity survive interruption");
        var resumed = new FakeTransport();
        string active = Installer(root, resumed).Install(Url, CancellationToken.None, null);
        Assert(resumed.Offsets[0] == 4, "retry requests exactly the retained byte offset");
        Assert(MobileResourceManifest.HashFile(Path.Combine(active, "data/a.pkg")) == Hash(A), "resumed bytes receive final full hash");
        Assert(Directory.GetFiles(active, "*.part*", SearchOption.AllDirectories).Length == 0, "active generation excludes partial and resume files");
    }

    private static void TestResumeBinding(string root)
    {
        var initial = new FakeTransport { InterruptPath = "/release/data/a.pkg" };
        Reject(() => Installer(root, initial).Install(Url, CancellationToken.None, null), "initial bound partial created");
        string otherUrl = "https://mirror.example.com/release/resources.tsv";
        var other = new FakeTransport(null, otherUrl);
        Installer(root, other).Install(otherUrl, CancellationToken.None, null);
        Assert(other.Offsets[0] == 0, "same manifest from different URL cannot reuse partial response identity");
        string oversizeRoot = root + "-oversize";
        Reject(() => Installer(oversizeRoot, new FakeTransport { InterruptPath = "/release/data/a.pkg" }).Install(Url, CancellationToken.None, null), "oversize fixture interrupted");
        File.WriteAllBytes(Path.Combine(Stage(oversizeRoot), "data/a.pkg.part"), new byte[A.Length + 1]);
        var oversized = new FakeTransport(); Installer(oversizeRoot, oversized).Install(Url, CancellationToken.None, null);
        Assert(oversized.Offsets[0] == 0, "oversized retained part restarts and cannot make negative space estimate");
        string damagedRoot = root + "-metadata";
        Reject(() => Installer(damagedRoot, new FakeTransport { InterruptPath = "/release/data/a.pkg" }).Install(Url, CancellationToken.None, null), "damaged identity fixture interrupted");
        string receipt = Path.Combine(Stage(damagedRoot), "data/a.pkg.part.resume");
        byte[] damaged = File.ReadAllBytes(receipt); damaged[0] = 0xff; File.WriteAllBytes(receipt, damaged);
        var metadata = new FakeTransport(); Installer(damagedRoot, metadata).Install(Url, CancellationToken.None, null);
        Assert(metadata.Offsets[0] == 0, "invalid UTF8 partial identity safely restarts");
    }

    private static void TestCompletePartial(string root)
    {
        using (var cancelled = new CancellationTokenSource())
        {
            var first = new FakeTransport { CancelAfterDownload = cancelled };
            Reject(() => Installer(root, first).Install(Url, cancelled.Token, null), "cancellation between completed download and hash stops activation");
        }
        var retry = new FakeTransport(); string active = Installer(root, retry).Install(Url, CancellationToken.None, null);
        Assert(retry.Downloads.Count == 2 && retry.Downloads[1].EndsWith("data/b.pkg"), "full retained partial is verified and committed without another HTTP request");
        Assert(Directory.Exists(active), "post-download cancellation is recoverable");
    }

    private static void TestCorruptResume(string root)
    {
        Reject(() => Installer(root, new FakeTransport { InterruptPath = "/release/data/a.pkg" }).Install(Url, CancellationToken.None, null), "create partial for corruption test");
        string part = Path.Combine(Stage(root), "data/a.pkg.part"); File.WriteAllBytes(part, new byte[4]);
        Reject(() => Installer(root, new FakeTransport()).Install(Url, CancellationToken.None, null), "bad retained prefix fails final full hash");
        Assert(!File.Exists(part) && !File.Exists(part + ".resume") && !File.Exists(Path.Combine(root, "active-resource-set.txt")), "bad completed partial is removed without activation");
        var retry = new FakeTransport(); Installer(root, retry).Install(Url, CancellationToken.None, null);
        Assert(retry.Offsets[0] == 0, "hash failure resets the next transfer to byte zero");
    }

    private static void TestStagingReceipt(string root)
    {
        string stage = Stage(root); Directory.CreateDirectory(Path.Combine(stage, "data"));
        File.WriteAllBytes(Path.Combine(stage, "data/a.pkg"), A); File.WriteAllBytes(Path.Combine(stage, "data/b.pkg"), B);
        var verification = new MobileResourceVerification(MobileResourceManifest.Parse(Manifest()), stage, Path.Combine(stage, ".resource-verified"));
        verification.Verify(0); verification.Verify(1); verification.CommitReceipt();
        string path = Path.Combine(stage, "data/a.pkg"); DateTime stamp = File.GetLastWriteTimeUtc(path);
        File.WriteAllBytes(path, new byte[A.Length]); File.SetLastWriteTimeUtc(path, stamp);
        // Normal starts intentionally trust unchanged size/mtime receipts. Only
        // explicit repair bypasses that metadata cache and detects this fixture.
        var http = new FakeTransport(); string active = Installer(root, http).Install(Url, CancellationToken.None, null, fullVerification: true);
        Assert(http.Downloads.Count == 2 && http.Downloads[1].EndsWith("data/a.pkg"), "explicit repair ignores staging metadata-only receipt and hashes retained bytes");
        Assert(MobileResourceManifest.HashFile(Path.Combine(active, "data/a.pkg")) == Hash(A), "only complete verified staging activates");
    }

    private static void TestAncestorLink(string root)
    {
        Directory.CreateDirectory(root);
        string outside = Path.Combine(root, "outside"); Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "sentinel"), "keep");
        string link = Path.Combine(root, "linked-parent");
        using (var process = Process.Start(new ProcessStartInfo("ln", "-s \"" + outside + "\" \"" + link + "\"") { UseShellExecute = false }))
        { process.WaitForExit(); Assert(process.ExitCode == 0, "ancestor link fixture created"); }
        var http = new FakeTransport();
        Reject(() => Installer(Path.Combine(link, "storage"), http).Install(Url, CancellationToken.None, null), "storage ancestor symlink is rejected before any network or write");
        Assert(http.Downloads.Count == 0 && Directory.GetFileSystemEntries(outside).Length == 1 && File.ReadAllText(Path.Combine(outside, "sentinel")) == "keep", "ancestor symlink leaves its target untouched");
    }

    private static void TestCancellationBeforeActivation(string root)
    {
        string old = Installer(root, new FakeTransport()).Install(Url, CancellationToken.None, null);
        string marker = Marker(root);
        using (var cancelled = new CancellationTokenSource())
        {
            var http = new FakeTransport(Manifest(C)); http.Bodies[new Uri(new Uri(Url), "data/b.pkg").AbsoluteUri] = C;
            Reject(() => Installer(root, http).Install(Url, cancelled.Token,
                (phase, detail, done, total) => { if (phase == "Finishing resource check") cancelled.Cancel(); }), "cancel before final activation keeps old marker");
        }
        Assert(Marker(root) == marker && Directory.Exists(old), "completed staging remains inactive after cancellation");
    }

}

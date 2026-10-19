using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using FakeFantasyOnline.Foundation;

internal static class ClientArtifactDownloadContract
{
    private static int checks;
    private static readonly byte[] Bytes = Encoding.UTF8.GetBytes("synthetic inert client package bytes; never a real installer");
    private const string Url = "https://updates.example.test/releases/1.1.0/client.apk";
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
    private static void Reject<T>(Action action, string label) where T : Exception
    { try { action(); } catch (T) { checks++; return; } throw new Exception("Accepted " + label); }
    private static string Hash(byte[] bytes)
    { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    private static ClientUpdatePlan.ClientArtifact Artifact(string url = Url, string version = "1.1.0", long? size = null, string hash = null)
    { return new ClientUpdatePlan.ClientArtifact { Url = url, Version = version, SizeBytes = size ?? Bytes.Length, Sha256 = hash ?? Hash(Bytes) }; }
    private static ClientArtifactDownload Downloader(string root, IMobileResourceTransport transport, string platform = "android",
        Func<string, long> available = null, bool development = false)
    { return new ClientArtifactDownload(root, "1.0.0", platform, development, transport, available ?? (path => long.MaxValue)); }
    private static string[] Find(string root, string pattern)
    { return Directory.Exists(root) ? Directory.GetFiles(root, pattern, SearchOption.AllDirectories) : new string[0]; }
    private static string One(string root, string pattern)
    { string[] matches = Find(root, pattern); Check(matches.Length == 1, "exactly one " + pattern); return matches[0]; }

    private sealed class FakeTransport : IMobileResourceResumableTransport
    {
        public byte[] Body = Bytes;
        public long LastOffset, LastExpected;
        public string LastUrl, LastPath;
        public int Calls, FailAfter = -1;
        public bool Pause, IgnoreRange;
        public Action BeforeWrite, AfterWrite;
        public void Download(Uri uri, string path, long maximumBytes, CancellationToken cancellation, Action<long> progress)
        { DownloadResumable(uri, path, maximumBytes, 0, cancellation, progress); }
        public void DownloadResumable(Uri uri, string path, long expected, long offset, CancellationToken cancellation, Action<long> progress)
        {
            Calls++; LastOffset = offset; LastExpected = expected; LastUrl = uri.AbsoluteUri; LastPath = path;
            BeforeWrite?.Invoke();
            long initial = IgnoreRange ? 0 : offset;
            using (var output = new FileStream(path, initial > 0 ? FileMode.Open : FileMode.Create, FileAccess.Write, FileShare.None))
            {
                output.Position = initial;
                progress?.Invoke(initial);
                int end = FailAfter < 0 ? Body.Length : Math.Min(Body.Length, (int)initial + FailAfter);
                for (int i = (int)initial; i < end; i++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    output.WriteByte(Body[i]);
                    progress?.Invoke(i + 1);
                }
                output.Flush(true);
                if (FailAfter >= 0)
                {
                    if (Pause) throw new OperationCanceledException("Synthetic pause");
                    throw new IOException("Synthetic network interruption or storage failure");
                }
            }
            AfterWrite?.Invoke();
        }
    }
    private sealed class BasicTransport : IMobileResourceTransport
    {
        public int Calls;
        public void Download(Uri uri, string path, long maximumBytes, CancellationToken cancellation, Action<long> progress)
        {
            Calls++;
            using (var input = new MemoryStream(Bytes))
            using (var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                MobileResourceHttpTransport.CopyBounded(input, output, maximumBytes, cancellation, progress);
        }
    }

    public static void Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "ffo-artifact-contract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            TestValidation(Path.Combine(root, "validation"));
            TestDownloadAndReverify(Path.Combine(root, "download"));
            TestPartialAndPause(Path.Combine(root, "pause"));
            TestIdentity(Path.Combine(root, "identity"));
            TestFailures(Path.Combine(root, "failures"));
            TestSpace(Path.Combine(root, "space"));
            TestLinks(Path.Combine(root, "links"));
            TestLock(Path.Combine(root, "lock"));
            Console.WriteLine("PASS client artifact download contract (" + checks + " assertions; synthetic bytes only, no network, Unity, devices or installer execution)");
        }
        finally { Directory.Delete(root, true); }
    }

    private static void TestValidation(string root)
    {
        var transport = new FakeTransport(); var downloader = Downloader(root, transport);
        foreach (string version in new[] { "1.0.0", "0.9.9", "1.1", "v1.1.0", "01.1.0", "1.1.0-rc.1", "1.1.0+build", "4294967296.0.0" })
            Reject<InvalidDataException>(() => downloader.Download(Artifact(version: version), CancellationToken.None), "version " + version);
        foreach (long size in new[] { 0L, -1L, ClientArtifactDownload.MaxArtifactBytes + 1, long.MaxValue })
            Reject<InvalidDataException>(() => downloader.Download(Artifact(size: size), CancellationToken.None), "size bound");
        foreach (string hash in new[] { "", "abc", new string('g', 64), new string('a', 63) })
            Reject<InvalidDataException>(() => downloader.Download(Artifact(hash: hash), CancellationToken.None), "bad digest");
        foreach (string url in new[] { "http://example.test/a.apk", "http://192.168.1.2/a.apk", "https://localhost/a.apk", "https://127.0.0.1/a.apk",
            "https://u:p@example.test/a.apk", "https://example.test/a.apk?token=x", "https://example.test/a.apk#fragment", "file:///tmp/a.apk",
            "https://example.test/client.exe", "https://example.test/.apk", "https://example.test/client.apk/", "https://example.test/a%2eapk",
            "https://example.test/client.apk.cs", "https://example.test/a%2fb.apk", "https://example.test/client.apk%20" })
            Reject<InvalidDataException>(() => downloader.Download(Artifact(url: url), CancellationToken.None), "URL/type " + url);
        Reject<InvalidDataException>(() => downloader.Download(null, CancellationToken.None), "missing artifact");
        Check(transport.Calls == 0 && !Directory.Exists(root), "invalid metadata does not touch cache or transport");
        Reject<InvalidDataException>(() => new ClientArtifactDownload(root, "1.0.0-rc.1", "android"), "bad current version");
        Reject<InvalidDataException>(() => new ClientArtifactDownload(root, "1.0.0", "ios"), "unsupported platform");
        Reject<ArgumentException>(() => new ClientArtifactDownload(Path.GetPathRoot(root), "1.0.0", "android"), "filesystem root cache");
        var platforms = new Dictionary<string, string[]> { { "android", new[] { "apk" } }, { "windows", new[] { "exe", "msi", "zip" } },
            { "macos", new[] { "dmg", "pkg", "zip" } }, { "linux", new[] { "AppImage", "tar.gz", "deb", "rpm", "zip" } } };
        foreach (var pair in platforms)
            foreach (string extension in pair.Value)
            {
                string path = Downloader(Path.Combine(root, pair.Key, extension), new FakeTransport(), pair.Key)
                    .Download(Artifact(url: "https://updates.example.test/client." + extension), CancellationToken.None);
                Check(path.EndsWith("package." + extension, StringComparison.Ordinal), "platform extension " + pair.Key + "/" + extension);
            }
        string dev = Downloader(Path.Combine(root, "dev"), new FakeTransport(), development: true)
            .Download(Artifact(url: "http://192.168.1.2/client.apk"), CancellationToken.None);
        Check(File.Exists(dev), "explicit development LAN HTTP accepted");
        Reject<InvalidDataException>(() => Downloader(root, new FakeTransport(), development: true)
            .Download(Artifact(url: "http://8.8.8.8/client.apk"), CancellationToken.None), "development cannot enable public HTTP");
    }

    private static void TestDownloadAndReverify(string root)
    {
        var transport = new FakeTransport(); var downloader = Downloader(root, transport); var artifact = Artifact();
        var phases = new List<string>(); long total = 0, transferred = 0;
        string completed = downloader.Download(artifact, CancellationToken.None, (phase, detail, done, all) => {
            phases.Add(phase); Check(done >= 0 && done <= all, "bounded progress");
            if (phase == "Downloading client package") { total = all; transferred = done; }
        });
        Check(transport.Calls == 1 && transport.LastOffset == 0 && transport.LastExpected == Bytes.Length, "bounded first transfer");
        Check(completed.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.Ordinal), "cache confinement");
        Check(completed.IndexOf(Hash(Bytes), StringComparison.Ordinal) >= 0, "content-addressed cache");
        Check(total == Bytes.Length && transferred == Bytes.Length, "network progress totals");
        Check(phases.Contains("Verifying client package") && phases[phases.Count - 1] == "Client package ready", "separate verify and completion phases");
        Check(Hash(File.ReadAllBytes(completed)) == artifact.Sha256 && Find(root, "download.part").Length == 0, "verified atomic publication");
        long hashed = 0;
        Check(downloader.Download(artifact, CancellationToken.None, (phase, detail, done, all) => {
            if (phase == "Checking client package") hashed = done;
        }) == completed && transport.Calls == 1 && hashed == Bytes.Length, "cache hit fully hashes without a request");
        hashed = 0;
        Check(downloader.VerifyCompleted(artifact, CancellationToken.None, (phase, detail, done, all) => {
            if (phase == "Checking client package") hashed = done;
        }) == completed && hashed == Bytes.Length, "handoff rehashes every byte");
        DateTime timestamp = File.GetLastWriteTimeUtc(completed);
        var changed = (byte[])Bytes.Clone(); changed[0] ^= 1;
        File.WriteAllBytes(completed, changed); File.SetLastWriteTimeUtc(completed, timestamp);
        Reject<InvalidDataException>(() => downloader.VerifyCompleted(artifact, CancellationToken.None), "same length/timestamp forgery");
        Check(transport.Calls == 1, "verify never requests download");
        Check(downloader.Download(artifact, CancellationToken.None) == completed && transport.Calls == 2, "corrupt cache re-download");
        File.WriteAllBytes(completed, new byte[1]);
        Reject<InvalidDataException>(() => downloader.VerifyCompleted(artifact, CancellationToken.None), "truncated completed package");
        downloader.Download(artifact, CancellationToken.None);
        File.Delete(completed);
        Reject<InvalidDataException>(() => downloader.VerifyCompleted(artifact, CancellationToken.None), "missing completed package");
    }

    private static void TestPartialAndPause(string root)
    {
        var transport = new FakeTransport { FailAfter = 11, Pause = true }; var downloader = Downloader(root, transport); var artifact = Artifact();
        Reject<OperationCanceledException>(() => downloader.Download(artifact, CancellationToken.None), "pause");
        string part = One(root, "download.part"); Check(new FileInfo(part).Length == 11, "pause keeps actual partial bytes");
        Check(Find(root, "package.apk").Length == 0, "pause never publishes package");
        Reject<InvalidDataException>(() => downloader.VerifyCompleted(artifact, CancellationToken.None), "paused bytes not handoff ready");
        transport.FailAfter = -1;
        string completed = downloader.Download(artifact, CancellationToken.None);
        Check(transport.LastOffset == 11 && Hash(File.ReadAllBytes(completed)) == artifact.Sha256, "resume then full-file hash");
        File.Move(completed, part); int calls = transport.Calls;
        completed = downloader.Download(artifact, CancellationToken.None);
        Check(transport.Calls == calls && File.Exists(completed), "complete partial verifies and publishes without network");
        File.Move(completed, part); File.WriteAllBytes(part, new byte[Bytes.Length]);
        downloader.Download(artifact, CancellationToken.None);
        Check(transport.LastOffset == 0 && transport.Calls == calls + 1, "corrupt full partial restarts from zero");
        File.Move(completed, part); File.WriteAllBytes(part, new byte[Bytes.Length + 1]);
        downloader.Download(artifact, CancellationToken.None);
        Check(transport.LastOffset == 0, "oversized retained partial discarded");
        string ignoreRoot = root + "-range"; transport = new FakeTransport { FailAfter = 7 };
        downloader = Downloader(ignoreRoot, transport);
        Reject<IOException>(() => downloader.Download(artifact, CancellationToken.None), "interrupted response");
        transport.FailAfter = -1; transport.IgnoreRange = true;
        completed = downloader.Download(artifact, CancellationToken.None);
        Check(transport.LastOffset == 7 && Hash(File.ReadAllBytes(completed)) == artifact.Sha256, "server ignoring Range restarts and verifies");
        var cancellation = new CancellationTokenSource(); cancellation.Cancel(); calls = transport.Calls;
        Reject<OperationCanceledException>(() => downloader.Download(artifact, cancellation.Token), "pre-cancel");
        Check(transport.Calls == calls, "pre-cancel does not transfer");
        string basicRoot = root + "-basic"; var partialTransport = new FakeTransport { FailAfter = 5 };
        Reject<IOException>(() => Downloader(basicRoot, partialTransport).Download(artifact, CancellationToken.None), "basic setup");
        var basic = new BasicTransport();
        Check(File.Exists(Downloader(basicRoot, basic).Download(artifact, CancellationToken.None)) && basic.Calls == 1, "non-resumable interface supported safely");
    }

    private static void TestIdentity(string root)
    {
        var transport = new FakeTransport { FailAfter = 9 }; var downloader = Downloader(root, transport); var artifact = Artifact();
        Reject<IOException>(() => downloader.Download(artifact, CancellationToken.None), "identity fixture");
        string firstPart = One(root, "download.part"), originalIdentity = One(root, "identity.txt");
        transport.FailAfter = -1;
        string second = downloader.Download(Artifact(url: "https://mirror.example.test/client.apk"), CancellationToken.None);
        Check(transport.LastOffset == 0 && new FileInfo(firstPart).Length == 9 && File.Exists(second), "different URL never resumes old partial");
        string third = downloader.Download(Artifact(version: "1.2.0"), CancellationToken.None);
        Check(third != second && transport.LastOffset == 0 && new FileInfo(firstPart).Length == 9, "exact version participates in identity");
        File.WriteAllText(originalIdentity, "wrong metadata"); int calls = transport.Calls;
        Reject<InvalidDataException>(() => downloader.Download(artifact, CancellationToken.None), "tampered resume metadata");
        Check(calls == transport.Calls && new FileInfo(firstPart).Length == 9, "bad identity never requests or modifies retained bytes");
        File.Delete(originalIdentity);
        string completed = downloader.Download(artifact, CancellationToken.None);
        Check(transport.LastOffset == 0 && File.Exists(completed), "unbound orphan partial discarded");
        string snapshotRoot = root + "-snapshot"; var mutable = Artifact(); var snapshotTransport = new FakeTransport();
        snapshotTransport.BeforeWrite = () => { mutable.Url = "https://other.example.test/client.apk"; mutable.Sha256 = new string('f', 64); mutable.SizeBytes = 1; mutable.Version = "9.0.0"; };
        completed = Downloader(snapshotRoot, snapshotTransport).Download(mutable, CancellationToken.None);
        Check(snapshotTransport.LastUrl == Url && snapshotTransport.LastExpected == Bytes.Length && File.ReadAllBytes(completed).Length == Bytes.Length,
            "artifact metadata snapshot cannot change during transfer");
        Reject<InvalidDataException>(() => Downloader(snapshotRoot, snapshotTransport).VerifyCompleted(mutable, CancellationToken.None), "mutated plan cannot handoff old package");
    }

    private static void TestFailures(string root)
    {
        foreach (int length in new[] { 1, Bytes.Length, Bytes.Length + 1 })
        {
            string scope = Path.Combine(root, "wrong-" + length); var transport = new FakeTransport { Body = new byte[length] };
            Reject<InvalidDataException>(() => Downloader(scope, transport).Download(Artifact(), CancellationToken.None), "wrong length or digest");
            Check(Find(scope, "package.apk").Length == 0 && Find(scope, "download.part").Length == 0, "bad final bytes never published or resumed");
        }
        string paused = Path.Combine(root, "cancel-hash"); var cancellation = new CancellationTokenSource();
        Reject<OperationCanceledException>(() => Downloader(paused, new FakeTransport()).Download(Artifact(), cancellation.Token,
            (phase, detail, done, total) => { if (phase == "Verifying client package") cancellation.Cancel(); }), "cancel hashing");
        Check(Find(paused, "package.apk").Length == 0 && new FileInfo(One(paused, "download.part")).Length == Bytes.Length,
            "cancel during verification retains resumable bytes without success");
        string old = Path.Combine(root, "old-client.exe"); File.WriteAllText(old, "existing running client sentinel");
        string scope2 = Path.Combine(root, "move-conflict"); var conflictTransport = new FakeTransport();
        conflictTransport.AfterWrite = () => Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(conflictTransport.LastPath), "package.apk"));
        Reject<IOException>(() => Downloader(scope2, conflictTransport).Download(Artifact(), CancellationToken.None), "atomic move conflict");
        Check(File.ReadAllText(old) == "existing running client sentinel" && Find(scope2, "package.apk").Length == 0,
            "failure leaves original client untouched and no completed file");
        string metaScope = Path.Combine(root, "meta-change"); var metaTransport = new FakeTransport();
        metaTransport.AfterWrite = () => File.WriteAllText(Path.Combine(Path.GetDirectoryName(metaTransport.LastPath), "identity.txt"), "changed");
        Reject<InvalidDataException>(() => Downloader(metaScope, metaTransport).Download(Artifact(), CancellationToken.None), "changed identity before verification");
        Check(Find(metaScope, "package.apk").Length == 0, "identity change never publishes");
    }

    private static void TestSpace(string root)
    {
        var transport = new FakeTransport(); string scope = Path.Combine(root, "empty");
        Reject<IOException>(() => Downloader(scope, transport, available: path => Bytes.Length + ClientArtifactDownload.StorageReserveBytes - 1)
            .Download(Artifact(), CancellationToken.None), "one byte below required storage");
        Check(transport.Calls == 0 && Find(scope, "download.part").Length == 0, "space check before payload");
        string completed = Downloader(scope, transport, available: path => Bytes.Length + ClientArtifactDownload.StorageReserveBytes)
            .Download(Artifact(), CancellationToken.None);
        Check(File.Exists(completed), "exact storage boundary");
        Check(Downloader(scope, transport, available: path => 0).Download(Artifact(), CancellationToken.None) == completed,
            "existing verified package requires no extra download space");
        scope = Path.Combine(root, "partial"); transport = new FakeTransport { FailAfter = 10 };
        Reject<IOException>(() => Downloader(scope, transport).Download(Artifact(), CancellationToken.None), "partial space fixture");
        transport.FailAfter = -1;
        completed = Downloader(scope, transport, available: path => Bytes.Length - 10 + ClientArtifactDownload.StorageReserveBytes)
            .Download(Artifact(), CancellationToken.None);
        Check(transport.LastOffset == 10 && File.Exists(completed), "space preflight credits only matching retained bytes");
    }

    private static void MakeLink(string path, string target, bool directory)
    {
        MethodInfo method = (directory ? typeof(Directory) : typeof(File)).GetMethod("CreateSymbolicLink", new[] { typeof(string), typeof(string) });
        if (method == null) throw new Exception("This offline link contract requires the .NET test host's symbolic-link API.");
        method.Invoke(null, new object[] { path, target });
    }
    private static void TestLinks(string root)
    {
        Directory.CreateDirectory(root); string outside = Path.Combine(root, "outside"); Directory.CreateDirectory(outside);
        string parent = Path.Combine(root, "parent"); MakeLink(parent, outside, true);
        var transport = new FakeTransport();
        Reject<InvalidDataException>(() => Downloader(Path.Combine(parent, "nested", "cache"), transport).Download(Artifact(), CancellationToken.None), "symlink anywhere above cache");
        Check(transport.Calls == 0 && Directory.GetFileSystemEntries(outside).Length == 0, "ancestor link rejected before filesystem writes");
        Directory.Delete(parent);
        string broken = Path.Combine(root, "broken"); MakeLink(broken, Path.Combine(outside, "absent"), false);
        Reject<InvalidDataException>(() => Downloader(broken, transport).Download(Artifact(), CancellationToken.None), "dangling cache link");
        File.Delete(broken);
        string scope = Path.Combine(root, "leaf"); transport.FailAfter = 4;
        Reject<IOException>(() => Downloader(scope, transport).Download(Artifact(), CancellationToken.None), "link setup");
        string part = One(scope, "download.part"); string target = Path.Combine(outside, "sentinel"); File.WriteAllText(target, "untouched");
        File.Delete(part); MakeLink(part, target, false);
        Reject<InvalidDataException>(() => Downloader(scope, transport).Download(Artifact(), CancellationToken.None), "partial symlink");
        Check(File.ReadAllText(target) == "untouched", "partial link target untouched"); File.Delete(part);
        transport.FailAfter = -1;
        string completed = Downloader(scope, transport).Download(Artifact(), CancellationToken.None);
        File.Delete(completed); MakeLink(completed, target, false);
        Reject<InvalidDataException>(() => Downloader(scope, transport).VerifyCompleted(Artifact(), CancellationToken.None), "completed symlink before handoff");
        Reject<InvalidDataException>(() => Downloader(scope, transport).Download(Artifact(), CancellationToken.None), "completed symlink before reuse");
        Check(File.ReadAllText(target) == "untouched", "completed link target untouched"); File.Delete(completed);
        string identity = One(scope, "identity.txt"); File.Delete(identity); MakeLink(identity, target, false);
        Reject<InvalidDataException>(() => Downloader(scope, transport).Download(Artifact(), CancellationToken.None), "identity symlink"); File.Delete(identity);
        string lockPath = Path.Combine(scope, ".client-download.lock"); File.Delete(lockPath); MakeLink(lockPath, target, false);
        Reject<InvalidDataException>(() => Downloader(scope, transport).Download(Artifact(), CancellationToken.None), "lock symlink"); File.Delete(lockPath);
    }

    private static void TestLock(string root)
    {
        Directory.CreateDirectory(root); var transport = new FakeTransport();
        using (var held = new FileStream(Path.Combine(root, ".client-download.lock"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            Reject<IOException>(() => Downloader(root, transport).Download(Artifact(), CancellationToken.None), "concurrent cache owner");
        Check(transport.Calls == 0, "concurrent owner rejected before transfer");
    }
}

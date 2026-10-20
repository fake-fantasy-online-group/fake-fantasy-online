using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using FakeFantasyOnline.Foundation;

// Pure managed integration contracts. All responses are tiny in-memory fixtures;
// no production endpoint, loopback listener, Unity runtime or device is involved.
internal static class ArchiveResourceInstallContract
{
    private const string ManifestUrl = "https://cdn.example.com/releases/1/mobile-resources.tsv";
    private const string ArchiveUrl = "https://cdn.example.com/releases/1/first-install.tar";
    private const string RootPrefix = "Assets/StreamingAssets/";
    private static readonly string[] Sources = { "packages/a.pkg", "packages/b.pkg", "tables/c.json", "tables/d.json" };
    private static readonly string[] Destinations = { "data/a.pkg", "data/b.pkg", "gameassets/tables/c.json", "gameassets/tables/d.json" };
    private static readonly int[] Archived = { 0, 1, 2 };
    private static int checks;

    private static void Assert(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++;
    }

    private static void Throws<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { checks++; return; }
        throw new Exception("Expected " + typeof(T).Name + ": " + message);
    }

    private static string Hash(byte[] bytes)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    private static string SourceUrl(int index)
    {
        return MobileRemoteResourceInstall.ResourceUri(new Uri(ManifestUrl), Sources[index]).AbsoluteUri;
    }

    private sealed class Fixture
    {
        public readonly string Platform, ManifestText;
        public readonly byte[][] Payloads;
        public readonly byte[] ManifestBytes, ArchiveBytes;
        public readonly MobileResourceManifest Manifest;
        public readonly ClientUpdatePlan Plan;

        public Fixture(string platform = "android", int generation = 0, int[] archived = null,
            Func<byte[], byte[]> changeArchive = null, string archiveHash = null)
        {
            Platform = platform;
            Payloads = new byte[4][];
            int[] lengths = { 32768, 16384, 8192, 1024 };
            var text = new StringBuilder("FFO-MOBILE-1\n@client\t").Append(platform).Append("\t1.0.0\n");
            for (int i = 0; i < Payloads.Length; i++)
            {
                Payloads[i] = new byte[lengths[i]];
                for (int p = 0; p < Payloads[i].Length; p++) Payloads[i][p] = (byte)(17 + i + p % 97 + generation);
                text.Append(Sources[i]).Append('\t').Append(Destinations[i]).Append('\t')
                    .Append(Payloads[i].Length).Append('\t').Append(Hash(Payloads[i])).Append('\n');
            }
            ManifestText = text.ToString();
            ManifestBytes = Encoding.UTF8.GetBytes(ManifestText);
            Manifest = MobileResourceManifest.Parse(ManifestText);
            int[] included = archived ?? Archived;
            ArchiveBytes = Tar(Payloads, included);
            if (changeArchive != null) ArchiveBytes = changeArchive(ArchiveBytes);
            var archive = new Dictionary<string, object> {
                { "url", ArchiveUrl }, { "format", "tar" }, { "sizeBytes", ArchiveBytes.Length },
                { "sha256", archiveHash ?? Hash(ArchiveBytes) }, { "rootPrefix", RootPrefix }
            };
            var manifest = new Dictionary<string, object> {
                { "manifestUrl", ManifestUrl }, { "sizeBytes", ManifestBytes.Length }, { "sha256", Hash(ManifestBytes) },
                { "archive", archive }
            };
            var update = new Dictionary<string, object> {
                { "schemaVersion", 1 },
                { "resourceManifests", Map(platform, Map("1.0.0", manifest)) }
            };
            string json = BoundedJson.Serialize(new Dictionary<string, object> {
                { "servers", new List<object> { new Dictionary<string, object> { { "name", "fixture" } } } },
                { "update", update }
            });
            Plan = ClientUpdatePlan.Parse(json, platform, "1.0.0", false);
            Assert(Plan.ResourceArchive != null, "Fixture dispatch selects its exact platform/version archive");
        }

        private static Dictionary<string, object> Map(string key, object value)
        {
            return new Dictionary<string, object> { { key, value } };
        }

        public long PayloadBytes
        {
            get { long result = 0; foreach (byte[] bytes in Payloads) result += bytes.Length; return result; }
        }

        public long NormalFreeBytes
        {
            get { return MobileRemoteResourceInstall.RequiredFreeBytes(PayloadBytes, ManifestBytes.Length, Payloads.Length); }
        }
    }

    private sealed class Request
    {
        public string Url, Destination;
        public long Offset;
        public Request(Uri uri, string destination, long offset)
        { Url = uri.AbsoluteUri; Destination = destination; Offset = offset; }
    }

    private sealed class MemoryTransport : IMobileResourceVerifiedTransport
    {
        public readonly Dictionary<string, byte[]> Bodies = new Dictionary<string, byte[]>();
        public readonly List<Request> Requests = new List<Request>();
        public Exception ArchiveFailure;
        public CancellationTokenSource CancelArchive;
        public int CancelAfterBytes = 777;

        public MemoryTransport(Fixture fixture)
        {
            Bodies[ManifestUrl] = fixture.ManifestBytes;
            Bodies[ArchiveUrl] = fixture.ArchiveBytes;
            for (int i = 0; i < Sources.Length; i++) Bodies[SourceUrl(i)] = fixture.Payloads[i];
        }

        public int Count(string url)
        {
            int result = 0;
            foreach (Request request in Requests) if (request.Url == url) result++;
            return result;
        }

        public Request ArchiveRequest
        {
            get { foreach (Request request in Requests) if (request.Url == ArchiveUrl) return request; return null; }
        }

        public void Download(Uri uri, string destination, long maximum, CancellationToken cancellation, Action<long> progress)
        { Transfer(uri, destination, maximum, 0, false, cancellation, progress, null); }

        public void DownloadResumable(Uri uri, string destination, long expected, long offset,
            CancellationToken cancellation, Action<long> progress)
        { Transfer(uri, destination, expected, offset, true, cancellation, progress, null); }

        public MobileResourceVerifiedFile DownloadVerifiedResumable(Uri uri, string destination, long expected,
            long offset, CancellationToken cancellation, Action<long> progress, Action<long> prefixProgress = null)
        { return Transfer(uri, destination, expected, offset, true, cancellation, progress, prefixProgress); }

        private MobileResourceVerifiedFile Transfer(Uri uri, string destination, long maximum, long offset,
            bool exact, CancellationToken cancellation, Action<long> progress, Action<long> prefixProgress)
        {
            cancellation.ThrowIfCancellationRequested();
            Requests.Add(new Request(uri, destination, offset));
            if (uri.AbsoluteUri == ArchiveUrl && ArchiveFailure != null) throw ArchiveFailure;
            byte[] body;
            if (!Bodies.TryGetValue(uri.AbsoluteUri, out body)) throw new IOException("Unexpected fixture URL: " + uri);
            Assert(body.Length <= maximum && (!exact || body.Length == maximum), "Fixture honors bounded response length");
            if (offset > 0)
            {
                Assert(File.Exists(destination) && new FileInfo(destination).Length == offset, "Resume starts at retained prefix length");
                byte[] retained = File.ReadAllBytes(destination);
                for (int i = 0; i < retained.Length; i++)
                    if (retained[i] != body[i]) throw new InvalidDataException("Fixture resume prefix changed.");
                prefixProgress?.Invoke(offset);
            }
            using (var file = new FileStream(destination, offset > 0 ? FileMode.Open : FileMode.Create,
                FileAccess.Write, FileShare.None))
            {
                file.Position = offset;
                progress?.Invoke(offset);
                int end = body.Length;
                bool cancel = uri.AbsoluteUri == ArchiveUrl && CancelArchive != null;
                if (cancel) end = (int)Math.Min(body.Length, offset + CancelAfterBytes);
                file.Write(body, (int)offset, end - (int)offset);
                file.Flush(true);
                progress?.Invoke(end);
                if (cancel)
                {
                    CancelArchive.Cancel();
                    cancellation.ThrowIfCancellationRequested();
                    throw new OperationCanceledException("Fixture cancelled archive transfer.");
                }
            }
            cancellation.ThrowIfCancellationRequested();
            return new MobileResourceVerifiedFile(destination, body.Length, Hash(body), File.GetLastWriteTimeUtc(destination).Ticks);
        }
    }

    private static MobileRemoteResourceInstall Installer(string storage, Fixture fixture, MemoryTransport transport,
        long free = long.MaxValue)
    {
        return new MobileRemoteResourceInstall(storage, "1.0.0", false, transport, fixture.Platform,
            path => free, maxConcurrentDownloads: 1);
    }

    private static string Install(string storage, Fixture fixture, MemoryTransport transport,
        CancellationToken cancellation = default(CancellationToken), long free = long.MaxValue,
        bool fullVerification = false, Action<string, string, long, long> progress = null, Action<long> networkProgress = null)
    {
        return Installer(storage, fixture, transport, free).Install(ManifestUrl, cancellation, progress,
            fixture.Plan.ResourceManifestSizeBytes, fixture.Plan.ResourceManifestSha256,
            fullVerification, networkProgress, fixture.Plan.ResourceArchive);
    }

    public static void Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "ffo-archive-install-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            foreach (string platform in new[] { "android", "windows", "macos" })
                TestFreshAndWarm(Path.Combine(root, platform), platform);
            TestMostlyStaged(Path.Combine(root, "staged"));
            TestOldGeneration(Path.Combine(root, "old-generation"));
            TestInvalidActiveMarker(Path.Combine(root, "invalid-active"));
            TestRepairSkipsArchive(Path.Combine(root, "repair"));
            TestLowCoverage(Path.Combine(root, "low-coverage"));
            TestArchiveFailures(Path.Combine(root, "fallback"));
            TestPartialExtractionFallback(Path.Combine(root, "partial-extraction"));
            TestManifestSourceAuthority(Path.Combine(root, "source-authority"));
            TestCancellationAndResume(Path.Combine(root, "cancel"));
            TestExtractionCancellation(Path.Combine(root, "extract-cancel"));
            TestStorageGate(Path.Combine(root, "space"));
            TestAbsentArchive(Path.Combine(root, "no-archive"));
            Console.WriteLine("PASS first-install archive integration contracts (" + checks +
                " checks; tiny fake transport/TAR/temp files only, no network/Unity/device validation)");
        }
        finally { Directory.Delete(root, true); }
    }

    private static void VerifyInstalled(string installed, Fixture fixture)
    {
        Assert(Directory.Exists(installed) && !installed.EndsWith(".staging"), "Only a complete installed generation is returned");
        for (int i = 0; i < Sources.Length; i++)
            Assert(Hash(File.ReadAllBytes(Path.Combine(installed, Destinations[i]))) == Hash(fixture.Payloads[i]),
                "Installed bytes match manifest destination: " + Destinations[i]);
        var receipt = new MobileResourceVerification(fixture.Manifest, installed, Path.Combine(installed, ".resource-verified"));
        Assert(receipt.CachedFiles == Sources.Length && receipt.BytesToHash == 0,
            "Archive and scatter files share a complete per-file receipt after activation");
        Assert(Directory.GetFiles(installed, "*.part", SearchOption.AllDirectories).Length == 0,
            "No archive cache or scatter partial enters the activated resource tree");
        var allowed = new HashSet<string>(Destinations, StringComparer.Ordinal);
        allowed.Add(".resource-manifest.tsv");
        allowed.Add(".resource-verified");
        string prefix = installed + Path.DirectorySeparatorChar;
        foreach (string file in Directory.GetFiles(installed, "*", SearchOption.AllDirectories))
            Assert(allowed.Contains(file.Substring(prefix.Length).Replace(Path.DirectorySeparatorChar, '/')),
                "Only current-manifest payloads and installer metadata survive activation or fallback");
    }

    private static void AssertScatter(MemoryTransport transport, params int[] wanted)
    {
        var indexes = new HashSet<int>(wanted);
        for (int i = 0; i < Sources.Length; i++)
            Assert(transport.Count(SourceUrl(i)) == (indexes.Contains(i) ? 1 : 0),
                "Scatter request set is exact for " + Sources[i]);
    }

    private static void TestFreshAndWarm(string storage, string platform)
    {
        var fixture = new Fixture(platform);
        var transport = new MemoryTransport(fixture);
        long hashCallbacks = 0, previousNetwork = 0;
        string installed = Install(storage, fixture, transport, progress: (phase, label, done, total) =>
        {
            if (phase == "Verifying downloaded resource" || phase == "Verifying extracted resource" ||
                phase == "Checking staged resources" || phase == "Checking installed resources") hashCallbacks++;
        }, networkProgress: bytes =>
        {
            Assert(bytes >= previousNetwork, "Fresh combined archive/scatter network count stays monotonic");
            previousNetwork = bytes;
        });
        VerifyInstalled(installed, fixture);
        Assert(transport.Count(ArchiveUrl) == 1 && transport.ArchiveRequest.Offset == 0, "Fresh install downloads one archive at offset zero");
        Assert(transport.Requests.Count == 3 && transport.Requests[0].Url == ManifestUrl &&
            transport.Requests[1].Url == ArchiveUrl && transport.Requests[2].Url == SourceUrl(3),
            "Archive transfer precedes only the remaining manifest scatter download");
        AssertScatter(transport, 3);
        Assert(hashCallbacks == 0, "Streaming archive extraction and scatter proofs avoid second full-payload hash passes");
        Assert(previousNetwork == fixture.ArchiveBytes.Length + fixture.Payloads[3].Length,
            "Combined network count includes archive plus scatter bytes exactly once");
        Assert(!transport.ArchiveRequest.Destination.StartsWith(installed + Path.DirectorySeparatorChar, StringComparison.Ordinal),
            "Archive is cached outside the active resource generation");

        var warm = new MemoryTransport(fixture);
        long warmHashCallbacks = 0;
        string again = Install(storage, fixture, warm, progress: (phase, label, done, total) =>
        {
            if (phase == "Checking installed resources") warmHashCallbacks++;
        });
        Assert(again == installed && warm.Requests.Count == 1 && warm.Count(ManifestUrl) == 1,
            "Warm same-generation run never requests the archive or a scatter payload");
        Assert(warmHashCallbacks == 0, "Warm run consumes extraction receipt without hashing payloads");
    }

    private static void TestMostlyStaged(string storage)
    {
        var fixture = new Fixture();
        string staging = Path.Combine(storage, fixture.Manifest.Version + ".staging");
        Directory.CreateDirectory(staging);
        var receipt = new MobileResourceVerification(fixture.Manifest, staging, Path.Combine(staging, ".resource-verified"));
        foreach (int i in Archived)
        {
            string file = Path.Combine(staging, Destinations[i]);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(file, fixture.Payloads[i]);
            Assert(receipt.Verify(i), "Staging fixture is independently verified");
        }
        receipt.CheckpointReceipt();
        var transport = new MemoryTransport(fixture);
        VerifyInstalled(Install(storage, fixture, transport), fixture);
        Assert(transport.Count(ArchiveUrl) == 0, "Mostly complete retained staging bypasses the first-install archive");
        AssertScatter(transport, 3);
    }

    private static void TestOldGeneration(string storage)
    {
        var old = new Fixture(generation: 1);
        var oldTransport = new MemoryTransport(old);
        string previous = Installer(storage, old, oldTransport).Install(ManifestUrl, CancellationToken.None, null);
        var current = new Fixture();
        var transport = new MemoryTransport(current);
        string installed = Install(storage, current, transport);
        VerifyInstalled(installed, current);
        Assert(installed != previous && Directory.Exists(previous), "Updating preserves the previous installed generation");
        Assert(transport.Count(ArchiveUrl) == 0, "Any reusable installed generation suppresses the first-install path");
        AssertScatter(transport, 0, 1, 2, 3);
    }

    private static void TestRepairSkipsArchive(string storage)
    {
        var fixture = new Fixture();
        var transport = new MemoryTransport(fixture);
        VerifyInstalled(Install(storage, fixture, transport, fullVerification: true), fixture);
        Assert(transport.Count(ArchiveUrl) == 0, "Explicit repair uses the existing per-file verification/download pipeline");
        AssertScatter(transport, 0, 1, 2, 3);
    }

    private static void TestInvalidActiveMarker(string storage)
    {
        Directory.CreateDirectory(storage);
        File.WriteAllText(Path.Combine(storage, "active-resource-set.txt"), "not-a-valid-installed-generation");
        var fixture = new Fixture();
        var transport = new MemoryTransport(fixture);
        VerifyInstalled(Install(storage, fixture, transport), fixture);
        Assert(transport.Count(ArchiveUrl) == 0, "An existing but damaged activation marker is not treated as a first install");
        AssertScatter(transport, 0, 1, 2, 3);
    }

    private static void TestLowCoverage(string storage)
    {
        var fixture = new Fixture(archived: new[] { 2, 3 });
        var transport = new MemoryTransport(fixture);
        VerifyInstalled(Install(storage, fixture, transport), fixture);
        Assert(transport.Count(ArchiveUrl) == 0, "Low missing-byte archive coverage skips archive overhead");
        AssertScatter(transport, 0, 1, 2, 3);
    }

    private static void TestArchiveFailures(string storage)
    {
        foreach (string failure in new[] { "network", "http", "hash", "tar" })
        {
            var fixture = new Fixture(
                changeArchive: failure == "tar" ? new Func<byte[], byte[]>(bytes => { bytes[0] ^= 1; return bytes; }) : null,
                archiveHash: failure == "hash" ? new string('0', 64) : null);
            var transport = new MemoryTransport(fixture);
            if (failure == "network") transport.ArchiveFailure = new IOException("Fixture disconnected archive download.");
            if (failure == "http") transport.ArchiveFailure = new WebException("Fixture archive endpoint unavailable.");
            string installed = Install(Path.Combine(storage, failure), fixture, transport);
            VerifyInstalled(installed, fixture);
            Assert(transport.Count(ArchiveUrl) == 1, failure + " failure attempted archive once");
            AssertScatter(transport, 0, 1, 2, 3);
            Assert(transport.Requests[1].Url == ArchiveUrl && transport.Requests[2].Url == SourceUrl(0),
                failure + " failure falls back to scatter before activation");
        }
    }

    private static void TestPartialExtractionFallback(string storage)
    {
        // The archive digest is valid, but its second payload does not match the
        // authoritative manifest. A proven first file is retained.
        var fixture = new Fixture(changeArchive: bytes => { bytes[512 + 32768 + 512] ^= 1; return bytes; });
        var transport = new MemoryTransport(fixture);
        VerifyInstalled(Install(storage, fixture, transport), fixture);
        Assert(transport.Count(ArchiveUrl) == 1, "Malformed inner payload still attempts archive once");
        AssertScatter(transport, 1, 2, 3);
        Assert(transport.Count(SourceUrl(0)) == 0, "Fallback retains already extracted and verified files");
    }

    private static void TestCancellationAndResume(string storage)
    {
        var fixture = new Fixture();
        var interrupted = new MemoryTransport(fixture);
        using (var cancellation = new CancellationTokenSource())
        {
            interrupted.CancelArchive = cancellation;
            Throws<OperationCanceledException>(() => Install(storage, fixture, interrupted, cancellation.Token),
                "Archive cancellation propagates instead of becoming a scatter fallback");
        }
        Assert(interrupted.Requests.Count == 2 && interrupted.Count(ArchiveUrl) == 1,
            "Cancelled archive run has only manifest and archive requests");
        AssertScatter(interrupted);
        Assert(!File.Exists(Path.Combine(storage, "active-resource-set.txt")), "Cancellation never activates incomplete resources");
        string part = interrupted.ArchiveRequest.Destination;
        Assert(File.Exists(part) && new FileInfo(part).Length == interrupted.CancelAfterBytes,
            "Cancellation retains the bounded archive prefix");
        Assert(File.Exists(part + ".resume"), "Cancelled archive retains its identity receipt");
        Assert(Path.GetDirectoryName(part) == Path.GetFullPath(storage), "Archive partial is outside manifest-controlled staging");

        var retry = new MemoryTransport(fixture);
        long lastNetwork = 0;
        string installed = Install(storage, fixture, retry,
            free: fixture.NormalFreeBytes + fixture.ArchiveBytes.Length - interrupted.CancelAfterBytes,
            networkProgress: value =>
            {
                Assert(value >= lastNetwork, "Resumed archive plus scatter byte count stays monotonic");
                lastNetwork = value;
            });
        VerifyInstalled(installed, fixture);
        Assert(retry.ArchiveRequest != null && retry.ArchiveRequest.Offset == interrupted.CancelAfterBytes,
            "Retry resumes the exact identity-bound archive prefix");
        AssertScatter(retry, 3);
        Assert(lastNetwork == fixture.ArchiveBytes.Length - interrupted.CancelAfterBytes + fixture.Payloads[3].Length,
            "Retained archive prefix is excluded from network throughput");
        Assert(!File.Exists(part) && !File.Exists(part + ".resume"), "Successful extraction removes cache and resume metadata");
    }

    private static void TestManifestSourceAuthority(string storage)
    {
        // A TAR path that resembles a destination must not bypass the mapping
        // through the manifest's source. Valid nonmatching content is ignored.
        var fixture = new Fixture(changeArchive: bytes =>
        {
            Array.Clear(bytes, 0, 100);
            WriteAscii(bytes, 0, 100, RootPrefix + Destinations[0]);
            for (int i = 148; i < 156; i++) bytes[i] = 32;
            long sum = 0;
            for (int i = 0; i < 512; i++) sum += bytes[i];
            WriteAscii(bytes, 148, 6, Convert.ToString(sum, 8).PadLeft(6, '0'));
            bytes[154] = 0; bytes[155] = 32;
            return bytes;
        });
        var transport = new MemoryTransport(fixture);
        VerifyInstalled(Install(storage, fixture, transport), fixture);
        Assert(transport.Count(ArchiveUrl) == 1, "Archive with a safe unlisted member is still a usable seed");
        AssertScatter(transport, 0, 3);
        Assert(transport.Count(SourceUrl(0)) == 1, "Only exact current-manifest source mapping authorizes extraction");
    }

    private static void TestStorageGate(string storage)
    {
        var fixture = new Fixture();
        var skip = new MemoryTransport(fixture);
        VerifyInstalled(Install(Path.Combine(storage, "scatter-only"), fixture, skip,
            free: fixture.NormalFreeBytes + fixture.ArchiveBytes.Length - 1), fixture);
        Assert(skip.Count(ArchiveUrl) == 0, "Enough normal space but insufficient extra archive cache space falls back to scatter");
        AssertScatter(skip, 0, 1, 2, 3);

        var exact = new MemoryTransport(fixture);
        VerifyInstalled(Install(Path.Combine(storage, "exact"), fixture, exact,
            free: fixture.NormalFreeBytes + fixture.ArchiveBytes.Length), fixture);
        Assert(exact.Count(ArchiveUrl) == 1, "Exact normal plus archive cache headroom permits archive install");
        AssertScatter(exact, 3);

        string shortage = Path.Combine(storage, "shortage");
        var insufficient = new MemoryTransport(fixture);
        Throws<IOException>(() => Install(shortage, fixture, insufficient, free: fixture.NormalFreeBytes - 1),
            "Insufficient normal storage fails rather than bypassing safety through archive");
        Assert(insufficient.Requests.Count == 1 && insufficient.Count(ManifestUrl) == 1,
            "Normal storage failure occurs before archive or scatter transfer");
        Assert(!File.Exists(Path.Combine(shortage, "active-resource-set.txt")), "Insufficient storage cannot activate resources");
    }

    private static void TestExtractionCancellation(string storage)
    {
        foreach (int cancelIndex in new[] { 0, 1 })
        {
            string path = Path.Combine(storage, "entry-" + cancelIndex);
            var fixture = new Fixture();
            var interrupted = new MemoryTransport(fixture);
            using (var cancellation = new CancellationTokenSource())
            {
                Throws<OperationCanceledException>(() => Install(path, fixture, interrupted, cancellation.Token,
                    progress: (phase, label, done, total) =>
                    {
                        if (phase == "Extracting first-install archive" && label == Destinations[cancelIndex] && done > 0)
                            cancellation.Cancel();
                    }), "Cancellation during extraction propagates without starting scatter workers");
            }
            Assert(interrupted.Requests.Count == 2, "Extraction cancellation performs no scatter downloads");
            AssertScatter(interrupted);
            Assert(!File.Exists(Path.Combine(path, "active-resource-set.txt")), "Cancelled extraction cannot activate a generation");
            string cache = interrupted.ArchiveRequest.Destination;
            Assert(File.Exists(cache) && new FileInfo(cache).Length == fixture.ArchiveBytes.Length &&
                File.Exists(cache + ".verified"), "Cancellation during extraction retains the fully verified archive cache");
            string staging = Path.Combine(path, fixture.Manifest.Version + ".staging");
            var receipt = new MobileResourceVerification(fixture.Manifest, staging, Path.Combine(staging, ".resource-verified"));
            Assert(receipt.CachedFiles == cancelIndex, "Extraction cancellation checkpoints only completed per-file proofs");
            Assert(!File.Exists(Path.Combine(staging, Destinations[cancelIndex] + ".part")),
                "Interrupted extracted payload cannot masquerade as an HTTP-resumable partial");

            var retry = new MemoryTransport(fixture);
            long stagedHashCallbacks = 0, networkBytes = 0;
            string installed = Install(path, fixture, retry, progress: (phase, label, done, total) =>
            {
                if (phase == "Checking staged resources" || phase == "Verifying downloaded resource") stagedHashCallbacks++;
            }, networkProgress: value => networkBytes = value);
            VerifyInstalled(installed, fixture);
            Assert(retry.Count(ArchiveUrl) == 0, "Retry never redownloads a completely verified archive cache");
            Assert(stagedHashCallbacks == 0, "Retry preserves completed extraction proofs without rehashing staged payloads");
            if (cancelIndex == 0)
            {
                AssertScatter(retry, 3);
                Assert(networkBytes == fixture.Payloads[3].Length, "Reusing complete archive cache contributes zero network bytes");
            }
            else
            {
                AssertScatter(retry, 1, 2, 3);
                Assert(networkBytes == fixture.Payloads[1].Length + fixture.Payloads[2].Length + fixture.Payloads[3].Length,
                    "Mostly staged retry counts only its remaining scatter transfers");
            }
            Assert(!File.Exists(cache) && !File.Exists(cache + ".resume") && !File.Exists(cache + ".verified"),
                "Successful retry removes complete archive cache and its private metadata");
        }
    }

    private static void TestAbsentArchive(string storage)
    {
        var fixture = new Fixture();
        var transport = new MemoryTransport(fixture);
        string installed = Installer(storage, fixture, transport).Install(ManifestUrl, CancellationToken.None, null,
            fixture.ManifestBytes.Length, Hash(fixture.ManifestBytes));
        VerifyInstalled(installed, fixture);
        Assert(transport.Count(ArchiveUrl) == 0, "Existing callers without archive metadata keep the scatter pipeline");
        AssertScatter(transport, 0, 1, 2, 3);
    }

    private static byte[] Tar(byte[][] payloads, int[] indexes)
    {
        using (var output = new MemoryStream())
        {
            foreach (int index in indexes)
            {
                byte[] header = new byte[512];
                WriteAscii(header, 0, 100, RootPrefix + Sources[index]);
                WriteOctal(header, 100, 8, 420);
                WriteOctal(header, 108, 8, 0);
                WriteOctal(header, 116, 8, 0);
                WriteOctal(header, 124, 12, payloads[index].Length);
                WriteOctal(header, 136, 12, 1);
                for (int i = 148; i < 156; i++) header[i] = 32;
                header[156] = (byte)'0';
                WriteAscii(header, 257, 6, "ustar");
                WriteAscii(header, 263, 2, "00");
                long sum = 0;
                foreach (byte value in header) sum += value;
                WriteAscii(header, 148, 6, Convert.ToString(sum, 8).PadLeft(6, '0'));
                header[154] = 0;
                header[155] = 32;
                output.Write(header, 0, header.Length);
                output.Write(payloads[index], 0, payloads[index].Length);
                int padding = (512 - payloads[index].Length % 512) % 512;
                output.Write(new byte[padding], 0, padding);
            }
            output.Write(new byte[1024], 0, 1024);
            return output.ToArray();
        }
    }

    private static void WriteAscii(byte[] header, int offset, int length, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        if (bytes.Length > length) throw new Exception("TAR fixture field overflow.");
        Buffer.BlockCopy(bytes, 0, header, offset, bytes.Length);
    }

    private static void WriteOctal(byte[] header, int offset, int length, long value)
    {
        WriteAscii(header, offset, length - 1, Convert.ToString(value, 8).PadLeft(length - 1, '0'));
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

// Deterministic managed contracts. Gates establish ordering; timeouts only stop a
// broken implementation from hanging. Fixtures use temporary files and loopback.
internal static class ParallelResourceDownloadContract
{
    private const string Url = "https://cdn.example.com/release/resources.tsv";
    private const int Timeout = 15000;
    private static int checks;

    private static void Assert(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        Interlocked.Increment(ref checks);
    }

    private static string Hash(byte[] bytes)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    private static void Wait(ManualResetEventSlim gate, CancellationToken token, string label)
    {
        if (!gate.Wait(Timeout, token)) throw new TimeoutException(label);
    }

    private static T Finish<T>(Task<T> task, string label)
    {
        if (!((IAsyncResult)task).AsyncWaitHandle.WaitOne(Timeout)) throw new TimeoutException(label);
        return task.GetAwaiter().GetResult();
    }

    private static Task<T> Start<T>(Func<T> work)
    {
        return Task.Factory.StartNew(work, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private static Exception Failure(Task<string> task)
    {
        try { Finish(task, "installer did not join workers"); }
        catch (Exception error) when (error is IOException || error is InvalidDataException || error is OperationCanceledException)
        { return error; }
        throw new Exception("Expected the install to fail or cancel.");
    }

    private sealed class Fixture
    {
        public readonly byte[][] Bodies;
        public readonly string Text;
        public readonly string Platform;
        public readonly long Total;

        public Fixture(string platform, int count, bool duplicates = false, string prefix = "item")
        {
            Platform = platform;
            Bodies = new byte[count][];
            var manifest = new StringBuilder("FFO-MOBILE-1\n@client\t").Append(platform).Append("\t1.0.0\n");
            for (int i = 0; i < count; i++)
            {
                Bodies[i] = new byte[duplicates ? 131072 : 4096 + i * 17];
                for (int n = 0; n < Bodies[i].Length; n++) Bodies[i][n] = (byte)((n * 31 + (duplicates ? 0 : i * 19)) % 251);
                Total += Bodies[i].Length;
                manifest.Append("files/").Append(i).Append(".bin\tdata/").Append(prefix).Append(i).Append(".pkg\t")
                    .Append(Bodies[i].Length).Append('\t').Append(Hash(Bodies[i])).Append('\n');
            }
            Text = manifest.ToString();
        }

        public string Destination(int index) { return MobileResourceManifest.Parse(Text).Entries[index].Destination; }
        public string Stage(string root) { return Path.Combine(root, MobileResourceManifest.Parse(Text).Version + ".staging"); }
    }

    // Implements the existing transport callback convention: initial offset (or
    // zero after a 200 fallback), then cumulative file length including the prefix.
    private sealed class ControlledTransport : IMobileResourceVerifiedTransport
    {
        public readonly Fixture Data;
        private readonly object gate = new object();
        private readonly Dictionary<int, long> offsets = new Dictionary<int, long>();
        public readonly ManualResetEventSlim FirstWave = new ManualResetEventSlim();
        public readonly ManualResetEventSlim ReleaseSlow = new ManualResetEventSlim();
        public readonly ManualResetEventSlim LaterStarted = new ManualResetEventSlim();
        public readonly ManualResetEventSlim ThreeHeld = new ManualResetEventSlim();
        public readonly ManualResetEventSlim Interrupt = new ManualResetEventSlim();
        public readonly ManualResetEventSlim CancellationObserved = new ManualResetEventSlim();
        public readonly ManualResetEventSlim ReleaseCleanup = new ManualResetEventSlim();
        public int BarrierWidth;
        public bool HoldFirst, Interrupted, FailInsteadOfCancel, IgnoreRange;
        public int Active, Peak, Started, Exited, Writes, ManifestRequests, Held;
        public long NetworkBytes;

        public ControlledTransport(Fixture data) { Data = data; }

        public Dictionary<int, long> Offsets()
        {
            lock (gate) return new Dictionary<int, long>(offsets);
        }

        public void Download(Uri uri, string path, long maximum, CancellationToken token, Action<long> progress)
        {
            if (!uri.AbsolutePath.EndsWith("resources.tsv", StringComparison.Ordinal))
                throw new Exception("Verified resource transport was unexpectedly bypassed.");
            Interlocked.Increment(ref ManifestRequests);
            byte[] bytes = Encoding.UTF8.GetBytes(Data.Text);
            using (var output = File.Create(path))
                MobileResourceHttpTransport.CopyBounded(new MemoryStream(bytes), output, maximum, token, progress);
        }

        public void DownloadResumable(Uri uri, string path, long expected, long offset, CancellationToken token, Action<long> progress)
        {
            DownloadVerifiedResumable(uri, path, expected, offset, token, progress);
        }

        public MobileResourceVerifiedFile DownloadVerifiedResumable(Uri uri, string path, long expected, long offset,
            CancellationToken token, Action<long> progress, Action<long> prefixProgress = null)
        {
            int index = int.Parse(Path.GetFileNameWithoutExtension(uri.AbsolutePath));
            lock (gate)
            {
                offsets.Add(index, offset);
                Active++; Started++; Peak = Math.Max(Peak, Active);
                if (Active == BarrierWidth) FirstWave.Set();
            }
            try
            {
                if (BarrierWidth > 0 && index < BarrierWidth) Wait(FirstWave, token, "first downloads did not overlap");
                if (index >= 4) LaterStarted.Set();
                if (HoldFirst && index == 0) Wait(ReleaseSlow, token, "slow resource was never released");

                byte[] bytes = Data.Bodies[index];
                long start = IgnoreRange ? 0 : offset;
                progress?.Invoke(start);
                if (start > 0) prefixProgress?.Invoke(start);
                using (var output = new FileStream(path, start == 0 ? FileMode.Create : FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    if (Interrupted && index != 0)
                    {
                        const int prefix = 7;
                        output.Write(bytes, (int)start, prefix);
                        output.Flush(true);
                        Interlocked.Increment(ref Writes);
                        Interlocked.Add(ref NetworkBytes, prefix);
                        progress?.Invoke(start + prefix);
                        if (Interlocked.Increment(ref Held) >= 3) ThreeHeld.Set();
                        if (index == 1 && FailInsteadOfCancel)
                        {
                            Wait(Interrupt, token, "failure was never released");
                            throw new IOException("injected primary failure");
                        }
                        if (!token.WaitHandle.WaitOne(Timeout)) throw new TimeoutException("sibling was not cancelled");
                        // Deliberately delay cancellation cleanup. Returning from
                        // Install before this gate opens would prove a missing join.
                        CancellationObserved.Set();
                        Wait(ReleaseCleanup, CancellationToken.None, "cancelled worker cleanup was not released");
                        token.ThrowIfCancellationRequested();
                    }
                    for (long written = start; written < bytes.Length;)
                    {
                        token.ThrowIfCancellationRequested();
                        int count = (int)Math.Min(997, bytes.Length - written);
                        output.Write(bytes, (int)written, count);
                        written += count;
                        Interlocked.Increment(ref Writes);
                        Interlocked.Add(ref NetworkBytes, count);
                        progress?.Invoke(written);
                    }
                    output.Flush(true);
                }
                // The fixture computes proof from its original in-memory body, not
                // by rereading the output file. Commit must consume this proof.
                return new MobileResourceVerifiedFile(path, bytes.Length, Hash(bytes), File.GetLastWriteTimeUtc(path).Ticks);
            }
            finally
            {
                lock (gate) { Active--; Exited++; }
            }
        }
    }

    private sealed class ProgressAudit
    {
        private int active;
        public int Calls, StagedHashCalls, SourceHashCalls;
        public long Network, HashBytes, PrefixBytes;
        public readonly Dictionary<string, long> Hashed = new Dictionary<string, long>();
        public readonly HashSet<string> SourceHashLabels = new HashSet<string>();

        private void Enter()
        {
            Assert(Interlocked.Increment(ref active) == 1, "progress and network callbacks must be serialized");
            Interlocked.Increment(ref Calls);
            Thread.SpinWait(15000);
        }
        private void Leave() { Interlocked.Decrement(ref active); }

        public void Report(string phase, string label, long done, long total)
        {
            Enter();
            try
            {
                Assert(done >= 0 && total >= 0 && (total == 0 || done <= total), "progress never exceeds its total: " + phase);
                // Worker progress is aggregated across files, so done can remain
                // zero even while hashing. The phase itself is the read signal.
                if (phase == "Verifying downloaded resource")
                    throw new Exception("a fresh streaming-verified file was hashed a second time");
                if (phase == "Checking staged resources") StagedHashCalls++;
                if (phase == "Verifying reused resource")
                {
                    SourceHashCalls++;
                    SourceHashLabels.Add(label);
                }
                if (phase == "Verifying resumed prefix") PrefixBytes += done;
                if ((phase == "Checking installed resources" || phase == "Checking staged resources" ||
                    phase == "Verifying reused resource") && done > 0)
                {
                    HashBytes += done;
                    Hashed[label] = done;
                }
            }
            finally { Leave(); }
        }

        public void NetworkReport(long value)
        {
            Enter();
            try
            {
                Assert(value >= Network, "network-only byte counter must be monotonic");
                Network = value;
            }
            finally { Leave(); }
        }
    }

    private static MobileRemoteResourceInstall Installer(string root, Fixture fixture, IMobileResourceTransport transport, int? width = null)
    {
        if (width.HasValue)
            return new MobileRemoteResourceInstall(root, "1.0.0", false, transport, fixture.Platform,
                path => long.MaxValue / 4, maxConcurrentDownloads: width.Value);
        // Deliberately omit the width: this is the production-default contract.
        return new MobileRemoteResourceInstall(root, "1.0.0", false, transport, fixture.Platform, path => long.MaxValue / 4);
    }

    private static string Install(string root, Fixture fixture, ControlledTransport transport, ProgressAudit progress,
        CancellationToken cancellation = default(CancellationToken), int? width = null, bool repair = false)
    {
        byte[] manifest = Encoding.UTF8.GetBytes(fixture.Text);
        return Installer(root, fixture, transport, width).Install(Url, cancellation, progress.Report,
            manifest.Length, Hash(manifest), repair, progress.NetworkReport);
    }

    private static void AssertComplete(string active, Fixture fixture)
    {
        var manifest = MobileResourceManifest.Parse(fixture.Text);
        Assert(Directory.Exists(active) && !active.EndsWith(".staging"), "only a completed generation activates");
        for (int i = 0; i < fixture.Bodies.Length; i++)
            Assert(MobileResourceManifest.HashFile(Path.Combine(active, manifest.Entries[i].Destination)) == Hash(fixture.Bodies[i]),
                "installed bytes match their first-download SHA-256");
        Assert(Directory.GetFiles(active, "*.part", SearchOption.AllDirectories).Length == 0, "no partial file enters activated generation");
        var receipt = new MobileResourceVerification(manifest, active, Path.Combine(active, ".resource-verified"));
        Assert(receipt.CachedFiles == fixture.Bodies.Length && receipt.BytesToHash == 0, "all concurrent commits appear in the durable receipt");
    }

    private static void TestDefaultConcurrency(string root, string platform)
    {
        var fixture = new Fixture(platform, 12);
        var transport = new ControlledTransport(fixture) { BarrierWidth = 4, HoldFirst = true };
        var progress = new ProgressAudit();
        using (var cancellation = new CancellationTokenSource())
        {
            Task<string> install = Start(() => Install(root, fixture, transport, progress, cancellation.Token));
            try
            {
                Wait(transport.FirstWave, cancellation.Token, "default install did not reach four downloads");
                Wait(transport.LaterStarted, cancellation.Token, "slow first file blocked later queued files");
                Assert(!transport.ReleaseSlow.IsSet, "queued file starts while first file is still blocked");
                transport.ReleaseSlow.Set();
                string active = Finish(install, "default parallel install did not complete");
                Assert(transport.Peak == 4, "default transfer overlap peaks at exactly four: " + platform);
                Assert(transport.Active == 0 && transport.Started == transport.Exited, "success joins every worker");
                Assert(progress.Network == fixture.Total && transport.NetworkBytes == fixture.Total,
                    "network-only progress excludes the manifest and counts each payload byte exactly once");
                Assert(progress.StagedHashCalls == 0 && progress.SourceHashCalls == 0 && progress.Calls > 0,
                    "fresh proofs bypass second-pass file hashing");
                AssertComplete(active, fixture);
            }
            finally
            {
                cancellation.Cancel(); transport.ReleaseSlow.Set();
                ((IAsyncResult)install).AsyncWaitHandle.WaitOne(Timeout);
            }
        }
    }

    private static void TestConfiguredWidths(string root)
    {
        var fixture = new Fixture("android", 6);
        foreach (int invalid in new[] { 0, 5 })
        {
            bool rejected = false;
            try { Installer(root, fixture, new ControlledTransport(fixture), invalid); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Assert(rejected, "parallelism outside 1..4 is rejected");
        }
        foreach (int width in new[] { 1, 2, 3, 4 })
        {
            var transport = new ControlledTransport(fixture) { BarrierWidth = width };
            string active = Install(Path.Combine(root, width.ToString()), fixture, transport, new ProgressAudit(), width: width);
            Assert(transport.Peak == width, "configured width is a real transfer bound: " + width);
            AssertComplete(active, fixture);
        }
    }

    private static void TestInterrupted(string root, string platform, bool fail)
    {
        // Keep a different, known-good active generation. The interrupted update
        // must never publish its marker or mutate the prior payload.
        var old = new Fixture(platform, 1, duplicates: true, prefix: "old");
        string previous = Install(root, old, new ControlledTransport(old), new ProgressAudit());
        string marker = File.ReadAllText(Path.Combine(root, "active-resource-set.txt"));
        var fixture = new Fixture(platform, 9);
        // Different content prevents entry zero from being silently reused.
        var transport = new ControlledTransport(fixture)
        { BarrierWidth = 4, Interrupted = true, FailInsteadOfCancel = fail };
        var progress = new ProgressAudit();
        using (var cancellation = new CancellationTokenSource())
        {
            Task<string> install = Start(() => Install(root, fixture, transport, progress, cancellation.Token));
            try
            {
                Wait(transport.ThreeHeld, CancellationToken.None, "three interrupted peers did not write their prefixes");
                string completed = Path.Combine(fixture.Stage(root), fixture.Destination(0));
                Assert(SpinWait.SpinUntil(() => File.Exists(completed), Timeout), "one resource commits before interruption");
                if (fail) transport.Interrupt.Set(); else cancellation.Cancel();
                Wait(transport.CancellationObserved, CancellationToken.None, "peer did not observe cancellation");
                Assert(!install.IsCompleted, "Install must wait while a cancelled worker is still cleaning up");
                transport.ReleaseCleanup.Set();
                Exception error = Failure(install);
                Assert(fail ? error.Message == "injected primary failure" : error is OperationCanceledException,
                    "original transfer failure or caller cancellation is preserved");
                Assert(transport.Active == 0 && transport.Started == transport.Exited, "failure returns only after every transfer has exited");
                Assert(File.ReadAllText(Path.Combine(root, "active-resource-set.txt")) == marker && Directory.Exists(previous),
                    "failed parallel update leaves the prior active generation untouched");
                string staging = fixture.Stage(root);
                var cached = new MobileResourceVerification(MobileResourceManifest.Parse(fixture.Text), staging,
                    Path.Combine(staging, ".resource-verified"));
                Assert(!cached.NeedsHash(0) && cached.CachedFiles == 1, "successful concurrent prefix survives in the receipt");
                long retained = fixture.Bodies[0].Length;
                foreach (var item in transport.Offsets())
                {
                    if (item.Key == 0) continue;
                    string part = Path.Combine(staging, fixture.Destination(item.Key)) + ".part";
                    Assert(File.Exists(part) && new FileInfo(part).Length == 7 && File.Exists(part + ".resume"),
                        "each unfinished transfer preserves its identity-bound partial");
                    retained += 7;
                }
                Assert(progress.Network == transport.NetworkBytes, "failure byte count excludes existing resources and local hashes");
                var retry = new ControlledTransport(fixture) { IgnoreRange = !fail };
                var retried = new ProgressAudit();
                string active = Install(root, fixture, retry, retried);
                Assert(!retry.Offsets().ContainsKey(0), "retry never downloads the completed staged file");
                foreach (var item in transport.Offsets())
                    if (item.Key != 0) Assert(retry.Offsets()[item.Key] == 7, "retry presents the retained prefix as its resume offset");
                long expected = fail ? fixture.Total - retained : fixture.Total - fixture.Bodies[0].Length;
                Assert(retried.Network == expected && retry.NetworkBytes == expected,
                    "resume counts only suffix bytes; ignored Range counts the replacement bytes");
                Assert(retried.StagedHashCalls == 0 && retried.SourceHashCalls == 0,
                    "retry trusts completed receipt and streaming proof without rereading payloads");
                AssertComplete(active, fixture);
            }
            finally
            {
                cancellation.Cancel(); transport.Interrupt.Set(); transport.ReleaseCleanup.Set();
                ((IAsyncResult)install).AsyncWaitHandle.WaitOne(Timeout);
            }
        }
    }

    private static void TestMetadataReuse(string root, string platform)
    {
        var fixture = new Fixture(platform, 40);
        string active = Install(root, fixture, new ControlledTransport(fixture), new ProgressAudit());
        string receipt = Path.Combine(active, ".resource-verified");
        DateTime stamp = File.GetLastWriteTimeUtc(receipt);
        var warm = new ControlledTransport(fixture); var progress = new ProgressAudit();
        Assert(Install(root, fixture, warm, progress) == active && warm.Started == 0 && warm.ManifestRequests == 1,
            "warm install uses manifest plus mtime/size receipt, not payload downloads");
        Assert(progress.Network == 0 && progress.HashBytes == 0 && File.GetLastWriteTimeUtc(receipt) == stamp,
            "warm metadata-only start performs no payload hashing or receipt rewrite");
        string changed = Path.Combine(active, fixture.Destination(3));
        File.SetLastWriteTimeUtc(changed, File.GetLastWriteTimeUtc(changed).AddSeconds(17));
        var changedProgress = new ProgressAudit();
        Installer(root, fixture, warm).UseInstalled(CancellationToken.None, changedProgress.Report);
        Assert(changedProgress.Hashed.Count == 1 && changedProgress.Hashed.ContainsKey(fixture.Destination(3)),
            "mtime change hashes exactly the affected file");
        using (var output = new FileStream(changed, FileMode.Append)) output.WriteByte(99);
        bool rejected = false;
        try { Installer(root, fixture, warm).UseInstalled(CancellationToken.None, null); }
        catch (InvalidDataException) { rejected = true; }
        Assert(rejected, "size change cannot pass metadata reuse");
        var repair = new ControlledTransport(fixture); var repairedProgress = new ProgressAudit();
        string repaired = Install(root, fixture, repair, repairedProgress);
        Assert(repair.Started == 1 && repair.Offsets().ContainsKey(3), "ordinary changed-file repair downloads only the invalid payload");
        Assert(repairedProgress.Network == fixture.Bodies[3].Length, "local reuse and copy do not inflate network bytes");
        AssertComplete(repaired, fixture);
    }

    private static void TestDuplicateReuse(string root, string platform)
    {
        var seed = new Fixture(platform, 1, true, "seed");
        Install(root, seed, new ControlledTransport(seed), new ProgressAudit());
        foreach (bool repair in new[] { false, true })
        {
            // >32 copies crosses the automatic receipt checkpoint boundary while
            // four workers reference the same old source verification index.
            var fixture = new Fixture(platform, 48, true, repair ? "repair-copy" : "copy");
            var transport = new ControlledTransport(fixture); var progress = new ProgressAudit();
            string active = Install(root, fixture, transport, progress, repair: repair);
            Assert(transport.Started == 0 && progress.Network == 0, "duplicate content reuses one old source safely without network");
            Assert(repair ? progress.SourceHashCalls > 0 && progress.SourceHashLabels.Count == 1 : progress.SourceHashCalls == 0,
                "ordinary reuse skips source hashing; explicit repair hashes the one shared source exactly once");
            AssertComplete(active, fixture);
        }
    }

    private static void TestLoopbackConnections(string root)
    {
        Directory.CreateDirectory(root);
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = new Uri("http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/file");
        var point = ServicePointManager.FindServicePoint(endpoint);
        int originalLimit = point.ConnectionLimit;
        point.ConnectionLimit = 2; // The transport must not silently retain this historical bottleneck.
        var allConnected = new ManualResetEventSlim();
        var release = new ManualResetEventSlim();
        var cancellation = new CancellationTokenSource();
        var peers = new HashSet<string>();
        var headers = new List<string>();
        var handlers = new List<Task>();
        int concurrent = 0, peak = 0;
        Task server = Task.Factory.StartNew(() =>
        {
            for (int i = 0; i < 4; i++)
            {
                TcpClient client = listener.AcceptTcpClient();
                handlers.Add(Task.Factory.StartNew(() =>
                {
                    using (client)
                    using (var stream = client.GetStream())
                    {
                        stream.ReadTimeout = Timeout; stream.WriteTimeout = Timeout;
                        var request = new StringBuilder();
                        using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
                        {
                            string line;
                            while (!string.IsNullOrEmpty(line = reader.ReadLine())) request.AppendLine(line);
                        }
                        lock (peers)
                        {
                            peers.Add(client.Client.RemoteEndPoint.ToString()); headers.Add(request.ToString());
                            concurrent++; peak = Math.Max(peak, concurrent);
                            if (concurrent == 4) allConnected.Set();
                        }
                        Wait(release, CancellationToken.None, "loopback release missing");
                        byte[] response = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 4\r\nConnection: close\r\n\r\ndata");
                        stream.Write(response, 0, response.Length); stream.Flush();
                        lock (peers) concurrent--;
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default));
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        var clients = new Task<MobileResourceVerifiedFile>[4];
        for (int i = 0; i < clients.Length; i++)
        {
            int index = i;
            clients[i] = Start(() => new MobileResourceHttpTransport().DownloadVerifiedResumable(
                new Uri(endpoint.AbsoluteUri + index), Path.Combine(root, index + ".part"), 4, 0, cancellation.Token, null));
        }
        try
        {
            Wait(allConnected, CancellationToken.None, "HTTP pool serialized four workers behind two connections");
            Assert(peak == 4 && peers.Count == 4, "real HTTP opens four simultaneous same-origin connections");
            release.Set();
            foreach (var client in clients) Assert(Finish(client, "loopback transfer did not finish") != null, "real transfer returns streaming proof");
            Assert(server.Wait(Timeout) && Task.WaitAll(handlers.ToArray(), Timeout), "all loopback server handlers joined");
            foreach (string header in headers)
                Assert(header.IndexOf("Authorization:", StringComparison.OrdinalIgnoreCase) < 0 &&
                    header.IndexOf("Cookie:", StringComparison.OrdinalIgnoreCase) < 0,
                    "parallel HTTP does not introduce authentication or cookie sharing");
        }
        finally
        {
            cancellation.Cancel(); release.Set(); listener.Stop();
            foreach (var client in clients) ((IAsyncResult)client).AsyncWaitHandle.WaitOne(Timeout);
            ((IAsyncResult)server).AsyncWaitHandle.WaitOne(Timeout);
            foreach (var handler in handlers) ((IAsyncResult)handler).AsyncWaitHandle.WaitOne(Timeout);
            point.ConnectionLimit = originalLimit;
            cancellation.Dispose(); allConnected.Dispose(); release.Dispose();
        }
    }

    public static void Main()
    {
        ThreadPool.SetMinThreads(16, 16);
        string root = Path.Combine(Path.GetTempPath(), "ffo-parallel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            TestConfiguredWidths(Path.Combine(root, "limits"));
            foreach (string platform in new[] { "windows", "macos", "android" })
            {
                string current = Path.Combine(root, platform);
                TestDefaultConcurrency(Path.Combine(current, "default"), platform);
                TestInterrupted(Path.Combine(current, "failure"), platform, true);
                TestInterrupted(Path.Combine(current, "cancel"), platform, false);
                TestMetadataReuse(Path.Combine(current, "metadata"), platform);
                TestDuplicateReuse(Path.Combine(current, "duplicates"), platform);
                Console.WriteLine("PASS parallel resource platform contracts: " + platform);
            }
            TestLoopbackConnections(Path.Combine(root, "http"));
            Console.WriteLine("PASS parallel resource download contracts (" + checks +
                " checks; default peak=4; Windows/macOS/Android manifest parameters; simulated files and real loopback HTTP, not Unity/device validation)");
        }
        finally { Directory.Delete(root, true); }
    }
}

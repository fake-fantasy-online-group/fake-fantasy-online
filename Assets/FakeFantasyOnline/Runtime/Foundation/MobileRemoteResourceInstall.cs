using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using System.Net;
using System.Security.Cryptography;

namespace FakeFantasyOnline.Foundation
{
/// <summary>Remote data-only install. Content-addressed staging is never a loader root.</summary>
public sealed class MobileRemoteResourceInstall
{
    public const int DefaultConcurrentDownloads = MobileResourceHttpTransport.ConcurrentConnectionLimit;
    public const long MaxTotalBytes = 64L * 1024 * 1024 * 1024;
    private const string StoredManifest = ".resource-manifest.tsv", Receipt = ".resource-verified";
    private const string ActiveMarker = "active-resource-set.txt";
    private readonly string storage, appVersion, platform;
    private readonly int maxConcurrentDownloads;
    private readonly Func<string, long> availableBytes;
    private readonly bool developmentHttp;
    private readonly IMobileResourceTransport transport;
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public MobileRemoteResourceInstall(string storage, string appVersion, bool developmentHttp, IMobileResourceTransport transport,
        string platform = "android", Func<string, long> availableBytes = null, int maxConcurrentDownloads = DefaultConcurrentDownloads)
    {
        if (maxConcurrentDownloads < 1 || maxConcurrentDownloads > DefaultConcurrentDownloads)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrentDownloads));
        this.maxConcurrentDownloads = maxConcurrentDownloads;
        this.storage = Path.GetFullPath(storage);
        this.appVersion = appVersion;
        ValidatePlatform(platform);
        this.platform = platform;
        this.availableBytes = availableBytes ?? (path => new DriveInfo(path).AvailableFreeSpace);
        this.developmentHttp = developmentHttp;
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public string Install(string manifestUrl, CancellationToken cancellation, Action<string, string, long, long> progress,
        long manifestBytes = 0, string manifestSha256 = null, bool fullVerification = false, Action<long> networkProgress = null, MobileResourceArchive archive = null)
    {
        Uri uri = MobileResourceBootstrapConfig.ValidateEndpoint(manifestUrl, developmentHttp);
        if ((manifestBytes == 0) != string.IsNullOrEmpty(manifestSha256) || manifestBytes < 0 ||
            manifestBytes > MobileResourceManifest.MaxManifestBytes ||
            (!string.IsNullOrEmpty(manifestSha256) && (manifestSha256.Length != 64 || !IsHex(manifestSha256))))
            throw new InvalidDataException("Resource manifest size and SHA-256 must form a valid paired identity.");
        cancellation.ThrowIfCancellationRequested();
        RejectPathLinks(storage);
        Directory.CreateDirectory(storage);
        string lockPath = Path.Combine(storage, ".install.lock");
        RejectLink(lockPath);
        using (var installLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            return InstallLocked(uri, cancellation, progress, manifestBytes, manifestSha256, fullVerification, networkProgress, archive);
    }

    private string InstallLocked(Uri uri, CancellationToken cancellation, Action<string, string, long, long> progress,
        long manifestBytes, string manifestSha256, bool fullVerification, Action<long> networkProgress, MobileResourceArchive archive)
    {
        string temporaryManifest = Path.Combine(storage, "manifest-" + Guid.NewGuid().ToString("N") + ".part");
        string text;
        try
        {
            Report(progress, "Downloading resource manifest", "Reading the configured resource endpoint...", 0, manifestBytes);
            transport.Download(uri, temporaryManifest, manifestBytes > 0 ? manifestBytes : MobileResourceManifest.MaxManifestBytes,
                cancellation, bytes => Report(progress, "Downloading resource manifest", "Checking the resource manifest identity...", bytes, manifestBytes));
            if (manifestBytes > 0 && (new FileInfo(temporaryManifest).Length != manifestBytes ||
                MobileResourceManifest.HashFile(temporaryManifest, bytes => cancellation.ThrowIfCancellationRequested()) != manifestSha256))
                throw new InvalidDataException("Resource manifest does not match the dispatch size or SHA-256.");
            text = ReadManifest(temporaryManifest);
        }
        finally { if (File.Exists(temporaryManifest)) File.Delete(temporaryManifest); }
        var manifest = MobileResourceManifest.Parse(text);
        ValidateManifest(manifest, appVersion, platform);
        cancellation.ThrowIfCancellationRequested();
        // Normal starts use the private metadata receipt; explicit repair bypasses it.
        // Failure never edits the active payload. Repair builds a separate generation.
        string activeRoot = null;
        try
        {
            string candidate = ReadActiveRoot();
            if (Path.GetFileName(candidate).StartsWith(manifest.Version + "-", StringComparison.Ordinal))
                activeRoot = UseInstalled(cancellation, progress, fullVerification);
        }
        catch (InvalidDataException) { }
        catch (IOException) { }
        if (activeRoot != null) return activeRoot;
        string staging = Path.Combine(storage, manifest.Version + ".staging");
        RejectLink(staging);
        Directory.CreateDirectory(staging);
        ValidateTree(staging, manifest, true);
        var verification = new MobileResourceVerification(manifest, staging, Path.Combine(staging, Receipt), !fullVerification);
        try
        {
        var reusable = FindReusableFiles(!fullVerification);
        long total = 0, additional = 0;
        foreach (var entry in manifest.Entries) total = checked(total + entry.Length);
        for (int i = 0; i < manifest.Entries.Count; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            var entry = manifest.Entries[i];
            // Hash retained completed staging files once before estimating extra space.
            // Otherwise retry could wrongly require room for bytes already on disk.
            SafePath(staging, entry.Destination);
            if (verification.Verify(i, bytes => { cancellation.ThrowIfCancellationRequested(); Report(progress,
                "Checking staged resources", entry.Destination, bytes, entry.Length); })) continue;
            string part = SafePath(staging, entry.Destination) + ".part";
            long retained = PartialLength(part, ResumeIdentity(ResourceUri(uri, entry.Source), entry));
            if (retained > entry.Length) retained = 0;
            additional = checked(additional + entry.Length - retained);
        }
        long required = RequiredFreeBytes(additional, StrictUtf8.GetByteCount(text), manifest.Entries.Count);
        long available = availableBytes(storage);
        if (available < required)
            throw new IOException("Insufficient free storage: need " + required.ToString(CultureInfo.InvariantCulture) +
                " additional bytes while retaining installed resources; available " + available.ToString(CultureInfo.InvariantCulture) + ".");
        Report(progress, "Preparing resource update", "Storage checked; existing resource sets are retained.", 0, total);
        // An archive is an optional first-install seed, never the authoritative resource set.
        // Repairs and existing generations retain the per-file path. The missing-byte
        // efficiency gate applies only to a NEW transfer, never to a complete local cache.
        long archiveNetworkBytes = 0;
        if (!fullVerification && !File.Exists(Path.Combine(storage, ActiveMarker)) &&
            reusable.Count == 0)
            archiveNetworkBytes = TryArchive(archive, manifest, staging, verification, total, required, available,
                additional >= total - total / 5, cancellation, progress, networkProgress);
        var aggregate = new InstallProgress(manifest, total, progress,
            networkProgress == null ? (Action<long>)null : bytes => networkProgress(archiveNetworkBytes + bytes));
        RunWorkers(manifest.Entries.Count, cancellation, (i, token) =>
            InstallEntry(uri, staging, manifest, verification, reusable, i, token, aggregate));
        cancellation.ThrowIfCancellationRequested();
        Report(progress, "Finishing resource check", "Activating only the complete verified resource set...", 0, 0);
        ValidateTree(staging, manifest, false);
        verification.CommitReceipt();
        WriteDurable(Path.Combine(staging, StoredManifest), text);
        cancellation.ThrowIfCancellationRequested();
        string generation = manifest.Version + "-" + Guid.NewGuid().ToString("N");
        string installed = Path.Combine(storage, generation);
        Directory.Move(staging, installed);
        // Same-filesystem atomic marker replacement preserves the old marker on failure.
        CommitMarker(generation, cancellation);
        CleanupArchive(archive);
        return installed;
        }
        finally { verification.CheckpointReceipt(); }
    }

    private long TryArchive(MobileResourceArchive archive, MobileResourceManifest manifest, string staging,
        MobileResourceVerification verification, long total, long required, long available,
        bool allowNewTransfer, CancellationToken cancellation, Action<string, string, long, long> progress, Action<long> networkProgress)
    {
        if (archive == null || total <= 0 || archive.SizeBytes < total - total / 5 ||
            archive.SizeBytes > checked(total + Math.Max(total / 5, 1024 * 1024))) return 0;
        // Size is only an efficiency gate. Every extracted file still needs the authoritative
        // manifest's length/hash. Unknown archive files are never installed.
        string cache = ArchiveCache(archive), verifiedCache = cache + ".verified";
        long networkBytes = 0, lastNetwork = -1;
        bool extractionStarted = false;
        try
        {
            Uri source = MobileResourceBootstrapConfig.ValidateEndpoint(archive.Url, developmentHttp);
            string identity = "FFO-RESOURCE-ARCHIVE-1\n" + source.AbsoluteUri + "\n" +
                archive.SizeBytes.ToString(CultureInfo.InvariantCulture) + ":" + archive.Sha256 + "\n";
            RejectLink(cache); RejectLink(verifiedCache); RejectLink(verifiedCache + ".part");
            long retained = PartialLength(cache, identity);
            if (retained > archive.SizeBytes) retained = 0;
            if (!allowNewTransfer && retained != archive.SizeBytes) return 0;
            if (available < checked(required + archive.SizeBytes - retained)) return 0;
            long offset = PreparePartial(cache, identity, archive.SizeBytes);
            MobileResourceVerifiedFile proof = null;
            if (offset < archive.SizeBytes)
            {
                Action<long> transfer = bytes =>
                {
                    if (lastNetwork >= 0 && bytes > lastNetwork) networkBytes += bytes - lastNetwork;
                    lastNetwork = bytes;
                    networkProgress?.Invoke(networkBytes);
                    Report(progress, "Downloading first-install archive", "Preparing the initial resource set...", bytes, archive.SizeBytes);
                };
                var streaming = transport as IMobileResourceVerifiedTransport;
                var resumable = transport as IMobileResourceResumableTransport;
                if (streaming != null)
                    proof = streaming.DownloadVerifiedResumable(source, cache, archive.SizeBytes, offset, cancellation, transfer,
                        bytes => cancellation.ThrowIfCancellationRequested());
                else if (resumable != null)
                    resumable.DownloadResumable(source, cache, archive.SizeBytes, offset, cancellation, transfer);
                else transport.Download(source, cache, archive.SizeBytes, cancellation, transfer);
            }
            cancellation.ThrowIfCancellationRequested();
            long stamp = File.GetLastWriteTimeUtc(cache).Ticks;
            string cacheIdentity = identity + stamp.ToString(CultureInfo.InvariantCulture) + "\n";
            bool cached = offset == archive.SizeBytes && File.Exists(verifiedCache) &&
                new FileInfo(verifiedCache).Length == StrictUtf8.GetByteCount(cacheIdentity) &&
                File.ReadAllText(verifiedCache, StrictUtf8) == cacheIdentity;
            if (new FileInfo(cache).Length != archive.SizeBytes ||
                (!cached && (proof == null ? MobileResourceManifest.HashFile(cache, bytes => {
                    cancellation.ThrowIfCancellationRequested();
                    Report(progress, "Checking first-install archive", "Verifying the retained archive...", bytes, archive.SizeBytes);
                }) != archive.Sha256 :
                !proof.Matches(cache, archive.SizeBytes, archive.Sha256, stamp))) || stamp != File.GetLastWriteTimeUtc(cache).Ticks)
                throw new InvalidDataException("Initial resource archive length/hash mismatch.");
            if (!cached) WriteDurable(verifiedCache, cacheIdentity);
            var sources = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                // Ambiguous sources cannot be mapped safely by this optimization.
                if (sources.ContainsKey(manifest.Entries[i].Source)) return networkBytes;
                sources.Add(manifest.Entries[i].Source, i);
            }
            extractionStarted = true;
            string extractionPhase = cached ? "Resuming first-install archive" : "Extracting first-install archive";
            long extracted = 0;
            Report(progress, extractionPhase, "Using the downloaded archive; no archive download is needed.", 0, total);
            using (var input = new FileStream(cache, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (input.Length != archive.SizeBytes || stamp != File.GetLastWriteTimeUtc(cache).Ticks)
                    throw new IOException("Initial resource archive changed before extraction.");
                MobileResourceTarReader.Read(input, archive.SizeBytes, cancellation, (name, length, payload) =>
                {
                    if (!name.StartsWith(archive.RootPrefix, StringComparison.Ordinal)) return;
                    int index;
                    if (!sources.TryGetValue(name.Substring(archive.RootPrefix.Length), out index)) return;
                    var entry = manifest.Entries[index];
                    if (length != entry.Length) throw new InvalidDataException("Archive resource size mismatch: " + entry.Destination);
                    if (verification.Verify(index, bytes => cancellation.ThrowIfCancellationRequested()))
                    {
                        extracted += length;
                        Report(progress, extractionPhase, entry.Destination, extracted, total);
                        return;
                    }
                    string target = SafePath(staging, entry.Destination), part = target + ".part";
                    RejectLink(part); Directory.CreateDirectory(Path.GetDirectoryName(target));
                    // Archive-derived partials must never be resumed as an HTTP per-file prefix.
                    DeletePartial(part);
                    try
                    {
                        string hash;
                        using (var output = new FileStream(part, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        using (var digest = SHA256.Create())
                        {
                            var buffer = new byte[65536]; long written = 0; int count;
                            while ((count = payload.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                cancellation.ThrowIfCancellationRequested();
                                if (count > entry.Length - written) throw new InvalidDataException("Archive resource exceeds manifest size.");
                                output.Write(buffer, 0, count); digest.TransformBlock(buffer, 0, count, buffer, 0); written += count;
                                Report(progress, extractionPhase, entry.Destination, extracted + written, total);
                            }
                            if (written != entry.Length) throw new InvalidDataException("Archive resource is truncated.");
                            digest.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                            hash = BitConverter.ToString(digest.Hash).Replace("-", "").ToLowerInvariant();
                            output.Flush(true);
                        }
                        cancellation.ThrowIfCancellationRequested();
                        var fileProof = new MobileResourceVerifiedFile(part, length, hash, File.GetLastWriteTimeUtc(part).Ticks);
                        verification.CommitFile(index, part, fileProof);
                        extracted += length;
                    }
                    finally { if (File.Exists(part)) File.Delete(part); }
                });
            }
            if (stamp != File.GetLastWriteTimeUtc(cache).Ticks) throw new IOException("Initial resource archive changed while extracting.");
            CleanupArchive(archive);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException || error is InvalidDataException || error is WebException || error is UnauthorizedAccessException || error is DecoderFallbackException)
        {
            cancellation.ThrowIfCancellationRequested();
            // Invalid completed archives are discarded; interrupted transfers retain their
            // identity-bound bytes until the normal pipeline succeeds or the user retries.
            if (error is InvalidDataException || error is DecoderFallbackException) CleanupArchive(archive);
            else if (extractionStarted) throw; // Output/storage failures do not invalidate a verified archive or justify network fallback.
            Report(progress, "Downloading resources", "Archive unavailable; continuing with individual verified files.", 0, total);
        }
        finally { verification.CheckpointReceipt(); }
        return networkBytes;
    }

    private string ArchiveCache(MobileResourceArchive archive)
    { return Path.Combine(storage, "archive-" + archive.Sha256 + ".part"); }

    private void CleanupArchive(MobileResourceArchive archive)
    {
        if (archive == null) return;
        try
        {
            string cache = ArchiveCache(archive);
            DeletePartial(cache);
            foreach (string path in new[] { cache + ".verified", cache + ".verified.part" })
            { RejectLink(path); if (File.Exists(path)) File.Delete(path); }
        }
        catch (IOException) { } // Cache cleanup must never invalidate a committed installation.
        catch (InvalidDataException) { }
        catch (UnauthorizedAccessException) { }
    }

    // A fixed number of workers drain the existing per-file pipeline. There is no
    // task/thread per file; failure cancels siblings and all writers join before
    // final receipt checkpointing, activation or returning control to the UI.
    private void RunWorkers(int count, CancellationToken cancellation, Action<int, CancellationToken> work)
    {
        if (count == 0) return;
        using (var group = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
        {
            int next = -1;
            ExceptionDispatchInfo failure = null;
            var failureLock = new object();
            var workers = new List<Task>();
            Action cancelWorkers = () =>
            {
                try { group.Cancel(); }
                catch (Exception error)
                {
                    lock (failureLock)
                        if (failure == null) failure = ExceptionDispatchInfo.Capture(error);
                }
            };
            try
            {
                for (int worker = 0; worker < Math.Min(maxConcurrentDownloads, count); worker++)
                    workers.Add(Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            while (true)
                            {
                                group.Token.ThrowIfCancellationRequested();
                                int index = Interlocked.Increment(ref next);
                                if (index >= count) return;
                                work(index, group.Token);
                            }
                        }
                        catch (Exception error)
                        {
                            lock (failureLock)
                                if (failure == null) failure = ExceptionDispatchInfo.Capture(error);
                            cancelWorkers();
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default));
                Task.WaitAll(workers.ToArray());
                cancellation.ThrowIfCancellationRequested();
                if (failure != null) failure.Throw();
            }
            finally
            {
                cancelWorkers();
                Task.WaitAll(workers.ToArray());
            }
        }
    }

    private void InstallEntry(Uri uri, string staging, MobileResourceManifest manifest,
        MobileResourceVerification verification, Dictionary<string, ReusableResource> reusable, int i,
        CancellationToken cancellation, InstallProgress progress)
    {
        cancellation.ThrowIfCancellationRequested();
        var entry = manifest.Entries[i];
        string target = SafePath(staging, entry.Destination), part = target + ".part";
        RejectLink(part); RejectLink(part + ".resume"); RejectLink(part + ".resume.part");
        // Preflight already verified retained staging bytes. This call is metadata
        // only unless the file changed in between, and does not flash a hash phase.
        if (!verification.Verify(i, bytes => { cancellation.ThrowIfCancellationRequested();
            progress.Local(i, "Checking staged resources", bytes); }))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            Uri source = ResourceUri(uri, entry.Source);
            long offset = PreparePartial(part, ResumeIdentity(source, entry), entry.Length);
            bool ready = false;
            if (offset == entry.Length && File.Exists(part))
            {
                try { verification.CommitFile(i, part, bytes => { cancellation.ThrowIfCancellationRequested();
                    progress.Local(i, "Verifying retained download", bytes); }); ready = true; }
                catch (InvalidDataException) { DeletePartial(part); offset = PreparePartial(part, ResumeIdentity(source, entry), entry.Length); }
            }
            ReusableResource reusableFile;
            if (!ready && offset == 0 && reusable.TryGetValue(ContentKey(entry), out reusableFile))
                ready = TryReuse(verification, i, reusableFile, part, entry, cancellation,
                    (phase, label, done, total) => progress.Local(i, phase, done), entry.Destination);
            if (!ready)
            {
                MobileResourceVerifiedFile proof = null;
                var streaming = transport as IMobileResourceVerifiedTransport;
                var resumable = transport as IMobileResourceResumableTransport;
                progress.StartDownload(i);
                if (streaming != null)
                    proof = streaming.DownloadVerifiedResumable(source, part, entry.Length, offset, cancellation,
                        bytes => progress.Download(i, bytes), bytes => cancellation.ThrowIfCancellationRequested());
                else if (resumable != null)
                    resumable.DownloadResumable(source, part, entry.Length, offset, cancellation,
                        bytes => progress.Download(i, bytes));
                else
                    transport.Download(source, part, entry.Length, cancellation, bytes => progress.Download(i, bytes));
                cancellation.ThrowIfCancellationRequested();
                progress.EndDownload(i);
                try
                {
                    verification.CommitFile(i, part, proof, bytes => { cancellation.ThrowIfCancellationRequested();
                        progress.Local(i, "Verifying downloaded resource", bytes); });
                }
                catch (InvalidDataException) { DeletePartial(part); throw; }
                // Interrupted responses retain identity-bound partials. Only a
                // completed payload which fails its full digest is discarded.
            }
        }
        DeletePartial(part);
        progress.Complete(i);
    }

    // Progress callbacks are serialized. Network bytes have a separate counter:
    // retained prefixes, local copies and hashes must never inflate the speed.
    private sealed class InstallProgress
    {
        private readonly object gate = new object();
        private readonly MobileResourceManifest manifest;
        private readonly Action<string, string, long, long> report;
        private readonly Action<long> networkReport;
        private readonly long total;
        private readonly long[] bytes, lastNetwork;
        private readonly bool[] downloading;
        private long done, networkBytes;
        private int activeDownloads, complete;

        public InstallProgress(MobileResourceManifest manifest, long total,
            Action<string, string, long, long> report, Action<long> networkReport)
        {
            this.manifest = manifest; this.total = total; this.report = report; this.networkReport = networkReport;
            bytes = new long[manifest.Entries.Count]; lastNetwork = new long[bytes.Length]; downloading = new bool[bytes.Length];
            networkReport?.Invoke(0);
        }
        public void StartDownload(int i)
        {
            lock (gate)
            {
                downloading[i] = true; activeDownloads++; lastNetwork[i] = -1;
                Emit(i, "Downloading resources");
            }
        }
        public void Download(int i, long value)
        {
            lock (gate)
            {
                value = Math.Max(0, Math.Min(value, manifest.Entries[i].Length));
                // Transports report their initial offset before reading bytes. A
                // server's 200 Range fallback reports zero, not the stale offset.
                if (lastNetwork[i] >= 0 && value > lastNetwork[i]) networkBytes += value - lastNetwork[i];
                lastNetwork[i] = value;
                SetBytes(i, value);
                networkReport?.Invoke(networkBytes);
                Emit(i, "Downloading resources");
            }
        }
        public void EndDownload(int i)
        {
            lock (gate) { if (downloading[i]) { downloading[i] = false; activeDownloads--; } }
        }
        public void Local(int i, string phase, long value)
        {
            lock (gate)
            {
                if (phase == "Reusing installed resources") SetBytes(i, value);
                Emit(i, phase);
            }
        }
        public void Complete(int i)
        {
            lock (gate) { SetBytes(i, manifest.Entries[i].Length); complete++; Emit(i, "Preparing resource update"); }
        }
        private void SetBytes(int i, long value)
        {
            value = Math.Max(0, Math.Min(value, manifest.Entries[i].Length));
            done += value - bytes[i]; bytes[i] = value;
        }
        private void Emit(int i, string phase)
        {
            if (activeDownloads > 0) phase = "Downloading resources";
            Report(report, phase, complete + "/" + bytes.Length + "  " + manifest.Entries[i].Destination, done, total);
        }
    }

    // Receipt checks detect ordinary file changes, not malicious same-size/same-timestamp
    // edits. Explicit repair bypasses the receipt and reads every payload byte.
    public string UseInstalled(CancellationToken cancellation, Action<string, string, long, long> progress, bool fullVerification = false)
    {
        string root = ReadActiveRoot();
        var manifest = ReadInstalledManifest(root);
        ValidateManifest(manifest, appVersion, platform);
        ValidateTree(root, manifest, false);
        var verification = new MobileResourceVerification(manifest, root, Path.Combine(root, Receipt), !fullVerification);
        try
        {
        long total = verification.BytesToHash, completed = 0;
        Report(progress, fullVerification ? "Repairing resource verification" : "Checking installed resource metadata",
            fullVerification ? "Reading every installed resource for a complete SHA-256 check..." : "Checking the saved verification receipt...", 0, total);
        for (int i = 0; i < manifest.Entries.Count; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            var entry = manifest.Entries[i];
            long before = completed;
            bool needsHash = verification.NeedsHash(i);
            SafePath(root, entry.Destination);
            if (!verification.Verify(i,
                bytes => { cancellation.ThrowIfCancellationRequested(); Report(progress, "Checking installed resources", entry.Destination, before + bytes, total); }))
                throw new InvalidDataException("Installed resource is missing or corrupt: " + entry.Destination + ". Download again to repair it.");
            if (needsHash) completed += entry.Length;
        }
        cancellation.ThrowIfCancellationRequested();
        verification.CommitReceipt();
        return root;
        }
        finally { verification.CheckpointReceipt(); }
    }

    private MobileResourceManifest ReadInstalledManifest(string root)
    {
        string path = Path.Combine(root, StoredManifest);
        RejectLink(path);
        var manifest = MobileResourceManifest.Parse(ReadManifest(path));
        if (!Path.GetFileName(root).StartsWith(manifest.Version + "-", StringComparison.Ordinal))
            throw new InvalidDataException("Installed resource manifest identity changed.");
        return manifest;
    }

    private sealed class ReusableResource
    {
        public readonly object Gate = new object();
        public string Path;
        public MobileResourceVerification Verification;
        public int Index;
    }

    private Dictionary<string, ReusableResource> FindReusableFiles(bool useCachedReceipt)
    {
        var result = new Dictionary<string, ReusableResource>(StringComparer.Ordinal);
        try
        {
            string root = ReadActiveRoot();
            var previous = ReadInstalledManifest(root);
            // Old app-compatible content can be reused solely as bytes. Explicit repair
            // must hash source bytes too, never revive a rejected file through its receipt.
            ValidateManifest(previous, previous.ClientVersion, platform);
            ValidateTree(root, previous, false);
            var verification = new MobileResourceVerification(previous, root, Path.Combine(root, Receipt), useCachedReceipt);
            for (int i = 0; i < previous.Entries.Count; i++)
            {
                var entry = previous.Entries[i];
                string path = SafePath(root, entry.Destination);
                if (File.Exists(path) && new FileInfo(path).Length == entry.Length)
                    result[ContentKey(entry)] = new ReusableResource { Path = path, Verification = verification, Index = i };
            }
        }
        catch (InvalidDataException) { }
        catch (IOException) { }
        return result;
    }

    private static bool TryReuse(MobileResourceVerification verification, int index, ReusableResource source, string part,
        MobileResourceManifest.Entry entry, CancellationToken cancellation, Action<string, string, long, long> progress, string label)
    {
        lock (source.Gate)
        try
        {
            cancellation.ThrowIfCancellationRequested();
            RejectPathLinks(source.Path);
            if (!source.Verification.Verify(source.Index, bytes => {
                cancellation.ThrowIfCancellationRequested(); Report(progress, "Verifying reused resource", label, bytes, entry.Length);
            })) return false;
            var proof = source.Verification.CopyVerified(source.Index, part, cancellation,
                bytes => Report(progress, "Reusing installed resources", label, bytes, entry.Length));
            if (proof == null) return false;
            verification.CommitFile(index, part, proof);
            return true;
        }
        catch (InvalidDataException) { if (File.Exists(part)) File.Delete(part); return false; }
        finally { source.Verification.CheckpointReceipt(); }
        // I/O failures stop safely; never disguise full storage with a network retry.
    }

    private static string ContentKey(MobileResourceManifest.Entry entry)
    { return entry.Sha256 + ":" + entry.Length.ToString(CultureInfo.InvariantCulture); }

    private static string ResumeIdentity(Uri source, MobileResourceManifest.Entry entry)
    { return "FFO-RESOURCE-PART-1\n" + source.AbsoluteUri + "\n" + ContentKey(entry) + "\n"; }

    private static long PartialLength(string part, string identity)
    {
        string receipt = part + ".resume";
        RejectLink(part); RejectLink(receipt); RejectLink(receipt + ".part");
        if (!File.Exists(part) || !File.Exists(receipt) || new FileInfo(receipt).Length != StrictUtf8.GetByteCount(identity)) return 0;
        try { if (File.ReadAllText(receipt, StrictUtf8) != identity) return 0; }
        catch (DecoderFallbackException) { return 0; } // Damaged local metadata restarts safely.
        return new FileInfo(part).Length;
    }

    private static long PreparePartial(string part, string identity, long expectedBytes)
    {
        long offset = PartialLength(part, identity);
        if (offset > 0 && offset <= expectedBytes) return offset;
        DeletePartial(part);
        string receipt = part + ".resume", temporary = receipt + ".part";
        WriteDurable(temporary, identity);
        File.Move(temporary, receipt);
        return 0;
    }

    private static void DeletePartial(string part)
    {
        foreach (string path in new[] { part, part + ".resume", part + ".resume.part" })
        { RejectLink(path); if (File.Exists(path)) File.Delete(path); }
    }

    public static long RequiredFreeBytes(long additionalPayloadBytes, long manifestBytes, int entryCount)
    {
        if (additionalPayloadBytes < 0 || manifestBytes < 0 || entryCount < 0) throw new ArgumentOutOfRangeException();
        // Includes manifest, receipt/resume metadata, per-file allocation overhead and a small margin.
        return checked(additionalPayloadBytes + manifestBytes + (long)entryCount * 4096 + 1024 * 1024);
    }

    private string ReadActiveRoot()
    {
        RejectPathLinks(storage);
        string marker = Path.Combine(storage, ActiveMarker);
        RejectLink(marker);
        if (!File.Exists(marker) || new FileInfo(marker).Length > 128)
            throw new InvalidDataException("No verified resource set is available yet. Download resources first.");
        string generation = File.ReadAllText(marker, StrictUtf8).Trim();
        if (generation.Length != 97 || generation[64] != '-' || !IsHex(generation.Substring(0, 64)) || !IsHex(generation.Substring(65)))
            throw new InvalidDataException("Invalid installed resource marker. Download resources again.");
        string root = Path.Combine(storage, generation);
        RejectLink(root);
        return root;
    }

    public static void ValidateManifest(MobileResourceManifest manifest, string appVersion, string platform = "android")
    {
        ValidatePlatform(platform);
        if (manifest.Offline || manifest.ClientPlatform != platform || manifest.ClientVersion != appVersion)
            throw new InvalidDataException("The resource manifest must declare @client, " + platform + ", and this exact application version.");
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var canonicalSegments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        foreach (var entry in manifest.Entries)
        {
            ValidateResourcePath(entry.Source, false);
            ValidateResourcePath(entry.Destination, true);
            if (!paths.Add(entry.Destination)) throw new InvalidDataException("Conflicting resource paths.");
            string prefix = "";
            foreach (string segment in entry.Destination.Split('/'))
            {
                prefix = prefix.Length == 0 ? segment : prefix + "/" + segment;
                string previous;
                if (canonicalSegments.TryGetValue(prefix, out previous) && previous != prefix)
                    throw new InvalidDataException("Resource directory casing conflicts.");
                canonicalSegments[prefix] = prefix;
            }
            total = checked(total + entry.Length);
            if (total > MaxTotalBytes) throw new InvalidDataException("Resource set exceeds the 64 GiB installation limit.");
        }
        foreach (string path in paths)
        {
            int slash = path.IndexOf('/');
            while (slash >= 0)
            {
                if (paths.Contains(path.Substring(0, slash))) throw new InvalidDataException("Resource file/directory paths conflict.");
                slash = path.IndexOf('/', slash + 1);
            }
        }
    }

    private static void ValidatePlatform(string platform)
    {
        if (platform != "android" && platform != "windows" && platform != "macos" && platform != "linux")
            throw new InvalidDataException("Unsupported resource client platform.");
    }

    private static bool IsPassiveOverlayPath(string path)
    {
        // Data-only overrides consumed ahead of PKGs. Match the publisher's narrow
        // directory rules; this is not a generic respatch or extension exemption.
        string[] pieces = path.Split('/');
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return (pieces.Length == 4 && path.StartsWith("gameassets/respatch/effect/", StringComparison.Ordinal) && ext == ".eff") ||
            (pieces.Length == 5 && path.StartsWith("gameassets/respatch/magic/magic/", StringComparison.Ordinal) && ext == ".gso") ||
            (pieces.Length == 5 && path.StartsWith("gameassets/respatch/item/item/", StringComparison.Ordinal) && ext == ".gso");
    }

    private static void ValidateResourcePath(string path, bool destination)
    {
        MobileResourceManifest.ValidateRelativePath(path);
        if (path.Length > 1024 || path.Split('/').Length > 64)
            throw new InvalidDataException("Resource path exceeds its length or depth limit.");
        // Cross-platform safe names; no alternate streams, URL escaping or hidden control bytes.
        foreach (char c in path)
            if (char.IsControl(c) || c == '<' || c == '>' || c == '"' || c == '|' || c == '*')
                throw new InvalidDataException("Unsafe resource filename.");
        foreach (string segment in path.Split('/'))
        {
            string name = segment.Split('.')[0].ToUpperInvariant();
            if (name == "CON" || name == "PRN" || name == "AUX" || name == "NUL" ||
                (name.Length == 4 && (name.StartsWith("COM") || name.StartsWith("LPT")) && name[3] >= '1' && name[3] <= '9'))
                throw new InvalidDataException("Reserved resource filename.");
        }
        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (!destination)
        {
            // A source may be in a publisher's data folder, but must still be passive data.
            if (!IsPassiveOverlayPath(path) && ext != ".pkg" && ext != ".ogg" && ext != ".wav" && ext != ".json" && ext != ".png" &&
                ext != ".jpg" && ext != ".jpeg" && ext != ".bytes" && ext != ".txt" && ext != ".bin" && ext != ".idx" && ext != ".wid")
                throw new InvalidDataException("Only passive resource files can be downloaded.");
            return;
        }
        bool fontMetadata = (path.StartsWith("ReferenceClient/font/", StringComparison.Ordinal) || path.StartsWith("gameassets/font/", StringComparison.Ordinal)) &&
            (ext == ".idx" || ext == ".wid");
        bool allowed = IsPassiveOverlayPath(path) || fontMetadata || (path.StartsWith("data/", StringComparison.Ordinal) && path.Substring(5).IndexOf('/') < 0 && ext == ".pkg") ||
            (path.StartsWith("Music/", StringComparison.Ordinal) && ext == ".ogg") ||
            ((path.StartsWith("sndeffect/", StringComparison.Ordinal) || path.StartsWith("sndeffect24_pcm/", StringComparison.Ordinal)) && (ext == ".wav" || ext == ".ogg")) ||
            ((path.StartsWith("gameassets/", StringComparison.Ordinal) || path.StartsWith("ReferenceClient/", StringComparison.Ordinal)) && (ext == ".json" || ext == ".png" || ext == ".jpg" || ext == ".jpeg" ||
                ext == ".bytes" || ext == ".txt" || ext == ".bin" || ext == ".ogg" || ext == ".wav"));
        if (!allowed) throw new InvalidDataException("Unsupported resource destination (plugins and configuration are not downloaded): " + path);
    }

    public static Uri ResourceUri(Uri manifestUri, string source)
    {
        ValidateResourcePath(source, false);
        var escaped = new List<string>();
        foreach (string part in source.Split('/')) escaped.Add(Uri.EscapeDataString(part));
        Uri baseUri = new Uri(manifestUri, ".");
        Uri result = new Uri(baseUri, string.Join("/", escaped));
        if (result.Scheme != manifestUri.Scheme || result.Authority != manifestUri.Authority || !baseUri.IsBaseOf(result))
            throw new InvalidDataException("Resource URL escaped the configured origin or directory.");
        return result;
    }

    private static string ReadManifest(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length > MobileResourceManifest.MaxManifestBytes)
            throw new InvalidDataException("Resource manifest is missing or exceeds its size limit.");
        return StrictUtf8.GetString(File.ReadAllBytes(path));
    }

    private static string SafePath(string root, string relative)
    {
        string path = root;
        RejectLink(root);
        foreach (string segment in relative.Split('/')) { path = Path.Combine(path, segment); RejectLink(path); }
        return path;
    }

    private static void RejectPathLinks(string path)
    {
        string current = Path.GetFullPath(path);
        while (!string.IsNullOrEmpty(current))
        { RejectLink(current); current = Path.GetDirectoryName(current); }
    }

    private static void RejectLink(string path)
    {
        // File.GetAttributes also detects dangling links on supported runtimes.
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Symbolic links are not allowed in installed resources.");
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
    }

    private static void ValidateTree(string root, MobileResourceManifest manifest, bool staging)
    {
        var expected = new HashSet<string>(StringComparer.Ordinal);
        expected.Add(StoredManifest); expected.Add(Receipt); expected.Add(Receipt + ".part");
        foreach (var entry in manifest.Entries)
        {
            expected.Add(entry.Destination);
            if (staging)
            { expected.Add(entry.Destination + ".part"); expected.Add(entry.Destination + ".part.resume"); expected.Add(entry.Destination + ".part.resume.part"); }
        }
        var pending = new Stack<string>(); pending.Push(root);
        while (pending.Count > 0)
        {
            string directory = pending.Pop(); RejectLink(directory);
            foreach (string path in Directory.GetFileSystemEntries(directory))
            {
                RejectLink(path);
                if (Directory.Exists(path)) pending.Push(path);
                else if (!expected.Contains(path.Substring(root.Length + 1).Replace(Path.DirectorySeparatorChar, '/')))
                    throw new InvalidDataException("Unexpected file in resource staging or installed set.");
            }
        }
    }

    private void CommitMarker(string generation, CancellationToken cancellation)
    {
        string marker = Path.Combine(storage, ActiveMarker), temporary = marker + ".part";
        RejectLink(marker); RejectLink(temporary);
        cancellation.ThrowIfCancellationRequested();
        WriteDurable(temporary, generation + "\n");
        cancellation.ThrowIfCancellationRequested();
        if (File.Exists(marker)) File.Replace(temporary, marker, null);
        else File.Move(temporary, marker);
    }

    private static void WriteDurable(string path, string text)
    {
        byte[] bytes = StrictUtf8.GetBytes(text);
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
    }
    private static bool IsHex(string text)
    { foreach (char c in text) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false; return true; }
    private static void Report(Action<string, string, long, long> report, string phase, string detail, long done, long total)
    { report?.Invoke(phase, detail, done, total); }
}
}

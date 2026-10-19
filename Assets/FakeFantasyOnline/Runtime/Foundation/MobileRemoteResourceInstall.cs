using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using System.Threading;

namespace FakeFantasyOnline.Foundation
{
/// <summary>Remote data-only install. Content-addressed staging is never a loader root.</summary>
public sealed class MobileRemoteResourceInstall
{
    public const long MaxTotalBytes = 64L * 1024 * 1024 * 1024;
    private const string StoredManifest = ".resource-manifest.tsv", Receipt = ".resource-verified";
    private const string ActiveMarker = "active-resource-set.txt";
    private readonly string storage, appVersion, platform;
    private readonly Func<string, long> availableBytes;
    private readonly bool developmentHttp;
    private readonly IMobileResourceTransport transport;
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public MobileRemoteResourceInstall(string storage, string appVersion, bool developmentHttp, IMobileResourceTransport transport,
        string platform = "android", Func<string, long> availableBytes = null)
    {
        this.storage = Path.GetFullPath(storage);
        this.appVersion = appVersion;
        ValidatePlatform(platform);
        this.platform = platform;
        this.availableBytes = availableBytes ?? (path => new DriveInfo(path).AvailableFreeSpace);
        this.developmentHttp = developmentHttp;
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public string Install(string manifestUrl, CancellationToken cancellation, Action<string, string, long, long> progress,
        long manifestBytes = 0, string manifestSha256 = null, bool fullVerification = false)
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
            return InstallLocked(uri, cancellation, progress, manifestBytes, manifestSha256, fullVerification);
    }

    private string InstallLocked(Uri uri, CancellationToken cancellation, Action<string, string, long, long> progress,
        long manifestBytes, string manifestSha256, bool fullVerification)
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
        var verification = new MobileResourceVerification(manifest, staging, Path.Combine(staging, Receipt), false);
        var reusable = FindReusableFiles();
        long total = 0, completed = 0, additional = 0;
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
        for (int i = 0; i < manifest.Entries.Count; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            var entry = manifest.Entries[i];
            string label = (i + 1) + "/" + manifest.Entries.Count + "  " + entry.Destination;
            string target = SafePath(staging, entry.Destination), part = target + ".part";
            RejectLink(part); RejectLink(part + ".resume"); RejectLink(part + ".resume.part");
            long previous = completed;
            Report(progress, "Checking resources", label, completed, total);
            if (!verification.Verify(i, bytes => { cancellation.ThrowIfCancellationRequested(); Report(progress, "Checking resources", label, previous + bytes, total); }))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                Uri source = ResourceUri(uri, entry.Source);
                long offset = PreparePartial(part, ResumeIdentity(source, entry), entry.Length);
                bool ready = false;
                // A process may stop after its last received byte but before verification.
                if (offset == entry.Length && File.Exists(part))
                {
                    try { verification.CommitFile(i, part, bytes => { cancellation.ThrowIfCancellationRequested(); Report(progress, "Verifying retained download", label, bytes, entry.Length); }); ready = true; }
                    catch (InvalidDataException) { DeletePartial(part); offset = PreparePartial(part, ResumeIdentity(source, entry), entry.Length); }
                }
                string reusablePath;
                if (!ready && offset == 0 && reusable.TryGetValue(ContentKey(entry), out reusablePath))
                    ready = TryReuse(verification, i, reusablePath, part, entry, cancellation, progress, label);
                if (!ready)
                {
                    var resumable = transport as IMobileResourceResumableTransport;
                    if (resumable != null)
                        resumable.DownloadResumable(source, part, entry.Length, offset, cancellation,
                            bytes => Report(progress, "Downloading resources", label, previous + bytes, total));
                    else
                        transport.Download(source, part, entry.Length, cancellation,
                            bytes => Report(progress, "Downloading resources", label, previous + bytes, total));
                    cancellation.ThrowIfCancellationRequested();
                    try
                    {
                        verification.CommitFile(i, part,
                            bytes => { cancellation.ThrowIfCancellationRequested(); Report(progress, "Verifying downloaded resource", label, bytes, entry.Length); });
                    }
                    catch (InvalidDataException) { DeletePartial(part); throw; }
                    // Interrupted/rejected responses retain the same identity-bound partial.
                    // A complete payload that fails its final hash is discarded.
                }
            }
            DeletePartial(part); // No payload fragments or resume metadata enter the loader root.
            completed += entry.Length;
        }
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
        return installed;
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

    private MobileResourceManifest ReadInstalledManifest(string root)
    {
        string path = Path.Combine(root, StoredManifest);
        RejectLink(path);
        var manifest = MobileResourceManifest.Parse(ReadManifest(path));
        if (!Path.GetFileName(root).StartsWith(manifest.Version + "-", StringComparison.Ordinal))
            throw new InvalidDataException("Installed resource manifest identity changed.");
        return manifest;
    }

    private Dictionary<string, string> FindReusableFiles()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            string root = ReadActiveRoot();
            var previous = ReadInstalledManifest(root);
            // Old app-compatible content can be reused solely as bytes. The new manifest
            // remains the authority and the copied bytes are always fully SHA-256 checked.
            ValidateManifest(previous, previous.ClientVersion, platform);
            ValidateTree(root, previous, false);
            foreach (var entry in previous.Entries)
            {
                string path = SafePath(root, entry.Destination);
                if (File.Exists(path) && new FileInfo(path).Length == entry.Length)
                    result[ContentKey(entry)] = path;
            }
        }
        catch (InvalidDataException) { }
        catch (IOException) { }
        return result;
    }

    private static bool TryReuse(MobileResourceVerification verification, int index, string source, string part,
        MobileResourceManifest.Entry entry, CancellationToken cancellation, Action<string, string, long, long> progress, string label)
    {
        try
        {
            RejectPathLinks(source);
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var output = new FileStream(part, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                MobileResourceHttpTransport.CopyBounded(input, output, entry.Length, cancellation,
                    bytes => Report(progress, "Reusing installed resources", label, bytes, entry.Length));
                output.Flush(true);
            }
            verification.CommitFile(index, part, bytes => { cancellation.ThrowIfCancellationRequested(); Report(progress, "Verifying reused resource", label, bytes, entry.Length); });
            return true;
        }
        catch (InvalidDataException) { if (File.Exists(part)) File.Delete(part); return false; }
        // Real I/O failures (e.g. no space) stop safely rather than hiding them with a download.
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
            (pieces.Length == 5 && path.StartsWith("gameassets/respatch/magic/magic/", StringComparison.Ordinal) && ext == ".gso");
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

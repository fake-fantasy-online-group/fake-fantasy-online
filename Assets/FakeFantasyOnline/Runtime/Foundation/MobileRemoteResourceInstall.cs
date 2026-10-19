using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace FakeFantasyOnline.Foundation
{
/// <summary>Remote Android data-only install. Content-addressed staging is never a loader root.</summary>
public sealed class MobileRemoteResourceInstall
{
    public const long MaxTotalBytes = 64L * 1024 * 1024 * 1024;
    private const string StoredManifest = ".resource-manifest.tsv", Receipt = ".resource-verified";
    private const string ActiveMarker = "active-resource-set.txt";
    private readonly string storage, appVersion;
    private readonly bool developmentHttp;
    private readonly IMobileResourceTransport transport;
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public MobileRemoteResourceInstall(string storage, string appVersion, bool developmentHttp, IMobileResourceTransport transport)
    {
        this.storage = Path.GetFullPath(storage);
        this.appVersion = appVersion;
        this.developmentHttp = developmentHttp;
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public string Install(string manifestUrl, CancellationToken cancellation, Action<string, string, long, long> progress)
    {
        Uri uri = MobileResourceBootstrapConfig.ValidateEndpoint(manifestUrl, developmentHttp);
        Directory.CreateDirectory(storage);
        RejectLink(storage);
        string lockPath = Path.Combine(storage, ".install.lock");
        RejectLink(lockPath);
        using (var installLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            return InstallLocked(uri, cancellation, progress);
    }

    private string InstallLocked(Uri uri, CancellationToken cancellation, Action<string, string, long, long> progress)
    {
        // A unique metadata temporary prevents an interrupted attempt being mistaken for a manifest.
        string temporaryManifest = Path.Combine(storage, "manifest-" + Guid.NewGuid().ToString("N") + ".part");
        string text;
        try
        {
            Report(progress, "Downloading resource manifest", "Reading the configured resource endpoint...", 0, 0);
            transport.Download(uri, temporaryManifest, MobileResourceManifest.MaxManifestBytes, cancellation, null);
            text = ReadManifest(temporaryManifest);
        }
        finally { if (File.Exists(temporaryManifest)) File.Delete(temporaryManifest); }
        var manifest = MobileResourceManifest.Parse(text);
        ValidateManifest(manifest, appVersion);
        cancellation.ThrowIfCancellationRequested();
        // Repeated Download clicks for an unchanged manifest do not redownload the entire set.
        // Full verification still runs. An invalid old set is retained while a replacement stages.
        string activeRoot = null;
        try
        {
            string candidate = ReadActiveRoot();
            if (Path.GetFileName(candidate).StartsWith(manifest.Version + "-", StringComparison.Ordinal))
                activeRoot = UseInstalled(cancellation, progress);
        }
        catch (InvalidDataException) { }
        catch (IOException) { }
        if (activeRoot != null) return activeRoot;
        string staging = Path.Combine(storage, manifest.Version + ".staging");
        Directory.CreateDirectory(staging);
        ValidateTree(staging, manifest, true);
        var verification = new MobileResourceVerification(manifest, staging, Path.Combine(staging, Receipt));
        long total = 0, completed = 0;
        foreach (var entry in manifest.Entries) total = checked(total + entry.Length);
        for (int i = 0; i < manifest.Entries.Count; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            var entry = manifest.Entries[i];
            string label = (i + 1) + "/" + manifest.Entries.Count + "  " + entry.Destination;
            string target = SafePath(staging, entry.Destination);
            string part = target + ".part";
            RejectLink(part);
            // Also remove a stale part left by a process kill when its final file is valid.
            if (File.Exists(part)) File.Delete(part);
            long previous = completed;
            Report(progress, "Checking resources", label, completed, total);
            if (!verification.Verify(i, bytes => { cancellation.ThrowIfCancellationRequested(); Report(progress, "Checking resources", label, previous + bytes, total); }))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                // First version deliberately restarts the CURRENT file. Completed verified files
                // in this manifest-specific staging directory are reused; no Range claim is made.
                if (File.Exists(part)) File.Delete(part);
                Uri source = ResourceUri(uri, entry.Source);
                try
                {
                    transport.Download(source, part, entry.Length, cancellation,
                        bytes => Report(progress, "Downloading resources", label + " (current file restarts on retry)", previous + bytes, total));
                    cancellation.ThrowIfCancellationRequested();
                    verification.CommitFile(i, part,
                        bytes => { cancellation.ThrowIfCancellationRequested(); Report(progress, "Verifying downloaded resource", label, bytes, entry.Length); });
                }
                finally { if (File.Exists(part)) File.Delete(part); }
            }
            completed += entry.Length;
        }
        cancellation.ThrowIfCancellationRequested();
        Report(progress, "Finishing resource check", "Activating only the complete verified resource set...", 0, 0);
        ValidateTree(staging, manifest, false);
        verification.CommitReceipt(); // Rechecks every verified file's length and timestamp.
        WriteDurable(Path.Combine(staging, StoredManifest), text);
        cancellation.ThrowIfCancellationRequested();
        // A fresh final name preserves every previous valid installation, even if a previous
        // generation with this manifest was damaged. Rename stays on the same filesystem.
        string generation = manifest.Version + "-" + Guid.NewGuid().ToString("N");
        string installed = Path.Combine(storage, generation);
        Directory.Move(staging, installed);
        // Atomic marker replacement is the persistent activation boundary. Failure here leaves
        // the old marker and old directory intact; the new directory is merely an orphan.
        CommitMarker(generation);
        return installed;
    }

    public string UseInstalled(CancellationToken cancellation, Action<string, string, long, long> progress)
    {
        string root = ReadActiveRoot();
        string generation = Path.GetFileName(root);
        string manifestPath = Path.Combine(root, StoredManifest);
        RejectLink(manifestPath);
        var manifest = MobileResourceManifest.Parse(ReadManifest(manifestPath));
        ValidateManifest(manifest, appVersion);
        if (!generation.StartsWith(manifest.Version + "-", StringComparison.Ordinal))
            throw new InvalidDataException("Installed resource manifest identity changed.");
        ValidateTree(root, manifest, false);
        long total = 0, completed = 0;
        foreach (var entry in manifest.Entries) total = checked(total + entry.Length);
        // A cached set may be used offline, but never trust a directory or marker alone.
        var verification = new MobileResourceVerification(manifest, root, Path.Combine(root, Receipt), false);
        for (int i = 0; i < manifest.Entries.Count; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            var entry = manifest.Entries[i];
            long before = completed;
            SafePath(root, entry.Destination);
            if (!verification.Verify(i,
                bytes => { cancellation.ThrowIfCancellationRequested(); Report(progress, "Checking installed resources", entry.Destination, before + bytes, total); }))
                throw new InvalidDataException("Installed resource is missing or corrupt: " + entry.Destination + ". Download again to repair it.");
            completed += entry.Length;
        }
        cancellation.ThrowIfCancellationRequested();
        verification.CommitReceipt(); // Also rejects changes to an earlier file while later files hash.
        return root;
    }

    private string ReadActiveRoot()
    {
        RejectLink(storage);
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

    public static void ValidateManifest(MobileResourceManifest manifest, string appVersion)
    {
        if (manifest.Offline || manifest.ClientPlatform != "android" || manifest.ClientVersion != appVersion)
            throw new InvalidDataException("The resource manifest must declare @client, android, and this exact application version.");
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
            if (ext != ".pkg" && ext != ".ogg" && ext != ".wav" && ext != ".json" && ext != ".png" &&
                ext != ".jpg" && ext != ".jpeg" && ext != ".bytes" && ext != ".txt" && ext != ".bin" && ext != ".idx" && ext != ".wid")
                throw new InvalidDataException("Only passive resource files can be downloaded.");
            return;
        }
        bool fontMetadata = (path.StartsWith("ReferenceClient/font/", StringComparison.Ordinal) || path.StartsWith("gameassets/font/", StringComparison.Ordinal)) &&
            (ext == ".idx" || ext == ".wid");
        bool allowed = fontMetadata || (path.StartsWith("data/", StringComparison.Ordinal) && path.Substring(5).IndexOf('/') < 0 && ext == ".pkg") ||
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
        { expected.Add(entry.Destination); if (staging) expected.Add(entry.Destination + ".part"); }
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

    private void CommitMarker(string generation)
    {
        string marker = Path.Combine(storage, ActiveMarker), temporary = marker + ".part";
        RejectLink(marker); RejectLink(temporary);
        WriteDurable(temporary, generation + "\n");
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

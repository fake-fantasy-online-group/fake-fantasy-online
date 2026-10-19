using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace FakeFantasyOnline.Foundation
{
/// <summary>
/// Downloads a newer client package as inert bytes into a private cache. This class never
/// extracts, loads, starts, installs or replaces the running client. The dispatch is the
/// authority for its full-file SHA-256; this is integrity checking, not publisher signing.
/// </summary>
public sealed class ClientArtifactDownload
{
    public const long MaxArtifactBytes = 8L * 1024 * 1024 * 1024;
    public const long StorageReserveBytes = 1024 * 1024 + 16384;
    private const string IdentityFile = "identity.txt", PartialFile = "download.part";
    private readonly string storage, currentVersion, platform;
    private readonly bool developmentHttp;
    private readonly IMobileResourceTransport transport;
    private readonly Func<string, long> availableBytes;
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public ClientArtifactDownload(string storageRoot, string currentVersion, string platform,
        bool allowDevelopmentLanHttp = false, IMobileResourceTransport transport = null,
        Func<string, long> availableBytes = null)
    {
        if (string.IsNullOrWhiteSpace(storageRoot)) throw new ArgumentException("A private package cache is required.", nameof(storageRoot));
        storage = Path.GetFullPath(storageRoot);
        if (storage == Path.GetPathRoot(storage)) throw new ArgumentException("The filesystem root cannot be a package cache.", nameof(storageRoot));
        if (!ClientProductVersion.IsValid(currentVersion)) throw new InvalidDataException("Invalid current application version.");
        if (platform != "android" && platform != "windows" && platform != "macos" && platform != "linux")
            throw new InvalidDataException("Unsupported client package platform.");
        this.currentVersion = currentVersion;
        this.platform = platform;
        developmentHttp = allowDevelopmentLanHttp;
        this.transport = transport ?? new MobileResourceHttpTransport();
        this.availableBytes = availableBytes ?? (path => new DriveInfo(path).AvailableFreeSpace);
    }

    /// <summary>Blocking worker-thread operation. Cancellation preserves identity-bound partial bytes.</summary>
    public string Download(ClientUpdatePlan.ClientArtifact artifact, CancellationToken cancellation,
        Action<string, string, long, long> progress = null)
    {
        // Snapshot all metadata before invoking transports or callbacks. Caller mutations cannot
        // change the requested URL, platform, expected version, length or digest mid-transfer.
        var expected = Snapshot(artifact);
        cancellation.ThrowIfCancellationRequested();
        EnsureDirectory(storage);
        using (var cacheLock = LockCache())
        {
            string directory = PackageDirectory(expected);
            EnsureDirectory(directory);
            string completed = Child(directory, "package" + expected.Extension);
            string partial = Child(directory, PartialFile);
            string identity = Child(directory, IdentityFile);
            bool bound = IdentityMatches(identity, expected.Identity);
            if (!bound)
            {
                if (File.Exists(identity)) throw new InvalidDataException("Client package cache identity is corrupt. Use a clean cache.");
                // Orphaned bytes have no authority to resume or become a completed package.
                DeleteFile(partial);
                DeleteFile(completed);
            }
            if (bound && File.Exists(completed))
            {
                Report(progress, "Checking client package", expected.Label, 0, expected.Size);
                if (VerifyBytes(completed, expected, cancellation, progress, "Checking client package"))
                    return Ready(completed, expected, cancellation, progress);
                DeleteFile(completed);
            }
            long offset = bound && File.Exists(partial) ? FileLength(partial) : 0;
            if (offset > expected.Size) { DeleteFile(partial); offset = 0; }
            // A process can stop after receiving the final byte and before the atomic move.
            if (offset == expected.Size)
            {
                if (VerifyBytes(partial, expected, cancellation, progress, "Verifying client package"))
                    return Publish(partial, completed, expected, cancellation, progress);
                DeleteFile(partial);
                offset = 0;
            }
            var resumable = transport as IMobileResourceResumableTransport;
            long required = checked(expected.Size - (resumable == null ? 0 : offset) + StorageReserveBytes);
            long free = availableBytes(storage);
            if (free < required)
                throw new IOException("Insufficient free storage for client package: need " + required.ToString(CultureInfo.InvariantCulture) +
                    " additional bytes; available " + free.ToString(CultureInfo.InvariantCulture) + ".");
            cancellation.ThrowIfCancellationRequested();
            if (!bound) WriteIdentity(identity, expected.Identity);
            RejectPathLinks(partial);
            if (resumable == null) offset = 0;
            Report(progress, "Downloading client package", expected.Label, offset, expected.Size);
            Action<long> received = bytes => {
                cancellation.ThrowIfCancellationRequested();
                if (bytes < 0 || bytes > expected.Size) throw new InvalidDataException("Invalid client package transfer progress.");
                Report(progress, "Downloading client package", expected.Label, bytes, expected.Size);
            };
            RejectPathLinks(partial); // The initial UI callback must not redirect the file path.
            // The default transport bounds every write, rejects redirects and validates Range.
            // Every transport still goes through independent exact-size and full-hash checking.
            try
            {
                if (resumable != null)
                    resumable.DownloadResumable(expected.Uri, partial, expected.Size, offset, cancellation, received);
                else
                    transport.Download(expected.Uri, partial, expected.Size, cancellation, received);
            }
            catch
            {
                // Ordinary interruption retains bounded bytes. An oversized file can never
                // be resumed, even when a custom transport failed before final verification.
                RejectPathLinks(partial);
                if (File.Exists(partial) && FileLength(partial) > expected.Size) DeleteFile(partial);
                throw;
            }
            cancellation.ThrowIfCancellationRequested();
            if (!IdentityMatches(identity, expected.Identity))
                throw new InvalidDataException("Client package identity changed during download.");
            if (!VerifyBytes(partial, expected, cancellation, progress, "Verifying client package"))
            {
                DeleteFile(partial);
                throw new InvalidDataException("Client package length or SHA-256 does not match dispatch metadata.");
            }
            return Publish(partial, completed, expected, cancellation, progress);
        }
    }

    /// <summary>
    /// Rechecks the exact cached package and every byte before each OS handoff. Does not use
    /// timestamps or a previous success flag as proof. Throws if missing, changed or invalid.
    /// The caller must perform its platform handoff immediately after this worker completes.
    /// </summary>
    public string VerifyCompleted(ClientUpdatePlan.ClientArtifact artifact, CancellationToken cancellation,
        Action<string, string, long, long> progress = null)
    {
        var expected = Snapshot(artifact);
        cancellation.ThrowIfCancellationRequested();
        RejectPathLinks(storage);
        if (!Directory.Exists(storage)) throw new InvalidDataException("No downloaded client package is available.");
        using (var cacheLock = LockCache())
        {
            string directory = PackageDirectory(expected);
            string identity = Child(directory, IdentityFile);
            string completed = Child(directory, "package" + expected.Extension);
            if (!IdentityMatches(identity, expected.Identity) ||
                !VerifyBytes(completed, expected, cancellation, progress, "Checking client package"))
                throw new InvalidDataException("Downloaded client package is missing or corrupt. Download it again.");
            return Ready(completed, expected, cancellation, progress);
        }
    }

    private sealed class Expected
    {
        public Uri Uri;
        public string Sha256, Version, Extension, Identity, Key, Label;
        public long Size;
    }

    private Expected Snapshot(ClientUpdatePlan.ClientArtifact artifact)
    {
        if (artifact == null) throw new InvalidDataException("No client package was published for this platform.");
        string version = artifact.Version, url = artifact.Url, hash = artifact.Sha256;
        long size = artifact.SizeBytes;
        if (!ClientProductVersion.IsValid(version) || ClientProductVersion.Compare(version, currentVersion) <= 0)
            throw new InvalidDataException("Only a strictly newer stable application version may be downloaded.");
        if (size <= 0 || size > MaxArtifactBytes) throw new InvalidDataException("Client package exceeds the positive 8 GiB size limit.");
        if (hash == null || hash.Length != 64) throw new InvalidDataException("Invalid client package SHA-256.");
        foreach (char c in hash)
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                throw new InvalidDataException("Invalid client package SHA-256.");
        hash = hash.ToLowerInvariant();
        Uri uri = MobileResourceBootstrapConfig.ValidateEndpoint(url, developmentHttp);
        string extension = PackageExtension(uri, url);
        // Original URL (not a mutable latest alias inferred by this client), exact target
        // version, platform, length and complete digest all participate in resume identity.
        string identity = "FFO-CLIENT-PACKAGE-1\n" + platform + "\n" + version + "\n" + url + "\n" +
            size.ToString(CultureInfo.InvariantCulture) + "\n" + hash + "\n" + extension + "\n";
        return new Expected { Uri = uri, Sha256 = hash, Size = size, Version = version,
            Extension = extension, Identity = identity, Key = Hash(StrictUtf8.GetBytes(identity)),
            Label = platform + " v" + version + " (" + extension + ")" };
    }

    private string PackageExtension(Uri uri, string originalUrl)
    {
        string leaf = originalUrl.Substring(originalUrl.LastIndexOf('/') + 1);
        if (uri.AbsolutePath.EndsWith("/", StringComparison.Ordinal)) throw new InvalidDataException("Client package URL has no filename.");
        // Never reuse publisher-controlled filenames as local paths. Reject encoded/trailing
        // filename tricks rather than guessing which file type the server intended.
        if (leaf.Length == 0 || leaf.Length > 255) throw new InvalidDataException("Invalid client package filename.");
        foreach (char c in leaf)
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '.' || c == '-' || c == '_'))
                throw new InvalidDataException("Client package URL must end in a plain supported filename.");
        string[] extensions = platform == "android" ? new[] { ".apk" } :
            platform == "windows" ? new[] { ".exe", ".msi", ".zip" } :
            platform == "macos" ? new[] { ".dmg", ".pkg", ".zip" } :
            new[] { ".AppImage", ".tar.gz", ".deb", ".rpm", ".zip" };
        foreach (string extension in extensions)
            if (leaf.Length > extension.Length && leaf.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) return extension;
        throw new InvalidDataException("Unsupported client package extension for " + platform + ".");
    }

    private string PackageDirectory(Expected expected)
    {
        string directory = Path.Combine(storage, expected.Sha256, expected.Key);
        RejectPathLinks(directory);
        return directory;
    }

    private FileStream LockCache()
    {
        string path = Child(storage, ".client-download.lock");
        var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        try { RejectPathLinks(path); return stream; }
        catch { stream.Dispose(); throw; }
    }

    private static bool IdentityMatches(string path, string identity)
    {
        RejectPathLinks(path);
        if (!File.Exists(path) || FileLength(path) != StrictUtf8.GetByteCount(identity)) return false;
        try
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream, StrictUtf8, false))
                return reader.ReadToEnd() == identity;
        }
        catch (DecoderFallbackException) { return false; }
    }

    private static void WriteIdentity(string path, string identity)
    {
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".part";
        RejectPathLinks(path); RejectPathLinks(temporary);
        try
        {
            byte[] bytes = StrictUtf8.GetBytes(identity);
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { output.Write(bytes, 0, bytes.Length); output.Flush(true); }
            RejectPathLinks(path); RejectPathLinks(temporary);
            File.Move(temporary, path); // Never mutate an existing identity in place.
        }
        finally { DeleteFile(temporary); }
    }

    private static bool VerifyBytes(string path, Expected expected, CancellationToken cancellation,
        Action<string, string, long, long> progress, string phase)
    {
        cancellation.ThrowIfCancellationRequested();
        RejectPathLinks(path);
        if (!File.Exists(path) || FileLength(path) != expected.Size) return false;
        long before = File.GetLastWriteTimeUtc(path).Ticks;
        using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        using (var sha = SHA256.Create())
        {
            if (input.Length != expected.Size) return false;
            var buffer = new byte[65536];
            long done = 0;
            Report(progress, phase, expected.Label, 0, expected.Size);
            while (true)
            {
                cancellation.ThrowIfCancellationRequested();
                int count = input.Read(buffer, 0, buffer.Length);
                if (count == 0) break;
                if (count > expected.Size - done) return false;
                sha.TransformBlock(buffer, 0, count, buffer, 0);
                done += count;
                Report(progress, phase, expected.Label, done, expected.Size);
            }
            cancellation.ThrowIfCancellationRequested();
            sha.TransformFinalBlock(new byte[0], 0, 0);
            RejectPathLinks(path);
            return done == expected.Size && input.Length == expected.Size && FileLength(path) == expected.Size &&
                File.GetLastWriteTimeUtc(path).Ticks == before && Hex(sha.Hash) == expected.Sha256;
        }
    }

    private static string Publish(string partial, string completed, Expected expected, CancellationToken cancellation,
        Action<string, string, long, long> progress)
    {
        cancellation.ThrowIfCancellationRequested();
        RejectPathLinks(partial); RejectPathLinks(completed);
        if (!IdentityMatches(Child(Path.GetDirectoryName(partial), IdentityFile), expected.Identity))
            throw new InvalidDataException("Client package identity changed before publication.");
        File.Move(partial, completed); // Same-directory atomic publication; no overwrite.
        return Ready(completed, expected, cancellation, progress);
    }

    private static string Ready(string completed, Expected expected, CancellationToken cancellation,
        Action<string, string, long, long> progress)
    {
        cancellation.ThrowIfCancellationRequested();
        RejectPathLinks(completed);
        Report(progress, "Client package ready", expected.Label, expected.Size, expected.Size);
        cancellation.ThrowIfCancellationRequested();
        RejectPathLinks(completed);
        return completed;
    }

    private static string Child(string directory, string name)
    {
        string path = Path.Combine(directory, name);
        RejectPathLinks(path);
        if (Directory.Exists(path)) throw new InvalidDataException("A client package file is occupied by a directory.");
        return path;
    }

    private static long FileLength(string path)
    { RejectPathLinks(path); return new FileInfo(path).Length; }

    private static void DeleteFile(string path)
    { RejectPathLinks(path); if (File.Exists(path)) File.Delete(path); }

    private static void EnsureDirectory(string path)
    {
        RejectPathLinks(path);
        if (!Directory.Exists(path))
        {
            string parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureDirectory(parent);
            RejectPathLinks(path);
            Directory.CreateDirectory(path);
        }
        RejectPathLinks(path);
    }

    // Check the entire chain, including ancestors of the configured cache and dangling
    // links. Recheck around callbacks/I/O. A hostile process racing filesystem operations
    // still requires OS sandbox ownership; portable managed paths are not an openat jail.
    private static void RejectPathLinks(string path)
    {
        string current = Path.GetFullPath(path);
        while (!string.IsNullOrEmpty(current))
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Symbolic links are not allowed in the client package cache.");
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            current = Path.GetDirectoryName(current);
        }
    }

    private static string Hash(byte[] bytes)
    { using (var sha = SHA256.Create()) return Hex(sha.ComputeHash(bytes)); }
    private static string Hex(byte[] bytes)
    { return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant(); }
    private static void Report(Action<string, string, long, long> report, string phase, string detail, long done, long total)
    { report?.Invoke(phase, detail, done, total); }
}
}

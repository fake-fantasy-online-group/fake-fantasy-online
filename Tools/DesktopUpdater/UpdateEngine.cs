using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.DesktopUpdater
{
public interface IDesktopPlatform
{
    void SetPermissions(string path, bool executable);
    void ValidateTrust(string root, DesktopInstallIdentity identity);
    void StartApplication(string root, DesktopInstallIdentity identity);
}

/// <summary>One same-volume, recoverable directory replacement. Never deletes an old application or user data.</summary>
public sealed class UpdateEngine : IDisposable
{
    public const int MaxEntries = 65534;
    public const long MaxExpandedBytes = 16L * 1024 * 1024 * 1024;
    private readonly DesktopUpdateRequest request;
    private readonly IDesktopPlatform platform;
    private readonly Action<string> checkpoint;
    private readonly Func<string, long> availableBytes;
    private readonly FileStream installLock;
    private readonly DesktopInstallIdentity current;
    private readonly string stage, candidate, backup, displaced, journal;
    private readonly Dictionary<string, string> stagedHashes = new Dictionary<string, string>(StringComparer.Ordinal);
    public string BackupPath { get { return backup; } }
    public UpdateEngine(DesktopUpdateRequest request, IDesktopPlatform platform, Action<string> checkpoint = null, Func<string, long> availableBytes = null)
    {
        request.Validate(); this.request = request; this.platform = platform; this.checkpoint = checkpoint ?? (_ => { });
        this.availableBytes = availableBytes ?? (path => new DriveInfo(Path.GetPathRoot(path)).AvailableFreeSpace);
        string target = request.InstallRoot;
        string lockPath = target + ".update.lock";
        DesktopUpdateFiles.NoLinks(lockPath);
        installLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        try
        {
            current = DesktopInstallIdentity.Read(target, request.Platform);
            if (current.Version != request.CurrentVersion) throw new InvalidDataException("Installed application version changed.");
            string id = Path.GetFileName(request.JobDirectory);
            Guid parsed;
            if (id.Length != 32 || !Guid.TryParseExact(id, "N", out parsed)) throw new InvalidDataException("Invalid update job identifier.");
            stage = target + ".update-stage-" + id;
            candidate = Path.Combine(stage, Path.GetFileName(target));
            backup = target + ".update-backup-" + id;
            displaced = target + ".update-failed-" + id;
            journal = Path.Combine(request.JobDirectory, "transaction.json");
            foreach (string path in new[] { stage, backup, displaced, journal }) DesktopUpdateFiles.NoLinks(path);
        }
        catch { installLock.Dispose(); throw; }
    }
    public void Prepare(CancellationToken cancellation)
    {
        if (Directory.Exists(stage) || Directory.Exists(backup) || File.Exists(journal))
            throw new InvalidDataException("An existing update transaction must be recovered before preparing again.");
        cancellation.ThrowIfCancellationRequested();
        // Keep one handle throughout hash and extraction. A cached success flag is not authority.
        DesktopUpdateFiles.NoLinks(request.PackagePath);
        using (var input = new FileStream(request.PackagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            VerifyPackage(input, cancellation);
            CheckCentralDirectory(input);
            input.Position = 0;
            using (var archive = new ZipArchive(input, ZipArchiveMode.Read, true))
            {
                var entries = ValidateEntries(archive);
                long size = 0; foreach (var entry in entries) size = checked(size + entry.Length);
                long free = availableBytes(request.InstallRoot);
                if (free < checked(size + 64L * 1024 * 1024)) throw new IOException("Insufficient free storage beside the installed application.");
                Directory.CreateDirectory(stage);
                foreach (var entry in entries)
                {
                    cancellation.ThrowIfCancellationRequested();
                    string relative = entry.FullName.TrimEnd('/');
                    string destination = Path.Combine(stage, relative.Replace('/', Path.DirectorySeparatorChar));
                    DesktopUpdateFiles.NoLinks(destination);
                    if (entry.FullName.EndsWith("/", StringComparison.Ordinal)) { Directory.CreateDirectory(destination); continue; }
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    using (var source = entry.Open())
                    using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[65536]; long done = 0;
                        while (true)
                        {
                            cancellation.ThrowIfCancellationRequested(); int count = source.Read(buffer, 0, buffer.Length);
                            if (count == 0) break;
                            if (count > entry.Length - done) throw new InvalidDataException("Archive entry exceeded declared length.");
                            output.Write(buffer, 0, count); done += count;
                        }
                        if (done != entry.Length) throw new InvalidDataException("Archive entry length mismatch.");
                        output.Flush(true);
                    }
                    stagedHashes.Add(destination, FileDigest(destination));
                    platform.SetPermissions(destination, ((entry.ExternalAttributes >> 16) & 73) != 0);
                }
            }
        }
        var next = DesktopInstallIdentity.Read(candidate, request.Platform);
        current.RequireUpdate(next, request.Version);
        if (!File.Exists(Path.Combine(candidate, next.Executable))) throw new InvalidDataException("Package is missing its executable.");
        platform.ValidateTrust(candidate, next);
        DesktopUpdateFiles.WriteNew(journal, BoundedJson.Serialize(new Dictionary<string, object> {
            {"schemaVersion",1}, {"installRoot",request.InstallRoot}, {"backup",backup}, {"stage",stage},
            {"oldVersion",current.Version}, {"newVersion",next.Version}, {"installId",current.InstallId}
        }));
        checkpoint("prepared");
    }
    public void Commit()
    {
        if (!File.Exists(journal) || !Directory.Exists(candidate) || Directory.Exists(backup) || Directory.Exists(displaced))
            throw new InvalidDataException("The update transaction is not prepared.");
        DesktopUpdateFiles.NoLinks(request.InstallRoot); DesktopUpdateFiles.NoLinks(candidate);
        var live = DesktopInstallIdentity.Read(request.InstallRoot, request.Platform);
        if (live.Version != current.Version || live.InstallId != current.InstallId) throw new InvalidDataException("Installed application changed before commit.");
        var next = DesktopInstallIdentity.Read(candidate, request.Platform); current.RequireUpdate(next, request.Version);
        VerifyStagedTree();
        platform.ValidateTrust(candidate, next); // Recheck prepared files immediately before committing.
        bool movedOld = false, movedNew = false;
        try
        {
            Directory.Move(request.InstallRoot, backup); movedOld = true; checkpoint("old-renamed");
            Directory.Move(candidate, request.InstallRoot); movedNew = true; checkpoint("new-renamed");
            platform.ValidateTrust(request.InstallRoot, next);
            DesktopUpdateFiles.WriteNew(Path.Combine(request.JobDirectory, "installed"), "installed\n");
            // No fallible transaction work after a successful launch: the new process may hold files open.
            platform.StartApplication(request.InstallRoot, next);
        }
        catch
        {
            if (movedNew && Directory.Exists(request.InstallRoot)) Directory.Move(request.InstallRoot, displaced);
            if (movedOld && !Directory.Exists(request.InstallRoot) && Directory.Exists(backup)) Directory.Move(backup, request.InstallRoot);
            throw;
        }
    }
    private void VerifyStagedTree()
    {
        int files = 0;
        var pending = new Stack<string>(); pending.Push(stage);
        while (pending.Count != 0)
        {
            string directory = pending.Pop(); DesktopUpdateFiles.NoLinks(directory);
            foreach (string child in Directory.GetDirectories(directory)) { DesktopUpdateFiles.NoLinks(child); pending.Push(child); }
            foreach (string path in Directory.GetFiles(directory))
            {
                DesktopUpdateFiles.NoLinks(path); string digest;
                if (!stagedHashes.TryGetValue(path, out digest) || FileDigest(path) != digest)
                    throw new InvalidDataException("Prepared package changed before installation.");
                files++;
            }
        }
        if (files != stagedHashes.Count) throw new InvalidDataException("Prepared package is incomplete.");
    }
    private static string FileDigest(string path)
    {
        DesktopUpdateFiles.NoLinks(path);
        using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(input));
    }
    private void VerifyPackage(Stream input, CancellationToken cancellation)
    {
        if (input.Length != request.SizeBytes) throw new InvalidDataException("Desktop package length changed.");
        using (var sha = SHA256.Create())
        {
            var buffer = new byte[65536]; int count;
            while ((count = input.Read(buffer, 0, buffer.Length)) != 0)
            { cancellation.ThrowIfCancellationRequested(); sha.TransformBlock(buffer, 0, count, buffer, 0); }
            sha.TransformFinalBlock(new byte[0], 0, 0);
            if (!BitConverter.ToString(sha.Hash).Replace("-", "").Equals(request.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Desktop package SHA-256 changed.");
        }
    }
    private static void CheckCentralDirectory(Stream input)
    {
        // Bound ZipArchive allocations before it reads any entries. ZIP64/multidisk are deliberately unsupported.
        int length = (int)Math.Min(input.Length, 65557); var tail = new byte[length];
        input.Position = input.Length - length; int read = 0;
        while (read < length) { int n = input.Read(tail, read, length - read); if (n == 0) throw new InvalidDataException("Truncated ZIP."); read += n; }
        for (int i = length - 22; i >= 0; i--)
        {
            if (BitConverter.ToUInt32(tail, i) != 0x06054b50) continue;
            if (i + 22 + BitConverter.ToUInt16(tail, i + 20) != length) continue;
            uint directorySize = BitConverter.ToUInt32(tail, i + 12), offset = BitConverter.ToUInt32(tail, i + 16);
            ushort count = BitConverter.ToUInt16(tail, i + 10);
            if (BitConverter.ToUInt16(tail, i + 4) != 0 || BitConverter.ToUInt16(tail, i + 6) != 0 ||
                BitConverter.ToUInt16(tail, i + 8) != count || count == 0 || count > MaxEntries ||
                directorySize > 64 * 1024 * 1024 || (long)offset + directorySize != input.Length - length + i)
                throw new InvalidDataException("ZIP64, multidisk, unbounded or noncanonical ZIP is not supported.");
            return;
        }
        throw new InvalidDataException("Missing canonical ZIP directory.");
    }
    private List<ZipArchiveEntry> ValidateEntries(ZipArchive archive)
    {
        if (archive.Entries.Count == 0 || archive.Entries.Count > MaxEntries) throw new InvalidDataException("Archive entry limit exceeded.");
        var names = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var canonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var entries = new List<ZipArchiveEntry>(); long expanded = 0;
        string root = Path.GetFileName(request.InstallRoot);
        foreach (var entry in archive.Entries)
        {
            string path = entry.FullName.TrimEnd('/'); bool directory = entry.FullName.EndsWith("/", StringComparison.Ordinal);
            DesktopUpdateFiles.Relative(path);
            if (path.Contains("/__MACOSX/") || path.EndsWith("/.DS_Store", StringComparison.Ordinal)) throw new InvalidDataException("Archive contains packaging metadata.");
            if (path != root && !path.StartsWith(root + "/", StringComparison.Ordinal)) throw new InvalidDataException("Archive has an unexpected application root.");
            int type = (entry.ExternalAttributes >> 16) & 0xf000;
            if ((type != 0 && type != 0x8000 && type != 0x4000) || (entry.ExternalAttributes & 1024) != 0 ||
                (type == 0x4000 && !directory) || (type == 0x8000 && directory))
                throw new InvalidDataException("Links and special files are forbidden in update archives.");
            if (names.ContainsKey(path) || (directory && entry.Length != 0) || entry.Length < 0 || entry.Length > uint.MaxValue)
                throw new InvalidDataException("Duplicate archive path or invalid size.");
            names.Add(path, directory);
            string part = path;
            while (!string.IsNullOrEmpty(part))
            {
                string previous;
                if (canonical.TryGetValue(part, out previous) && previous != part) throw new InvalidDataException("Archive has case-colliding paths.");
                canonical[part] = part;
                int slash = part.LastIndexOf('/'); part = slash < 0 ? null : part.Substring(0, slash);
            }
            expanded = checked(expanded + entry.Length);
            if (expanded > MaxExpandedBytes) throw new InvalidDataException("Archive expanded size limit exceeded.");
            entries.Add(entry);
        }
        foreach (string path in names.Keys)
        {
            for (int slash = path.LastIndexOf('/'); slash >= 0; slash = path.LastIndexOf('/', slash - 1))
            {
                bool directory;
                if (names.TryGetValue(path.Substring(0, slash), out directory) && !directory) throw new InvalidDataException("Archive file/directory conflict.");
                if (slash == 0) break;
            }
        }
        return entries;
    }
    public static void Recover(DesktopUpdateRequest request)
    {
        request.Validate(); string id = Path.GetFileName(request.JobDirectory); Guid parsed;
        if (id.Length != 32 || !Guid.TryParseExact(id, "N", out parsed)) throw new InvalidDataException("Invalid recovery job.");
        string backup = request.InstallRoot + ".update-backup-" + id;
        string displaced = request.InstallRoot + ".update-failed-" + id;
        string journal = Path.Combine(request.JobDirectory, "transaction.json");
        var record = DesktopUpdateFiles.ReadObject(journal);
        if (DesktopUpdateFiles.Text(record, "installRoot") != request.InstallRoot || DesktopUpdateFiles.Text(record, "backup") != backup ||
            DesktopUpdateFiles.Text(record, "oldVersion") != request.CurrentVersion || DesktopUpdateFiles.Text(record, "newVersion") != request.Version)
            throw new InvalidDataException("Recovery journal does not match the request.");
        foreach (string path in new[] { request.InstallRoot, backup, displaced }) DesktopUpdateFiles.NoLinks(path);
        string lockPath = request.InstallRoot + ".update.lock"; DesktopUpdateFiles.NoLinks(lockPath);
        using (var guard = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            if (!Directory.Exists(backup)) throw new InvalidDataException("No retained application backup is available.");
            var old = DesktopInstallIdentity.Read(backup, request.Platform);
            if (old.Version != request.CurrentVersion || old.InstallId != DesktopUpdateFiles.Text(record, "installId"))
                throw new InvalidDataException("Retained backup identity does not match the journal.");
            if (Directory.Exists(request.InstallRoot))
            {
                if (Directory.Exists(displaced)) throw new IOException("A prior failed application is retained; move it aside before recovery.");
                var next = DesktopInstallIdentity.Read(request.InstallRoot, request.Platform); old.RequireUpdate(next, request.Version);
                Directory.Move(request.InstallRoot, displaced);
            }
            Directory.Move(backup, request.InstallRoot);
        }
    }
    public void Dispose() { installLock.Dispose(); }
}
}

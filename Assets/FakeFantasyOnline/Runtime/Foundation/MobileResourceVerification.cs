using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace FakeFantasyOnline.Foundation
{
/// <summary>
/// Private, best-effort per-file receipt. Detects ordinary changes, not malicious
/// same-size/same-timestamp edits or forged receipts. The manifest remains authoritative.
/// </summary>
public sealed class MobileResourceVerification
{
    private const string Header = "FFO-VERIFIED-2", LegacyHeader = "FFO-VERIFIED-1";
    private const int MaxReceiptBytes = 24 * 1024 * 1024;
    private readonly MobileResourceManifest manifest;
    private readonly string root, receiptPath;
    private readonly object metadataLock = new object();
    private readonly long[] stamps;
    private readonly bool[] verified;
    private readonly Stopwatch checkpointClock = Stopwatch.StartNew();
    private bool dirty;
    private int pending;
    public long BytesToHash { get; private set; }
    public int CachedFiles { get; private set; }

    public MobileResourceVerification(MobileResourceManifest manifest, string root, string receiptPath, bool useCachedReceipt = true)
    {
        this.manifest = manifest; this.root = root; this.receiptPath = receiptPath;
        stamps = new long[manifest.Entries.Count]; verified = new bool[stamps.Length];
        var previous = useCachedReceipt ? ReadReceipt() : null;
        for (int i = 0; i < stamps.Length; i++)
        {
            var entry = manifest.Entries[i];
            stamps[i] = Stamp(PathFor(i), entry.Length);
            long previousStamp;
            verified[i] = previous != null && previous.TryGetValue(Key(entry), out previousStamp) &&
                stamps[i] >= 0 && stamps[i] == previousStamp;
            if (verified[i]) CachedFiles++;
            else BytesToHash = checked(BytesToHash + entry.Length);
        }
    }

    public bool NeedsHash(int index) { lock (metadataLock) return !verified[index]; }

    public bool Verify(int index, Action<long> progress = null)
    {
        var entry = manifest.Entries[index]; string path = PathFor(index);
        long before = Stamp(path, entry.Length);
        lock (metadataLock)
        {
            if (verified[index] && before >= 0 && before == stamps[index]) return true;
            // A failed or cancelled real check must revoke the old receipt row,
            // including a repair which detected same-size/same-mtime corruption.
            verified[index] = false; dirty = true;
        }
        if (before < 0 || !MobileResourceManifest.Verify(path, entry, progress)) return false;
        long after = Stamp(path, entry.Length);
        if (before != after) return false;
        Record(index, after);
        return true;
    }

    public void CommitFile(int index, string temporary, Action<long> progress = null)
    { CommitFile(index, temporary, null, progress); }

    public void CommitFile(int index, string temporary, MobileResourceVerifiedFile proof, Action<long> progress = null)
    {
        var entry = manifest.Entries[index]; long before = Stamp(temporary, entry.Length);
        lock (metadataLock) { verified[index] = false; dirty = true; }
        if (before < 0 || (proof == null ? !MobileResourceManifest.Verify(temporary, entry, progress) :
            !proof.Matches(temporary, entry.Length, entry.Sha256, before)) || before != Stamp(temporary, entry.Length))
            throw new InvalidDataException("Mobile resource length/hash mismatch or file changed: " + entry.Destination);
        string destination = PathFor(index);
        if (File.Exists(destination)) File.Delete(destination);
        File.Move(temporary, destination);
        long after = Stamp(destination, entry.Length);
        if (after != before) throw new IOException("Resource changed while installing: " + entry.Destination);
        Record(index, after);
    }

    // Reuse trusted logical content without hashing it again just because a manifest
    // changed. Copy from an open read-only handle; reject size/stamp changes before
    // accepting the copied bytes. Unknown/changed sources still pass full Verify.
    internal MobileResourceVerifiedFile CopyVerified(int index, string temporary, CancellationToken cancellation,
        Action<long> progress)
    {
        if (!Verify(index, bytes => cancellation.ThrowIfCancellationRequested())) return null;
        var entry = manifest.Entries[index]; string source = PathFor(index); long before;
        lock (metadataLock) before = stamps[index];
        using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            if (input.Length != entry.Length || Stamp(source, entry.Length) != before)
                throw new IOException("Reusable resource changed before copying.");
            MobileResourceHttpTransport.CopyBounded(input, output, entry.Length, cancellation, progress);
            if (output.Length != entry.Length || input.Length != entry.Length || Stamp(source, entry.Length) != before)
                throw new IOException("Reusable resource changed while copying.");
            output.Flush(true);
        }
        return new MobileResourceVerifiedFile(temporary, entry.Length, entry.Sha256, Stamp(temporary, entry.Length));
    }

    private void Record(int index, long stamp)
    {
        lock (metadataLock)
        {
            stamps[index] = stamp; verified[index] = true; dirty = true; pending++;
            // Bound write amplification while retaining completed large files promptly.
            if (pending >= 32 || checkpointClock.ElapsedMilliseconds >= 1000) CheckpointReceipt();
        }
    }

    public void CheckpointReceipt()
    {
        // Protect the snapshot, shared temporary receipt and dirty bookkeeping
        // together. Payload hashing/copying stays outside this metadata lock.
        lock (metadataLock)
        {
            if (!dirty) return;
            var body = new StringBuilder(Header).Append('\n');
            for (int i = 0; i < stamps.Length; i++)
                if (verified[i] && stamps[i] >= 0)
                    body.Append(Key(manifest.Entries[i])).Append('\t')
                        .Append(stamps[i].ToString(CultureInfo.InvariantCulture)).Append('\n');
            string content = body.ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(content + Digest(content) + "\n");
            string temporary = receiptPath + ".part";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(receiptPath)) File.Replace(temporary, receiptPath, null);
            else File.Move(temporary, receiptPath);
            dirty = false; pending = 0; checkpointClock.Restart();
        }
    }

    public void CommitReceipt()
    {
        lock (metadataLock)
        {
            for (int i = 0; i < stamps.Length; i++)
                if (!verified[i] || stamps[i] < 0 || stamps[i] != Stamp(PathFor(i), manifest.Entries[i].Length))
                    throw new InvalidDataException("Resource changed before activation: " + manifest.Entries[i].Destination);
            CheckpointReceipt(); // Unchanged warm starts do not rewrite/fsync a valid receipt.
        }
    }

    private static string Key(MobileResourceManifest.Entry entry)
    { return entry.Destination + "\t" + entry.Sha256 + "\t" + entry.Length.ToString(CultureInfo.InvariantCulture); }
    private string PathFor(int index) { return MobileResourceManifest.FilePath(root, manifest.Entries[index].Destination); }
    private static long Stamp(string path, long length)
    { var f = new FileInfo(path); return f.Exists && f.Length == length ? f.LastWriteTimeUtc.Ticks : -1; }

    private Dictionary<string, long> ReadReceipt()
    {
        try
        {
            if (!File.Exists(receiptPath) || new FileInfo(receiptPath).Length > MaxReceiptBytes) return null;
            string text = File.ReadAllText(receiptPath); string[] lines = text.Split('\n');
            if (lines.Length < 3 || lines[lines.Length - 1] != "") return null;
            int footer = text.LastIndexOf('\n', text.Length - 2) + 1;
            if (Digest(text.Substring(0, footer)) != lines[lines.Length - 2]) return null;
            var result = new Dictionary<string, long>(StringComparer.Ordinal);
            if (lines[0] == LegacyHeader)
            {
                if (lines.Length != manifest.Entries.Count + 4 || lines[1] != manifest.Version) return null;
                for (int i = 0; i < manifest.Entries.Count; i++)
                {
                    var fields = lines[i + 2].Split('\t'); long length, ticks; var entry = manifest.Entries[i];
                    if (fields.Length != 3 || fields[0] != entry.Sha256 ||
                        !long.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out length) || length != entry.Length ||
                        !ValidTicks(fields[2], out ticks)) return null;
                    result.Add(Key(entry), ticks);
                }
                dirty = true; // Migrate the matching old receipt without rehashing payload.
            }
            else if (lines[0] == Header)
            {
                if (lines.Length > 100003) return null;
                for (int i = 1; i < lines.Length - 2; i++)
                {
                    var fields = lines[i].Split('\t'); long length, ticks;
                    if (fields.Length != 4 || fields[1].Length != 64 ||
                        !long.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out length) || length < 0 ||
                        !ValidTicks(fields[3], out ticks)) return null;
                    MobileResourceManifest.ValidateRelativePath(fields[0]);
                    string key = fields[0] + "\t" + fields[1] + "\t" + fields[2];
                    if (result.ContainsKey(key)) return null;
                    result.Add(key, ticks);
                }
            }
            else return null;
            return result;
        }
        catch (InvalidDataException) { return null; }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }
    private static bool ValidTicks(string value, out long ticks)
    { return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out ticks) && ticks >= 0 && ticks <= DateTime.MaxValue.Ticks; }
    private static string Digest(string text)
    { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant(); }
}
}

using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FakeFantasyOnline.Foundation
{
/// <summary>
/// Private, best-effort verification receipt. It detects ordinary file changes, not
/// malicious edits that preserve timestamps or forge receipts. The APK manifest is
/// always the sole authority for paths, lengths and hashes.
/// </summary>
public sealed class MobileResourceVerification
{
    private const string Header = "FFO-VERIFIED-1";
    private const int MaxReceiptBytes = 24 * 1024 * 1024;
    private readonly MobileResourceManifest manifest;
    private readonly string root, receiptPath;
    private readonly long[] stamps;
    private readonly bool[] verified;
    public long BytesToHash { get; private set; }
    public int CachedFiles { get; private set; }

    public MobileResourceVerification(MobileResourceManifest manifest, string root, string receiptPath, bool useCachedReceipt = true)
    {
        this.manifest = manifest; this.root = root; this.receiptPath = receiptPath;
        stamps = new long[manifest.Entries.Count];
        verified = new bool[stamps.Length];
        long[] previous = useCachedReceipt ? ReadReceipt() : null;
        for (int i = 0; i < stamps.Length; i++)
        {
            stamps[i] = Stamp(PathFor(i), manifest.Entries[i].Length);
            verified[i] = previous != null && stamps[i] >= 0 && stamps[i] == previous[i];
            if (verified[i]) CachedFiles++;
            else BytesToHash = checked(BytesToHash + manifest.Entries[i].Length);
        }
    }

    public bool NeedsHash(int index) { return !verified[index]; }

    public bool Verify(int index, Action<long> progress = null)
    {
        var entry = manifest.Entries[index];
        string path = PathFor(index);
        long before = Stamp(path, entry.Length);
        if (verified[index] && before >= 0 && before == stamps[index]) return true;
        verified[index] = false;
        if (before < 0 || !MobileResourceManifest.Verify(path, entry, progress)) return false;
        long after = Stamp(path, entry.Length);
        if (before != after) return false; // Never cache a file changed during hashing.
        stamps[index] = after;
        verified[index] = true;
        return true;
    }

    public void CommitFile(int index, string temporary, Action<long> progress = null)
    {
        var entry = manifest.Entries[index];
        long before = Stamp(temporary, entry.Length);
        verified[index] = false;
        if (before < 0 || !MobileResourceManifest.Verify(temporary, entry, progress) ||
            before != Stamp(temporary, entry.Length))
            throw new InvalidDataException("Mobile resource length/hash mismatch: " + entry.Destination);
        string destination = PathFor(index);
        if (File.Exists(destination)) File.Delete(destination);
        File.Move(temporary, destination);
        stamps[index] = Stamp(destination, entry.Length);
        if (stamps[index] != before) throw new IOException("Resource changed while installing: " + entry.Destination);
        verified[index] = true;
    }

    public void CommitReceipt()
    {
        var body = new StringBuilder(Header).Append('\n').Append(manifest.Version).Append('\n');
        for (int i = 0; i < stamps.Length; i++)
        {
            var entry = manifest.Entries[i];
            if (!verified[i] || stamps[i] < 0 || stamps[i] != Stamp(PathFor(i), entry.Length))
                throw new InvalidDataException("Resource changed before activation: " + entry.Destination);
            body.Append(entry.Sha256).Append('\t').Append(entry.Length.ToString(CultureInfo.InvariantCulture))
                .Append('\t').Append(stamps[i].ToString(CultureInfo.InvariantCulture)).Append('\n');
        }
        string content = body.ToString();
        byte[] bytes = Encoding.UTF8.GetBytes(content + Digest(content) + "\n");
        string temporary = receiptPath + ".part";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }
        // Never delete the old receipt first. A crash exposes either the old complete
        // receipt or the new complete receipt, not a partially written success marker.
        if (File.Exists(receiptPath)) File.Replace(temporary, receiptPath, null);
        else File.Move(temporary, receiptPath);
    }

    private string PathFor(int index) { return MobileResourceManifest.FilePath(root, manifest.Entries[index].Destination); }
    private static long Stamp(string path, long expectedLength)
    {
        var file = new FileInfo(path);
        return file.Exists && file.Length == expectedLength ? file.LastWriteTimeUtc.Ticks : -1;
    }

    private long[] ReadReceipt()
    {
        try
        {
            if (!File.Exists(receiptPath) || new FileInfo(receiptPath).Length > MaxReceiptBytes) return null;
            string text = File.ReadAllText(receiptPath);
            string[] lines = text.Split('\n');
            if (lines.Length != manifest.Entries.Count + 4 || lines[0] != Header ||
                lines[1] != manifest.Version || lines[lines.Length - 1] != "") return null;
            int footer = text.LastIndexOf('\n', text.Length - 2) + 1;
            if (Digest(text.Substring(0, footer)) != lines[lines.Length - 2]) return null;
            var result = new long[manifest.Entries.Count];
            for (int i = 0; i < result.Length; i++)
            {
                var fields = lines[i + 2].Split('\t');
                var entry = manifest.Entries[i];
                long length, ticks;
                if (fields.Length != 3 || fields[0] != entry.Sha256 ||
                    !long.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out length) || length != entry.Length ||
                    !long.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out ticks) || ticks < 0 || ticks > DateTime.MaxValue.Ticks)
                    return null;
                result[i] = ticks;
            }
            return result;
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    private static string Digest(string text)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
    }
}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace FakeFantasyOnline.Foundation
{
/// <summary>
/// Bounded, forward-only reader for resource TARs. It never writes to the filesystem.
/// Only regular files and directories are accepted; a file's stream is valid only during
/// its callback. Unread file bytes are drained without hashing. The input is left open.
/// </summary>
public static class MobileResourceTarReader
{
    public const long MaxArchiveBytes = 64L * 1024 * 1024 * 1024;
    public const int MaxEntries = 100000;
    public const int MaxPathBytes = 4096;
    public const int MaxPathDepth = 64;
    private const int BlockBytes = 512;
    private const int MaxMetadataBytes = 64 * 1024;
    private const int MaxTotalMetadataBytes = 8 * 1024 * 1024;
    private const int MaxTrackedPathBytes = 16 * 1024 * 1024;
    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

    /// <param name="archiveBytes">Exact uncompressed TAR length, including end padding.</param>
    public static void Read(Stream input, long archiveBytes, CancellationToken cancellationToken,
        Action<string, long, Stream> onFile)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (onFile == null) throw new ArgumentNullException(nameof(onFile));
        if (!input.CanRead) throw new ArgumentException("TAR input must be readable.", nameof(input));
        if (archiveBytes < 2 * BlockBytes || archiveBytes > MaxArchiveBytes || archiveBytes % BlockBytes != 0)
            throw new InvalidDataException("Invalid or excessive TAR archive length.");
        new Reader(input, archiveBytes, cancellationToken, onFile).Run();
    }

    private sealed class PathNode
    {
        public string Spelling;
        public bool Directory;
        public bool Explicit;
    }

    private sealed class Reader
    {
        private readonly Stream input;
        private readonly CancellationToken cancellationToken;
        private readonly Action<string, long, Stream> onFile;
        private readonly byte[] header = new byte[BlockBytes];
        private readonly byte[] scratch = new byte[32 * 1024];
        private readonly Dictionary<string, PathNode> paths = new Dictionary<string, PathNode>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> global = new Dictionary<string, string>(StringComparer.Ordinal);
        private Dictionary<string, string> local;
        private string longName;
        private long remaining;
        private int entries;
        private int metadataBytes;
        private int trackedPathBytes;

        public Reader(Stream input, long length, CancellationToken token, Action<string, long, Stream> onFile)
        { this.input = input; remaining = length; cancellationToken = token; this.onFile = onFile; }

        public void Run()
        {
            while (true)
            {
                Exact(header, 0, header.Length);
                if (AllZero(header, 0, header.Length))
                {
                    if (local != null || longName != null) throw Invalid("Dangling TAR metadata.");
                    Exact(header, 0, header.Length);
                    if (!AllZero(header, 0, header.Length)) throw Invalid("Missing TAR end marker.");
                    ZeroPadding(remaining);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (input.Read(scratch, 0, 1) != 0) throw Invalid("TAR exceeds its declared archive length.");
                    cancellationToken.ThrowIfCancellationRequested();
                    return;
                }
                if (++entries > MaxEntries) throw Invalid("Excessive TAR entries.");
                ValidateChecksum(header);
                bool ustar = Matches(header, 257, "ustar\0") && Matches(header, 263, "00");
                bool gnu = Matches(header, 257, "ustar ") && header[263] == (byte)' ' && header[264] == 0;
                if (!ustar && !gnu) throw Invalid("Unsupported TAR header format.");
                string name = Text(header, 0, 100);
                string prefix = ustar ? Text(header, 345, 155) : "";
                if (prefix.Length != 0) name = prefix + "/" + name;
                long size = Octal(header, 124, 12);
                if (Text(header, 157, 100).Length != 0) throw Invalid("TAR links are not supported.");
                byte type = header[156];
                if (type == (byte)'x' || type == (byte)'g' || type == (byte)'L')
                {
                    // Standard metadata pseudo-names are not extraction destinations.
                    if (name != "././@PaxHeader" && name != "././@LongLink") ValidatePath(name, false);
                    EnsurePayload(size);
                    if (size > MaxMetadataBytes || size > MaxTotalMetadataBytes - metadataBytes)
                        throw Invalid("Excessive TAR metadata.");
                    metadataBytes += (int)size;
                    var payload = new byte[(int)size];
                    Exact(payload, 0, payload.Length);
                    Padding(size);
                    if (type == (byte)'L')
                    {
                        if (longName != null || payload.Length < 2 || payload.Length > MaxPathBytes + 1 || payload[payload.Length - 1] != 0)
                            throw Invalid("Invalid GNU TAR long name.");
                        longName = Decode(payload, 0, payload.Length - 1);
                        ValidatePath(longName, longName.EndsWith("/", StringComparison.Ordinal));
                    }
                    else
                    {
                        Dictionary<string, string> values = ParsePax(payload);
                        if (type == (byte)'x')
                        {
                            if (local != null) throw Invalid("Repeated per-file TAR metadata.");
                            local = values;
                        }
                        else
                        {
                            foreach (var pair in values)
                            {
                                if (pair.Value.Length == 0) global.Remove(pair.Key);
                                else global[pair.Key] = pair.Value;
                            }
                        }
                    }
                    continue;
                }
                bool directory = type == (byte)'5';
                if (type != 0 && type != (byte)'0' && !directory) throw Invalid("Unsupported TAR entry type.");
                string paxPath = Value("path");
                if (longName != null && paxPath != null) throw Invalid("Conflicting TAR path metadata.");
                if (paxPath != null || longName != null) ValidateFallbackPath(name);
                string path = ValidatePath(paxPath ?? longName ?? name, directory);
                string paxSize = Value("size");
                if (paxSize != null) size = Decimal(paxSize);
                if (directory && size != 0) throw Invalid("TAR directory contains data.");
                EnsurePayload(size);
                RegisterPath(path, directory);
                local = null;
                longName = null;
                if (!directory)
                {
                    var bounded = new EntryStream(this, size);
                    try { onFile(path, size, bounded); }
                    finally { bounded.Invalidate(); }
                    Drain(bounded.Remaining);
                }
                Padding(size);
            }
        }

        private string Value(string key)
        {
            string value;
            if (local != null && local.TryGetValue(key, out value)) return value.Length == 0 ? null : value;
            return global.TryGetValue(key, out value) ? value : null;
        }

        private void RegisterPath(string path, bool directory)
        {
            int end = -1;
            while (true)
            {
                end = path.IndexOf('/', end + 1);
                bool final = end < 0;
                string part = final ? path : path.Substring(0, end);
                bool isDirectory = !final || directory;
                PathNode node;
                if (paths.TryGetValue(part, out node))
                {
                    if (!string.Equals(part, node.Spelling, StringComparison.Ordinal)) throw Invalid("Case-colliding TAR paths.");
                    if (node.Directory != isDirectory || (final && node.Explicit)) throw Invalid("Duplicate or conflicting TAR paths.");
                    if (final) node.Explicit = true;
                }
                else
                {
                    int pathBytes = Utf8.GetByteCount(part);
                    if (paths.Count >= MaxEntries || pathBytes > MaxTrackedPathBytes - trackedPathBytes)
                        throw Invalid("Excessive TAR path inventory.");
                    trackedPathBytes += pathBytes;
                    paths.Add(part, new PathNode { Spelling = part, Directory = isDirectory, Explicit = final });
                }
                if (final) return;
            }
        }

        private void EnsurePayload(long size)
        {
            if (size < 0 || size > MaxArchiveBytes || remaining < 2 * BlockBytes ||
                size > remaining - 2 * BlockBytes || PadBytes(size) > remaining - 2 * BlockBytes - size)
                throw Invalid("TAR entry exceeds the archive boundary.");
        }

        internal int ReadPayload(byte[] buffer, int offset, int count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (count > remaining) throw Invalid("TAR archive boundary exceeded.");
            int read = input.Read(buffer, offset, count);
            if (read <= 0 || read > count) throw Invalid("Truncated TAR archive.");
            remaining -= read;
            cancellationToken.ThrowIfCancellationRequested();
            return read;
        }

        private void Exact(byte[] buffer, int offset, int count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            while (count > 0)
            {
                int read = ReadPayload(buffer, offset, count);
                offset += read;
                count -= read;
            }
        }

        private void Drain(long bytes)
        {
            while (bytes > 0) bytes -= ReadPayload(scratch, 0, (int)Math.Min(bytes, scratch.Length));
            cancellationToken.ThrowIfCancellationRequested();
        }

        private void Padding(long size) { ZeroPadding(PadBytes(size)); }

        private void ZeroPadding(long bytes)
        {
            while (bytes > 0)
            {
                int read = ReadPayload(scratch, 0, (int)Math.Min(bytes, scratch.Length));
                if (!AllZero(scratch, 0, read)) throw Invalid("Nonzero TAR padding or data after the end marker.");
                bytes -= read;
            }
        }
    }

    private sealed class EntryStream : Stream
    {
        private readonly Reader reader;
        private readonly long length;
        private bool active = true;
        private readonly byte[] single = new byte[1];
        internal long Remaining { get; private set; }
        public EntryStream(Reader reader, long length) { this.reader = reader; this.length = length; Remaining = length; }
        internal void Invalidate() { active = false; }
        public override bool CanRead { get { return active; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { CheckActive(); return length; } }
        public override long Position { get { CheckActive(); return length - Remaining; } set { throw new NotSupportedException(); } }
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckActive();
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset > buffer.Length - count) throw new ArgumentOutOfRangeException();
            if (count == 0 || Remaining == 0) return 0;
            int read = reader.ReadPayload(buffer, offset, (int)Math.Min(Remaining, count));
            Remaining -= read;
            return read;
        }
        public override int ReadByte() { return Read(single, 0, 1) == 0 ? -1 : single[0]; }
        public override void Flush() { CheckActive(); }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        protected override void Dispose(bool disposing) { Invalidate(); base.Dispose(disposing); }
        private void CheckActive() { if (!active) throw new ObjectDisposedException(nameof(EntryStream)); }
    }

    private static Dictionary<string, string> ParsePax(byte[] payload)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        int offset = 0;
        while (offset < payload.Length)
        {
            int start = offset, length = 0, digits = 0;
            while (offset < payload.Length && payload[offset] >= (byte)'0' && payload[offset] <= (byte)'9')
            {
                if (++digits > 6 || (digits == 1 && payload[offset] == (byte)'0')) throw Invalid("Invalid PAX record length.");
                length = length * 10 + payload[offset++] - (byte)'0';
            }
            if (digits == 0 || offset >= payload.Length || payload[offset++] != (byte)' ' ||
                length > payload.Length - start || length <= offset - start + 2)
                throw Invalid("Invalid PAX record framing.");
            int end = start + length;
            if (payload[end - 1] != (byte)'\n') throw Invalid("Unterminated PAX record.");
            int equals = offset;
            while (equals < end - 1 && payload[equals] != (byte)'=') equals++;
            if (equals == offset || equals >= end - 1) throw Invalid("Invalid PAX key/value.");
            string key = Decode(payload, offset, equals - offset);
            string value = Decode(payload, equals + 1, end - equals - 2);
            if (values.ContainsKey(key)) throw Invalid("Duplicate PAX metadata key.");
            ValidatePax(key, value);
            values.Add(key, value);
            offset = end;
        }
        return values;
    }

    private static void ValidatePax(string key, string value)
    {
        foreach (char c in value) if (char.IsControl(c)) throw Invalid("Control character in PAX metadata.");
        switch (key)
        {
            case "path":
                if (value.Length != 0) ValidatePath(value, value.EndsWith("/", StringComparison.Ordinal));
                return;
            case "size":
            case "uid":
            case "gid":
                if (value.Length != 0) Decimal(value);
                return;
            case "mtime":
            case "atime":
            case "ctime":
                if (value.Length != 0)
                {
                    int i = value[0] == '-' ? 1 : 0;
                    bool digit = false, point = false;
                    for (; i < value.Length; i++)
                    {
                        char c = value[i];
                        if (c >= '0' && c <= '9') { digit = true; continue; }
                        if (c == '.' && digit && !point && i < value.Length - 1) { point = true; continue; }
                        throw Invalid("Invalid PAX timestamp.");
                    }
                    if (!digit || value.Length > 64) throw Invalid("Invalid PAX timestamp.");
                }
                return;
            case "uname":
            case "gname":
            case "comment":
                return;
            default:
                // Sparse maps, links, alternative encodings and vendor type overrides are
                // intentionally unsupported, even when their fields could be ignored.
                throw Invalid("Unsupported PAX metadata key.");
        }
    }

    private static void ValidateFallbackPath(string path)
    {
        // PAX producers store an ASCII replacement/truncated name in the base header.
        // It is not the selected destination, so '?' and a truncated final component
        // are legitimate here. Still reject absolute paths and traversal in that header.
        if (string.IsNullOrEmpty(path) || path[0] == '/') throw Invalid("Unsafe TAR fallback path.");
        foreach (char c in path)
            if (char.IsControl(c) || c == '\\' || c == ':') throw Invalid("Unsafe TAR fallback path.");
        string[] parts = path.Split('/');
        foreach (string part in parts)
            if (part == ".." || part == ".") throw Invalid("Unsafe TAR fallback path.");
    }

    private static string ValidatePath(string path, bool directory)
    {
        if (string.IsNullOrEmpty(path) || Utf8.GetByteCount(path) > MaxPathBytes || path[0] == '/')
            throw Invalid("Unsafe TAR path.");
        foreach (char c in path)
            if (char.IsControl(c) || c == '\\' || c == ':' || c == '?' || c == '#' || c == '%' || c == '*' || c == '"' || c == '<' || c == '>' || c == '|')
                throw Invalid("Unsafe TAR path character.");
        if (directory && path.EndsWith("/", StringComparison.Ordinal)) path = path.Substring(0, path.Length - 1);
        string[] parts = path.Split('/');
        if (parts.Length > MaxPathDepth) throw Invalid("Excessive TAR path depth.");
        foreach (string part in parts)
        {
            if (part.Length == 0 || part == "." || part == ".." || part.EndsWith(".", StringComparison.Ordinal) || part.EndsWith(" ", StringComparison.Ordinal))
                throw Invalid("Unsafe TAR path segment.");
            string stem = part.Split('.')[0];
            if (stem.Equals("CON", StringComparison.OrdinalIgnoreCase) || stem.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
                stem.Equals("AUX", StringComparison.OrdinalIgnoreCase) || stem.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
                (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase) || stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) && stem[3] >= '1' && stem[3] <= '9'))
                throw Invalid("Reserved TAR path segment.");
        }
        return path;
    }

    private static void ValidateChecksum(byte[] block)
    {
        long expected = Octal(block, 148, 8), actual = 0;
        for (int i = 0; i < block.Length; i++) actual += i >= 148 && i < 156 ? (byte)' ' : block[i];
        if (expected != actual) throw Invalid("Invalid TAR header checksum.");
    }

    private static long Octal(byte[] data, int offset, int length)
    {
        long value = 0;
        int end = offset + length;
        while (offset < end && data[offset] == (byte)' ') offset++;
        bool digits = false;
        while (offset < end && data[offset] >= (byte)'0' && data[offset] <= (byte)'7')
        {
            digits = true;
            if (value > (long.MaxValue - 7) / 8) throw Invalid("TAR numeric overflow.");
            value = value * 8 + data[offset++] - (byte)'0';
        }
        while (offset < end && (data[offset] == 0 || data[offset] == (byte)' ')) offset++;
        if (!digits || offset != end) throw Invalid("Invalid TAR numeric field.");
        return value;
    }

    private static long Decimal(string text)
    {
        if (text.Length == 0 || text.Length > 20) throw Invalid("Invalid PAX numeric field.");
        long value = 0;
        foreach (char c in text)
        {
            if (c < '0' || c > '9' || value > (long.MaxValue - (c - '0')) / 10) throw Invalid("Invalid PAX numeric field.");
            value = value * 10 + c - '0';
        }
        return value;
    }

    private static string Text(byte[] bytes, int offset, int length)
    {
        int count = 0;
        while (count < length && bytes[offset + count] != 0) count++;
        if (!AllZero(bytes, offset + count, length - count)) throw Invalid("Nonzero bytes after TAR string terminator.");
        return Decode(bytes, offset, count);
    }

    private static string Decode(byte[] bytes, int offset, int count)
    {
        try { return Utf8.GetString(bytes, offset, count); }
        catch (DecoderFallbackException) { throw Invalid("Invalid UTF-8 in TAR metadata."); }
    }
    private static bool Matches(byte[] bytes, int offset, string value)
    {
        for (int i = 0; i < value.Length; i++) if (bytes[offset + i] != (byte)value[i]) return false;
        return true;
    }
    private static bool AllZero(byte[] bytes, int offset, int length)
    {
        for (int i = 0; i < length; i++) if (bytes[offset + i] != 0) return false;
        return true;
    }
    private static long PadBytes(long size) { return (BlockBytes - size % BlockBytes) % BlockBytes; }
    private static InvalidDataException Invalid(string message) { return new InvalidDataException(message); }
}
}

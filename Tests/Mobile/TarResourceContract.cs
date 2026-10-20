using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using FakeFantasyOnline.Foundation;

// Managed contracts use synthetic archives only, unless an explicit read-only TAR path is supplied.
internal static class TarResourceContract
{
    private static int checks;
    private static void Assert(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }
    private static void Throws<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { checks++; return; }
        throw new Exception("Expected " + typeof(T).Name + ": " + message);
    }
    private static byte[] Bytes(string text) { return Encoding.UTF8.GetBytes(text); }
    private static byte[] Join(params byte[][] parts)
    {
        using (var stream = new MemoryStream())
        { foreach (var part in parts) stream.Write(part, 0, part.Length); return stream.ToArray(); }
    }
    private static void Field(byte[] bytes, int offset, int length, string value)
    {
        byte[] encoded = Bytes(value);
        if (encoded.Length > length) throw new Exception("Bad fixture field");
        Array.Clear(bytes, offset, length);
        Buffer.BlockCopy(encoded, 0, bytes, offset, encoded.Length);
    }
    private static void Checksum(byte[] header)
    {
        for (int i = 148; i < 156; i++) header[i] = (byte)' ';
        int sum = 0;
        foreach (byte value in header) sum += value;
        Field(header, 148, 8, Convert.ToString(sum, 8).PadLeft(6, '0') + "\0 ");
    }
    private static byte[] Header(string name, long size, char type = '0', string prefix = "", bool gnu = false)
    {
        var header = new byte[512];
        Field(header, 0, 100, name);
        Field(header, 100, 8, "0000644\0");
        Field(header, 108, 8, "0000000\0");
        Field(header, 116, 8, "0000000\0");
        Field(header, 124, 12, Convert.ToString(size, 8).PadLeft(11, '0') + "\0");
        Field(header, 136, 12, "00000000000\0");
        header[156] = (byte)type;
        Field(header, 257, 6, gnu ? "ustar " : "ustar\0");
        Field(header, 263, 2, gnu ? " \0" : "00");
        Field(header, 345, 155, prefix);
        Checksum(header);
        return header;
    }
    private static byte[] Entry(string path, string body, char type = '0', string prefix = "")
    { return Entry(Header(path, Bytes(body).Length, type, prefix), Bytes(body)); }
    private static byte[] Entry(byte[] header, byte[] body)
    { return Join(header, body, new byte[(512 - body.Length % 512) % 512]); }
    private static byte[] Tar(params byte[][] entries) { return Join(Join(entries), new byte[1024]); }
    private static string Pax(string key, string value)
    {
        string content = " " + key + "=" + value + "\n";
        int length = Bytes(content).Length + 1;
        while (true)
        {
            string result = length + content;
            int actual = Bytes(result).Length;
            if (length == actual) return result;
            length = actual;
        }
    }
    private static byte[] Metadata(string text, char type = 'x')
    { return Entry("././@PaxHeader", text, type); }
    private static void Read(byte[] bytes, Action<string, long, Stream> callback = null, long? expected = null)
    {
        using (var stream = new MemoryStream(bytes, false))
            MobileResourceTarReader.Read(stream, expected ?? bytes.LongLength, CancellationToken.None, callback ?? ((p, n, s) => { }));
    }
    private static void Reject(byte[] bytes, string reason, long? expected = null)
    { Throws<InvalidDataException>(() => Read(bytes, null, expected), reason); }
    private static string Contents(Stream stream)
    {
        using (var output = new MemoryStream())
        { stream.CopyTo(output); return Encoding.UTF8.GetString(output.ToArray()); }
    }

    public static void Main(string[] args)
    {
        TestStreamingAndPaths();
        TestMetadata();
        TestUnsafePathsAndCollisions();
        TestHeadersAndBoundaries();
        TestHostileMetadata();
        TestLimitsAndCancellation();
        if (args.Length > 0)
        {
            long files = 0, payload = 0;
            using (var stream = File.OpenRead(args[0]))
                MobileResourceTarReader.Read(stream, stream.Length, CancellationToken.None, (p, n, s) => { files++; payload += n; });
            Console.WriteLine("READ ONLY archive validated: " + files + " regular files, " + payload + " payload bytes.");
        }
        Console.WriteLine("PASS safe streaming TAR contracts (" + checks + " checks; .NET managed host, no Unity/device/production validation)");
    }

    private static void TestStreamingAndPaths()
    {
        byte[] tar = Tar(Entry("Assets/", "", '5'), Entry("StreamingAssets/", "", '5', "Assets"),
            Entry("first.bin", "first payload", '0', "Assets/StreamingAssets"),
            Entry("AppIcon/icon.png", "PNG"), Entry("empty", ""), Entry("second.bin", "second payload"));
        var names = new List<string>();
        Stream retained = null;
        using (var source = new ShortReadStream(tar, 3))
        {
            MobileResourceTarReader.Read(source, tar.Length, CancellationToken.None, (path, length, data) =>
            {
                names.Add(path);
                Assert(!data.CanSeek && !data.CanWrite && data.CanRead, "Entry stream is read-only and forward-only");
                Assert(data.Length == length && data.Position == 0, "Entry length and initial position are exact");
                if (path.EndsWith("first.bin", StringComparison.Ordinal))
                {
                    Assert(data.ReadByte() == 'f', "Callback may consume only a prefix");
                    retained = data;
                    data.Dispose(); // Does not close input; remainder is still drained.
                }
                else if (path == "AppIcon/icon.png")
                {
                    Assert(Contents(data) == "PNG", "Skipping partial predecessor preserves the next header");
                    Assert(data.ReadByte() == -1 && data.Read(new byte[3], 0, 3) == 0, "File stream cannot read following padding/header");
                }
                else if (path == "empty") Assert(data.ReadByte() == -1, "Empty file stream is immediately bounded");
                else Assert(Contents(data) == "second payload", "Final payload is intact");
            });
            Assert(!source.Disposed && source.PositionRead == tar.Length, "Reader leaves caller's input open and consumes exact TAR");
        }
        Assert(names.Count == 4 && names[0] == "Assets/StreamingAssets/first.bin", "Directories are not sent as file callbacks; USTAR prefix is combined");
        Throws<ObjectDisposedException>(() => retained.ReadByte(), "Retained callback stream is invalid");
        var skippedHeader = Header("not-a-real-file", 0);
        int called = 0;
        Read(Tar(Entry(Header("skip.bin", skippedHeader.Length), skippedHeader), Entry("real", "yes")), (p, n, s) => called++);
        Assert(called == 2, "Skipped header-shaped payload is drained, never interpreted as a header");
        Read(Tar(), (p, n, s) => { throw new Exception("Empty archive callback"); });
        checks++;
        Read(Tar(Entry("a/b", "child"), Entry("a/", "", '5')));
        checks++; // Explicit parent directory may follow an implicitly registered parent.
        Read(Tar(Entry("unicode/中文.bin", "ok")));
        checks++;
    }

    private static void TestMetadata()
    {
        string longPath = "Assets/StreamingAssets/" + new string('x', 140) + ".bin";
        var seen = new List<string>();
        byte[] tar = Tar(Metadata(Pax("mtime", "1700000000.1234567"), 'g'),
            Metadata(Pax("path", longPath) + Pax("size", "7") + Pax("mtime", "-1.5")),
            Entry(Header("fallback", 0), Bytes("content")), Entry("plain", "next"));
        Read(tar, (p, n, s) => { seen.Add(p); Assert(Contents(s) == (p == longPath ? "content" : "next"), "PAX size controls bounded payload, not base header size"); });
        Assert(seen.Count == 2 && seen[0] == longPath && seen[1] == "plain", "PAX local path is applied once; global timestamps persist safely");

        Read(Tar(Metadata(Pax("path", "global") + Pax("size", "3"), 'g'), Entry(Header("fallback", 0), Bytes("abc")),
            Metadata(Pax("path", "") + Pax("size", ""), 'g'), Entry("ordinary", "1234")),
            (p, n, s) => Assert((p == "global" && n == 3) || (p == "ordinary" && n == 4), "PAX global values can be removed"));
        Read(Tar(Metadata(Pax("path", "global"), 'g'), Metadata(Pax("path", "")), Entry("local-cleared", "a")),
            (p, n, s) => Assert(p == "local-cleared", "Empty local PAX value suppresses inherited global value"));
        Read(Tar(Entry(Header("././@LongLink", Bytes(longPath).Length + 1, 'L', "", true), Join(Bytes(longPath), new byte[1])),
            Entry(Header("fallback", 2, '0', "", true), Bytes("ok"))),
            (p, n, s) => Assert(p == longPath && Contents(s) == "ok", "Simple GNU longname is supported"));
        Read(Tar(Metadata(Pax("path", "unicode/中文.bin")), Entry("unicode/??.bin", "ok")),
            (p, n, s) => Assert(p == "unicode/中文.bin", "PAX Unicode replacement base name is not the extraction destination"));
        Read(Tar(Metadata(Pax("path", "valid/longname.bin")), Entry("valid/", "ok")),
            (p, n, s) => Assert(p == "valid/longname.bin", "PAX base-name truncation at slash is harmless"));
        Read(Tar(Metadata(Pax("path", "nested/directory/")), Entry("fallback/", "", '5'), Entry("nested/directory/file", "x")));
        checks++;
    }

    private static void TestUnsafePathsAndCollisions()
    {
        string[] paths = { "/absolute", "../escape", "a/../escape", "a/./file", "./file", "a//file", "a\\file", "C:/file", "//server/file",
            "file/", "a\tfile", "a\nfile", "a\rfile", "a\u0001file", "a/space ", "a/dot.", "a/%2e%2e/file", "a?x", "a#x", "NUL.txt", "a/COM1", "a/<x>" };
        foreach (string path in paths) Reject(Tar(Entry(path, "")), "Unsafe path: " + path);
        Reject(Tar(Entry("a", "1"), Entry("a", "2")), "Duplicate file");
        Reject(Tar(Entry("A", "1"), Entry("a", "2")), "Case-colliding file");
        Reject(Tar(Entry("Dir/a", "1"), Entry("dir/b", "2")), "Case-colliding implicit directory");
        Reject(Tar(Entry("a/", "", '5'), Entry("a/", "", '5')), "Duplicate directory");
        Reject(Tar(Entry("a", "1"), Entry("a/b", "2")), "File as parent");
        Reject(Tar(Entry("a/b", "1"), Entry("a", "2")), "Parent as file");
        Reject(Tar(Entry("a/", "x", '5')), "Directory with data");
        Reject(Tar(Metadata(Pax("path", "../escape")), Entry("safe", "")), "PAX traversal");
        Reject(Tar(Metadata(Pax("path", "/escape")), Entry("safe", "")), "PAX absolute path");
        Reject(Tar(Metadata(Pax("path", "safe")), Entry("../unsafe", "")), "Unsafe base header is rejected even with safe PAX override");
        Reject(Tar(Metadata(Pax("path", "same")), Entry("safe1", ""), Entry("same", "")), "Duplicate effective PAX path");
        foreach (char type in new[] { '1', '2', '3', '4', '6', '7', 'S', 'K', 'Z' })
            Reject(Tar(Entry("special", "", type)), "Links, devices, FIFO, contiguous/sparse and unknown types");
        byte[] link = Header("regular", 0);
        Field(link, 157, 100, "target"); Checksum(link);
        Reject(Tar(link), "Unexpected link target on regular file");
    }

    private static void TestHeadersAndBoundaries()
    {
        byte[] header = Header("file", 0);
        header[0] ^= 1;
        Reject(Tar(header), "Bad checksum");
        string[] sizes = { "-0000000001\0", "00000000008\0", "0001\000002\0", "            " };
        foreach (string size in sizes)
        {
            header = Header("file", 0); Field(header, 124, 12, size); Checksum(header);
            Reject(Tar(header), "Invalid octal size");
        }
        header = Header("file", 0); header[124] = 0x80; Checksum(header);
        Reject(Tar(header), "Unsupported binary TAR size");
        header = Header("file", 0); Field(header, 257, 6, "other"); Checksum(header);
        Reject(Tar(header), "Unknown TAR magic");
        header = Header("file", 0); header[5] = (byte)'x'; Checksum(header);
        Reject(Tar(header), "Hidden bytes after name terminator");
        header = Header("file", 0); header[0] = 0xff; Checksum(header);
        Reject(Tar(header), "Invalid UTF-8 name");
        Reject(new byte[512], "One-block archive has no two-block terminator");
        Reject(Tar(Entry("file", "x")), "Invalid declared length alignment", 2047);
        Reject(Tar(Entry("file", "x")), "Expected archive length shorter than actual", 1024);
        byte[] valid = Tar(Entry("file", "contents"));
        Reject(valid, "Declared archive length longer than physical stream", valid.Length + 512);
        Reject(Join(valid, new byte[512]), "Physical stream longer than declared archive", valid.Length);
        Reject(Join(Header("file", 600), new byte[88]), "Truncated payload with declared complete archive", 2560);
        Reject(Join(Header("file", 0), new byte[512]), "Only one end block", 1024);
        Reject(Join(Header("file", 0), new byte[1023]), "Truncated end block", 1536);
        Reject(Tar(Header("file", 512)), "Entry payload cannot consume mandatory end marker");
        byte[] badPadding = Tar(Entry("file", "x")); badPadding[513] = 1;
        Reject(badPadding, "Nonzero entry padding");
        byte[] badEnd = Join(Tar(Entry("file", "")), new byte[512]); badEnd[badEnd.Length - 1] = 1;
        Reject(badEnd, "Nonzero data after end marker");
        Reject(Join(new byte[512], Header("hidden", 0), new byte[1024]), "Only one zero block before another header");
        Read(Join(Tar(Entry("file", "")), new byte[512 * 15])); checks++;
        Throws<InvalidOperationException>(() => Read(valid, (p, n, s) => { throw new InvalidOperationException("callback"); }), "Callback exception propagates");
        Reject(valid, "Negative declared archive length", -512);
        Reject(valid, "Excessive archive length", MobileResourceTarReader.MaxArchiveBytes + 512);
    }

    private static void TestHostileMetadata()
    {
        string[] malformed = { "0 path=x\n", "999 path=x\n", "11 path=x\n", "a path=x\n", "12 path=x\r\n", "10 path=x!", "10 path=x\ntrailing" };
        foreach (string text in malformed) Reject(Tar(Metadata(text), Entry("file", "")), "Malformed PAX framing");
        foreach (string key in new[] { "linkpath", "GNU.sparse.map", "SCHILY.filetype", "hdrcharset", "unknown" })
            Reject(Tar(Metadata(Pax(key, "value")), Entry("file", "")), "Hostile/unsupported PAX semantic key");
        Reject(Tar(Metadata(Pax("path", "a") + Pax("path", "b")), Entry("file", "")), "Duplicate PAX key");
        foreach (string value in new[] { "-1", "+1", "1.5", "9223372036854775808", "999999999999999999999999", "1\0" })
            Reject(Tar(Metadata(Pax("size", value)), Entry("file", "")), "Invalid PAX size");
        Reject(Tar(Metadata(Pax("size", "9223372036854775807")), Entry("file", "")), "PAX size exceeds archive");
        foreach (string value in new[] { "NaN", "Infinity", "1e10", ".5", "1.", "-", "1..2" })
            Reject(Tar(Metadata(Pax("mtime", value)), Entry("file", "")), "Invalid PAX timestamp");
        Reject(Tar(Metadata(Pax("comment", "injected\nvalue")), Entry("file", "")), "PAX value controls");
        Reject(Tar(Metadata(Pax("mtime", "1"))), "Dangling local PAX header");
        Reject(Tar(Metadata(Pax("mtime", "1")), Metadata(Pax("size", "0")), Entry("file", "")), "Repeated local metadata");
        Reject(Tar(Entry("././@LongLink", "../escape\0", 'L'), Entry("file", "")), "GNU longname traversal");
        Reject(Tar(Entry("././@LongLink", "valid", 'L'), Entry("file", "")), "GNU longname missing terminator");
        Reject(Tar(Entry("././@LongLink", "valid\0hidden\0", 'L'), Entry("file", "")), "GNU longname embedded NUL");
        Reject(Tar(Entry("././@LongLink", "valid\0", 'L')), "Dangling longname");
        Reject(Tar(Entry("././@LongLink", "valid\0", 'L'), Metadata(Pax("path", "different")), Entry("file", "")), "Ambiguous GNU/PAX override");
        byte[] invalidUtf = Bytes(Pax("comment", "x")); invalidUtf[invalidUtf.Length - 2] = 0xff;
        Reject(Tar(Entry(Header("././@PaxHeader", invalidUtf.Length, 'x'), invalidUtf), Entry("file", "")), "PAX invalid UTF-8");
        Reject(Tar(Metadata(new string('x', 65537)), Entry("file", "")), "Oversized PAX payload");
    }

    private static void TestLimitsAndCancellation()
    {
        string depth64 = string.Join("/", new string[64]).Replace("/", "a/") + "a";
        Read(Tar(Metadata(Pax("path", depth64)), Entry("file", ""))); checks++;
        Reject(Tar(Metadata(Pax("path", depth64 + "/a")), Entry("file", "")), "Path depth limit");
        Reject(Tar(Metadata(Pax("path", new string('a', 4097))), Entry("file", "")), "Path byte length limit");
        Read(Tar(Metadata(Pax("path", new string('a', 4096))), Entry("file", ""))); checks++;
        using (var source = new RepeatedHeaderStream(MobileResourceTarReader.MaxEntries))
        {
            MobileResourceTarReader.Read(source, source.ArchiveLength, CancellationToken.None, (p, n, s) => { });
            checks++;
        }
        using (var source = new RepeatedHeaderStream(MobileResourceTarReader.MaxEntries + 1))
            Throws<InvalidDataException>(() => MobileResourceTarReader.Read(source, source.ArchiveLength, CancellationToken.None, (p, n, s) => { }), "100,000-entry bound includes metadata headers");
        byte[] metadata = Metadata(Pax("comment", new string('a', 65000)), 'g');
        var tooMuchMetadata = new byte[130][];
        for (int i = 0; i < tooMuchMetadata.Length; i++) tooMuchMetadata[i] = metadata;
        Reject(Tar(tooMuchMetadata), "Aggregate PAX metadata bound");
        // Each unique leading directory creates 64 implicit nodes from one file header.
        // Path inventory must also be bounded, not merely the number of TAR headers.
        var tooManyPaths = new byte[1600][];
        string suffix = string.Join("/", new string[63]).Replace("/", "a/") + "a";
        for (int i = 0; i < tooManyPaths.Length; i++)
            tooManyPaths[i] = Entry("a", "", '0', i.ToString() + "/" + suffix.Substring(0, suffix.Length - 2));
        Reject(Tar(tooManyPaths), "Implicit parent paths cannot amplify the inventory beyond its bound");
        var tooManyPathBytes = new byte[250][];
        var longComponents = new string[63];
        for (int i = 0; i < longComponents.Length; i++) longComponents[i] = new string('a', 40);
        string longSuffix = string.Join("/", longComponents);
        for (int i = 0; i < tooManyPathBytes.Length; i++)
            tooManyPathBytes[i] = Join(Metadata(Pax("path", i.ToString() + "/" + longSuffix)), Entry("fallback", ""));
        Reject(Tar(tooManyPathBytes), "Cumulative stored prefix bytes are bounded below the node-count limit");
        byte[] tar = Tar(Entry("file", new string('x', 100000)));
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            Throws<OperationCanceledException>(() => MobileResourceTarReader.Read(new MemoryStream(tar), tar.Length, cancellation.Token, (p, n, s) => { }), "Cancelled before first read");
        }
        using (var cancellation = new CancellationTokenSource())
        {
            Throws<OperationCanceledException>(() => MobileResourceTarReader.Read(new MemoryStream(tar), tar.Length, cancellation.Token,
                (p, n, s) => { Assert(s.ReadByte() == 'x', "Read before cancellation"); cancellation.Cancel(); }), "Cancelled during skipped payload drain");
        }
        using (var cancellation = new CancellationTokenSource())
        {
            Throws<OperationCanceledException>(() => MobileResourceTarReader.Read(new MemoryStream(tar), tar.Length, cancellation.Token,
                (p, n, s) => { cancellation.Cancel(); s.ReadByte(); }), "Callback stream reads respect cancellation");
        }
        Throws<ArgumentNullException>(() => MobileResourceTarReader.Read(null, 1024, CancellationToken.None, (p, n, s) => { }), "Null input");
        Throws<ArgumentNullException>(() => MobileResourceTarReader.Read(new MemoryStream(), 1024, CancellationToken.None, null), "Null callback");
    }

    private sealed class ShortReadStream : Stream
    {
        private readonly byte[] data;
        private readonly int chunk;
        public int PositionRead { get; private set; }
        public bool Disposed { get; private set; }
        public ShortReadStream(byte[] data, int chunk) { this.data = data; this.chunk = chunk; }
        public override bool CanRead { get { return !Disposed; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = Math.Min(Math.Min(count, chunk), data.Length - PositionRead);
            Buffer.BlockCopy(data, PositionRead, buffer, offset, read); PositionRead += read; return read;
        }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }

    // No giant fixture allocation. Empty global PAX headers exercise the header count independently of duplicate paths.
    private sealed class RepeatedHeaderStream : Stream
    {
        private readonly byte[] repeated = Header("global", 0, 'g');
        private readonly long headersBytes;
        private long position;
        public long ArchiveLength { get { return headersBytes + 1024; } }
        public RepeatedHeaderStream(int count) { headersBytes = count * 512L; }
        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { return ArchiveLength; } }
        public override long Position { get { return position; } set { throw new NotSupportedException(); } }
        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = (int)Math.Min(count, ArchiveLength - position);
            for (int i = 0; i < read; i++, position++) buffer[offset + i] = position < headersBytes ? repeated[position % 512] : (byte)0;
            return read;
        }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
    }
}

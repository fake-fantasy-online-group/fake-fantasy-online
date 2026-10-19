using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FakeFantasyOnline.Foundation
{
/// <summary>Versioned resource content manifest. Hashes identify content; they are not signatures.</summary>
public sealed class MobileResourceManifest
{
    public const string FileName = "mobile-resources.tsv";
    public const int MaxManifestBytes = 8 * 1024 * 1024;
    public sealed class Entry
    {
        public string Source { get; private set; }
        public string Destination { get; private set; }
        public long Length { get; private set; }
        public string Sha256 { get; private set; }
        internal Entry(string source, string destination, long length, string sha256)
        { Source = source; Destination = destination; Length = length; Sha256 = sha256; }
    }
    public IReadOnlyList<Entry> Entries { get; private set; }
    public string Version { get; private set; }
    public bool Offline { get; private set; }
    public string ClientPlatform { get; private set; }
    public string ClientVersion { get; private set; }

    public static MobileResourceManifest Parse(string text)
    {
        if (text == null || Encoding.UTF8.GetByteCount(text) > MaxManifestBytes)
            throw new InvalidDataException("Mobile resource manifest is missing or too large.");
        bool offline;
        string platform = null, clientVersion = null;
        var entries = new List<Entry>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var reader = new StringReader(text))
        {
            string header = reader.ReadLine();
            offline = header == "FFO-MOBILE-OFFLINE-1";
            if (header != "FFO-MOBILE-1" && !offline) throw new InvalidDataException("Unsupported mobile resource manifest.");
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var fields = line.Split('\t');
                if (fields.Length == 3 && fields[0] == "@client")
                {
                    if (platform != null || entries.Count != 0 || string.IsNullOrWhiteSpace(fields[1]) || string.IsNullOrWhiteSpace(fields[2]))
                        throw new InvalidDataException("Invalid client compatibility declaration.");
                    platform = fields[1]; clientVersion = fields[2];
                    continue;
                }
                long length;
                if (fields.Length != 4 || !long.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out length) || length < 0)
                    throw new InvalidDataException("Invalid mobile resource entry.");
                ValidateRelativePath(fields[0]); ValidateRelativePath(fields[1]);
                if (!names.Add(fields[1]) || entries.Count >= 100000)
                    throw new InvalidDataException("Duplicate or excessive mobile resource entries.");
                if (fields[3].Length != 64 || Array.Exists(fields[3].ToCharArray(), c => !((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))))
                    throw new InvalidDataException("Invalid SHA-256 digest.");
                entries.Add(new Entry(fields[0], fields[1], length, fields[3]));
            }
        }
        if (entries.Count == 0) throw new InvalidDataException("Empty mobile resource manifest.");
        using (var sha = SHA256.Create())
            return new MobileResourceManifest { Entries = entries.AsReadOnly(), Offline = offline, ClientPlatform = platform, ClientVersion = clientVersion, Version = Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(text))) };
    }

    public void VerifyDirectory(string root)
    {
        foreach (var entry in Entries)
            if (!Verify(FilePath(root, entry.Destination), entry))
                throw new InvalidDataException("Missing or corrupt mobile resource: " + entry.Destination);
    }

    public static void ValidateRelativePath(string path)
    {
        if (string.IsNullOrEmpty(path) || path.IndexOfAny(new[] { '\\', ':', '\t', '\r', '\n', '\0', '?', '#', '%' }) >= 0 || path.StartsWith("/"))
            throw new InvalidDataException("Unsafe mobile resource path.");
        foreach (string part in path.Split('/'))
            if (part == "" || part == "." || part == ".." || part.EndsWith(".") || part.EndsWith(" ") || part.EndsWith(".part", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Unsafe mobile resource path segment.");
    }

    public static string FilePath(string root, string relative)
    {
        ValidateRelativePath(relative);
        return Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
    }

    public static string HashFile(string path, Action<long> progress = null)
    {
        // Report bytes actually hashed, including within large packages. Callbacks run on
        // the caller's thread; UI callers must marshal or use atomic counters.
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create())
        {
            var buffer = new byte[256 * 1024];
            long completed = 0;
            progress?.Invoke(0);
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                sha.TransformBlock(buffer, 0, read, buffer, 0);
                completed += read;
                progress?.Invoke(completed);
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return Hex(sha.Hash);
        }
    }

    public static bool Verify(string path, Entry entry, Action<long> progress = null)
    {
        return File.Exists(path) && new FileInfo(path).Length == entry.Length && HashFile(path, progress) == entry.Sha256;
    }

    public static void CommitFile(string temporary, string destination, Entry entry, Action<long> progress = null)
    {
        if (!Verify(temporary, entry, progress)) throw new InvalidDataException("Mobile resource length/hash mismatch: " + entry.Destination);
        if (File.Exists(destination)) File.Delete(destination);
        File.Move(temporary, destination);
    }

    private static string Hex(byte[] bytes) { return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant(); }
}
}

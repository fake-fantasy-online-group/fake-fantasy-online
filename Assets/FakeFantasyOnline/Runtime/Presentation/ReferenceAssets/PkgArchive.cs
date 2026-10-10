using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official package archive owner.
/// </summary>
public class PkgArchive
{
    private struct Entry
    {
        public string pkg;
        public long offset;
        public int packsize;
        public int originsize;
        public bool enc;
        public ulong seed;
    }

    private readonly Dictionary<string, Entry> index;

    private const uint MAGIC_ENC = 1363695685u;
    private const ulong FNV_OFFSET = 14695981039346656037UL;
    private const ulong FNV_PRIME = 1099511628211UL;
    private const ulong SM_GAMMA = 11400714819323198485UL;
    private const ulong SM_MIX1 = 13787848793156543929UL;
    private const ulong SM_MIX2 = 10723151780598845931UL;

    private static readonly byte[] SECRET =
    {
        0x7c, 0x3d, 0x9a, 0xf1, 0xe0, 0x5b, 0x28, 0xc4,
        0xd6, 0x90, 0x7e, 0x1a, 0x3f, 0x8b, 0x52, 0xc0,
        0xa9, 0x4d, 0x17, 0xe6, 0xb3, 0x82, 0x0f, 0xdc,
        0x5e, 0x19, 0xa7, 0xc4, 0x0d, 0xb6, 0xf2, 0x38
    };

    public string Tag;

    private static readonly Dictionary<string, List<(string, string)>> dirListCache =
        new Dictionary<string, List<(string, string)>>();

    public static string UsageLogPath;

    private static readonly HashSet<string> logged = new HashSet<string>();

    public int FileCount => index.Count;

    public int PkgCount { get; private set; }

    private static ulong DeriveSeedCore(byte[] extra)
    {
        unchecked
        {
            ulong value = FNV_OFFSET;
            for (int i = 0; i < SECRET.Length; i++)
            {
                value = (value ^ SECRET[i]) * FNV_PRIME;
            }

            for (int i = 0; i < extra.Length; i++)
            {
                value = (value ^ extra[i]) * FNV_PRIME;
            }

            return value;
        }
    }

    private static ulong FileSeed(string name)
    {
        return DeriveSeedCore(Encoding.UTF8.GetBytes(name.ToLowerInvariant()));
    }

    private static ulong IndexSeed()
    {
        return DeriveSeedCore(Encoding.ASCII.GetBytes("INDEX"));
    }

    private static void XorStream(byte[] buf, int len, ulong seed)
    {
        unchecked
        {
            int pos = 0;
            while (pos < len)
            {
                seed += SM_GAMMA;
                ulong mixed = seed;
                mixed = (mixed ^ (mixed >> 30)) * SM_MIX1;
                mixed = (mixed ^ (mixed >> 27)) * SM_MIX2;
                mixed ^= mixed >> 31;

                int count = Math.Min(8, len - pos);
                for (int i = 0; i < count; i++)
                {
                    buf[pos + i] ^= (byte)(mixed >> (i * 8));
                }

                pos += 8;
            }
        }
    }

    public bool MountLooseDirs(IEnumerable<string> roots, string filterFile = null)
    {
        HashSet<string> allow = null;
        if (filterFile != null && File.Exists(filterFile))
        {
            allow = new HashSet<string>();
            string[] lines = File.ReadAllLines(filterFile);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > 0)
                {
                    allow.Add(lines[i].Replace('\\', '/'));
                }
            }
        }

        foreach (string candidate in roots)
        {
            if (string.IsNullOrEmpty(candidate) || !Directory.Exists(candidate))
            {
                continue;
            }

            string root = Path.GetFullPath(candidate);
            List<(string, string)> files;
            if (!dirListCache.TryGetValue(root, out files))
            {
                files = new List<(string, string)>();
                foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    string relative = file.Substring(root.Length + 1).Replace('\\', '/');
                    files.Add((file, relative));
                }

                dirListCache[root] = files;
            }

            for (int i = 0; i < files.Count; i++)
            {
                string file = files[i].Item1;
                string relative = files[i].Item2;
                if ((allow == null || allow.Contains(relative)) && !index.ContainsKey(relative))
                {
                    index[relative] = new Entry
                    {
                        pkg = file,
                        offset = -1L
                    };
                }
            }

            PkgCount++;
        }

        return index.Count > 0;
    }

    public bool MountPkgs(IEnumerable<string> pkgPaths, string filterFile = null)
    {
        HashSet<string> allow = null;
        if (filterFile != null && File.Exists(filterFile))
        {
            // Official 0x06000bec: the allow set is built with StringComparer.OrdinalIgnoreCase
            // (arm64 0x2fcf1c / x86_64 read StringComparer static field +0x18, the same slot
            // get_OrdinalIgnoreCase returns), so a filter line matches a package entry
            // regardless of case. The default ordinal comparer would not.
            allow = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] lines = File.ReadAllLines(filterFile);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > 0)
                {
                    allow.Add(lines[i].Replace('\\', '/'));
                }
            }
        }

        int mounted = 0;
        foreach (string pkgPath in pkgPaths)
        {
            if (string.IsNullOrEmpty(pkgPath) || !File.Exists(pkgPath))
            {
                continue;
            }

            ReadIndex(pkgPath, allow, true);
            mounted++;
        }

        PkgCount = mounted;
        return index.Count > 0;
    }

    private void LogUse(string path, Entry? e)
    {
        if (UsageLogPath == null)
        {
            return;
        }

        string key = string.Concat(Tag, "\t", path);
        lock (logged)
        {
            if (!logged.Add(key))
            {
                return;
            }

            string source;
            if (!e.HasValue)
            {
                source = "MISS";
            }
            else if (e.Value.offset < 0L)
            {
                source = e.Value.pkg.Contains("05diff") ? "05diff" : "q21";
            }
            else
            {
                source = Path.GetFileName(e.Value.pkg);
            }

            File.AppendAllText(
                UsageLogPath,
                string.Concat(Tag, "\t", source, "\t", path, "\n"));
        }
    }

    public bool Mount(IEnumerable<string> candidateDirs, string pattern = "objects*.pkg")
    {
        string directory = null;
        foreach (string candidate in candidateDirs)
        {
            if (string.IsNullOrEmpty(candidate) || !Directory.Exists(candidate))
            {
                continue;
            }

            string[] found = Directory.GetFiles(candidate, pattern);
            if (found.Length > 0)
            {
                directory = candidate;
                break;
            }
        }

        if (directory == null)
        {
            return false;
        }

        string[] packages = Directory.GetFiles(directory, pattern);
        Array.Sort(packages);
        for (int i = 0; i < packages.Length; i++)
        {
            ReadIndex(packages[i]);
        }

        PkgCount = packages.Length;
        return index.Count > 0;
    }

    private void ReadIndex(string pkgPath, HashSet<string> allow = null, bool firstWins = false)
    {
        using (FileStream fs = new FileStream(pkgPath, FileMode.Open, FileAccess.Read))
        {
            byte[] header = new byte[16];
            if (!ReadFull(fs, header, header.Length))
            {
                return;
            }

            uint magic = BitConverter.ToUInt32(header, 0);
            uint count = BitConverter.ToUInt32(header, 4);
            uint indexOffset = BitConverter.ToUInt32(header, 8);
            uint indexSize = BitConverter.ToUInt32(header, 12);

            byte[] bytes = new byte[indexSize];
            fs.Seek(indexOffset, SeekOrigin.Begin);
            if (!ReadFull(fs, bytes, bytes.Length))
            {
                return;
            }

            if (magic == MAGIC_ENC)
            {
                XorStream(bytes, bytes.Length, IndexSeed());
            }

            int pos = 0;
            int remaining = unchecked((int)count);
            while (remaining > 0 && pos + 2 <= bytes.Length)
            {
                ushort nameLength = BitConverter.ToUInt16(bytes, pos);
                int nameOffset = pos + 2;
                int tailOffset = nameOffset + nameLength;
                int next = tailOffset + 16;
                if (next > bytes.Length)
                {
                    break;
                }

                // Official 0x06000bef: the name bytes are copied into their own array first
                // (arm64 0x2fd898 newarr + 0x2fd8b8 Array.Copy) because the decoder is
                // Gbk.GetString(Byte[]) 06000471 (arm64 0x2fd8c4 -> 0x446834, x86_64 0x30fbe1
                // -> 0x45e800), not Encoding.GetEncoding(936).GetString(byte[], int, int).
                byte[] nameBytes = new byte[nameLength];
                Array.Copy(bytes, nameOffset, nameBytes, 0, nameLength);
                string name = Gbk.GetString(nameBytes).Replace('\\', '/');
                uint offset = BitConverter.ToUInt32(bytes, tailOffset + 4);
                uint originsize = BitConverter.ToUInt32(bytes, tailOffset + 8);
                uint packsize = BitConverter.ToUInt32(bytes, tailOffset + 12);
                if ((allow == null || allow.Contains(name)) &&
                    (!firstWins || !index.ContainsKey(name)))
                {
                    index[name] = new Entry
                    {
                        pkg = pkgPath,
                        offset = offset,
                        // Official 0x06000bef stores the raw uint bit pattern (no check emitted on
                        // either architecture), so these conversions stay unchecked.
                        packsize = unchecked((int)packsize),
                        originsize = unchecked((int)originsize),
                        enc = magic == MAGIC_ENC,
                        seed = magic == MAGIC_ENC ? FileSeed(name) : 0UL
                    };
                }

                pos = next;
                remaining--;
            }
        }
    }

    private static bool ReadFull(FileStream fs, byte[] buf, int n)
    {
        if (n <= 0)
        {
            return true;
        }

        int read = fs.Read(buf, 0, n);
        if (read <= 0)
        {
            return false;
        }

        while (read < n)
        {
            int next = fs.Read(buf, read, n - read);
            if (next <= 0)
            {
                return false;
            }

            read += next;
        }

        return true;
    }

    public bool Has(string path)
    {
        return index.ContainsKey(path.Replace('\\', '/'));
    }

    public string FindUnique(string dirPrefix, string suffix)
    {
        string result = null;
        foreach (string path in index.Keys)
        {
            if (!path.StartsWith(dirPrefix, StringComparison.OrdinalIgnoreCase) ||
                !path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (result != null)
            {
                return null;
            }

            result = path;
        }

        return result;
    }

    public List<string> FindAll(string dirPrefix, string suffix)
    {
        List<string> result = new List<string>();
        foreach (string path in index.Keys)
        {
            if (path.StartsWith(dirPrefix, StringComparison.OrdinalIgnoreCase) &&
                path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(path);
            }
        }

        return result;
    }

    public byte[] Read(string path)
    {
        string normalized = path.Replace('\\', '/');
        Entry e;
        if (!index.TryGetValue(normalized, out e))
        {
            LogUse(normalized, null);
            return null;
        }

        LogUse(normalized, e);
        long readStarted = LoadTrace.Now;
        if (e.offset < 0L)
        {
            try
            {
                byte[] loose = File.ReadAllBytes(e.pkg);
                if (LoadTrace.On)
                {
                    LoadTrace.Pkg(
                        normalized,
                        Tag + ":散文件",
                        loose.Length,
                        loose.Length,
                        LoadTrace.Ms(readStarted),
                        0f);
                }

                return loose;
            }
            catch
            {
                return null;
            }
        }

        byte[] packed;
        using (FileStream fs = new FileStream(e.pkg, FileMode.Open, FileAccess.Read))
        {
            fs.Seek(e.offset, SeekOrigin.Begin);
            packed = new byte[e.packsize];
            ReadFull(fs, packed, e.packsize);
        }

        if (e.enc)
        {
            XorStream(packed, e.packsize, e.seed);
        }

        float readMs = LoadTrace.Ms(readStarted);
        if (e.packsize != e.originsize)
        {
            long inflateStarted = LoadTrace.Now;
            byte[] inflated = Inflate(packed, e.originsize);
            if (LoadTrace.On)
            {
                LoadTrace.Pkg(
                    normalized,
                    Tag + (e.enc ? ":pkg-zlib-enc" : ":pkg-zlib"),
                    e.packsize,
                    e.originsize,
                    readMs,
                    LoadTrace.Ms(inflateStarted));
            }

            return inflated;
        }

        if (LoadTrace.On)
        {
            LoadTrace.Pkg(
                normalized,
                Tag + (e.enc ? ":pkg-raw-enc" : ":pkg-raw"),
                e.packsize,
                e.originsize,
                readMs,
                0f);
        }

        return packed;
    }

    private static byte[] Inflate(byte[] zlib, int originsize)
    {
        try
        {
            using (MemoryStream source = new MemoryStream(zlib, 2, zlib.Length - 2, false))
            using (DeflateStream stream = new DeflateStream(source, CompressionMode.Decompress, false))
            {
                byte[] result = new byte[originsize];
                int read = 0;
                while (read < originsize)
                {
                    int next = stream.Read(result, read, originsize - read);
                    read += next;
                    if (next <= 0)
                    {
                        break;
                    }
                }

                return result;
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    public PkgArchive()
    {
        index = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        Tag = "?";
    }

}

}

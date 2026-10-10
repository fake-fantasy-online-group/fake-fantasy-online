using System.Collections.Generic;
using System.Diagnostics;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official timeline-line owner used by the art-detail boundary.
/// </summary>
/// <remarks>
/// This file retains the original timeline/art-detail cone. Lifecycle, world instrumentation,
/// reporting helpers and full export are recovered in the adjacent LoadTrace partials; see
/// VERSION_UPGRADE_1_5_8_LOADTRACE_LIFECYCLE_EXPORT_V1.md for pending integration boundaries.
/// </remarks>
public static partial class LoadTrace
{
    public const float RecordSec = 60f;
    public const int MaxTimelineLines = 40000;
    // Official static LoadTrace row SlowNetMs (System.Int32): LoadTrace.Net (0x060001EF) logs a
    // timeline line once the dispatch time reaches it (both architectures compare `3.0 <= ms`).
    public const int SlowNetMs = 3;

    public static bool On { get; private set; }

    // Official 06000206: keep every nondefault static initializer in this one lexical
    // sequence. Splitting them across partials cannot preserve the native allocation order.
    // The compiler supplies the initializer; do not add an explicit static constructor.
    private static readonly List<string> _tl = new List<string>(8192);
    private static string _tag = "";
    private static readonly double TickMs = 1000.0 / Stopwatch.Frequency;
    private static readonly Dictionary<string, (int, string, UnityEngine.LogType)> _logs =
        new Dictionary<string, (int, string, UnityEngine.LogType)>();
    private static readonly List<(string, float)> _phases = new List<(string, float)>();
    private static readonly Dictionary<string, (int, float, long)> _pkgBy =
        new Dictionary<string, (int, float, long)>();
    private static readonly Dictionary<string, (int, float, int)> _pkgTop =
        new Dictionary<string, (int, float, int)>();
    private static readonly List<(string, string, float, bool)> _avTop =
        new List<(string, string, float, bool)>();
    private static readonly List<(
        int,
        int,
        string,
        string,
        long,
        int,
        long,
        float,
        float,
        int,
        long)> _gridRows =
            new List<(int, int, string, string, long, int, long, float, float, int, long)>();
    private static readonly Dictionary<int, (int, float, long)> _netBy =
        new Dictionary<int, (int, float, long)>();
    private static int _gcPrev = -1;
    private static readonly List<string> _hitchTop = new List<string>();
    private static readonly double[] _segMs = new double[(int)Seg._Count];
    private static readonly string[] _segName =
    {
        "远端玩家", "怪宠实体", "特效", "美术管线", "其它",
        "└摆姿势", "└名牌血条", "└路径", "└头部", "└骑乘", "└走动待机",
        "  └光圈X", "  └合成坐骑", "  └可见判定", "  └骑手姿势"
    };
    private static readonly double[] _segTotal = new double[(int)Seg._Count];
    private static readonly long[] _segCalls = new long[(int)Seg._Count];

    private static bool _tlFull;
    private static long _t0;
    private static int _pkgN;
    private static long _pkgPack;
    private static long _pkgOrig;
    private static float _pkgReadMs;
    private static float _pkgInflMs;
    private static int _decPxN;
    private static int _decGeoN;
    private static float _decPxMs;
    private static float _decGeoMs;
    private static long _decPxBytes;
    private static int _upN;
    private static long _upBytes;
    private static int _frUp;
    private static long _frUpBytes;
    // Official static LoadTrace rows for the receive counters: _netN (+0xC0, Int32), _netBytes
    // (+0xC8, Int64), _netMs (+0xD0, Single), _netBy (+0xD8, Dictionary<Int32,(Int32,Single,
    // Int64)>), _backlogPeak (+0xE0, Int32), _frNet (+0x104, Int32), _frNetMs (+0x108, Single).
    private static int _netN;
    private static long _netBytes;
    private static float _netMs;
    private static int _backlogPeak;
    private static int _frNet;
    private static float _frNetMs;

    private static long Ticks => Stopwatch.GetTimestamp();

    private static float T => (float)(TickMs * (Ticks - _t0) / 1000.0);

    private static string Stamp => string.Format("[+{0,7:F3}]", T);

    public static long Now => On ? Stopwatch.GetTimestamp() : 0L;

    public static void Line(string s)
    {
        if (!On)
        {
            return;
        }

        if (_tl.Count < MaxTimelineLines)
        {
            _tl.Add(Stamp + " " + s);
            return;
        }

        if (_tlFull)
        {
            return;
        }

        _tlFull = true;
        _tl.Add(Stamp + " …… 时间线已达上限, 后续只进汇总 ……");
    }

    // 060001fa
    private static string Kb(long b)
    {
        if (b < 1024L)
        {
            return b + "B";
        }

        if (b < 1024L * 1024L)
        {
            return string.Format("{0:F0}KB", b / 1024f);
        }

        return string.Format("{0:F1}MB", b / (1024f * 1024f));
    }

    // 060001e9
    public static void Pkg(
        string path,
        string source,
        int packBytes,
        int origBytes,
        float readMs,
        float inflateMs)
    {
        if (!On)
        {
            return;
        }

        _pkgN++;
        _pkgPack += packBytes;
        _pkgOrig += origBytes;
        _pkgReadMs += readMs;
        _pkgInflMs += inflateMs;

        (int, float, long) by;
        _pkgBy.TryGetValue(source, out by);
        _pkgBy[source] =
            (by.Item1 + 1, by.Item2 + readMs + inflateMs, by.Item3 + origBytes);

        (int, float, int) top;
        _pkgTop.TryGetValue(path, out top);
        _pkgTop[path] = (top.Item1 + 1, top.Item2 + readMs + inflateMs, origBytes);

        if (readMs + inflateMs < 2f)
        {
            return;
        }

        Line(string.Format(
            "PKG {0} [{1}] 压{2}→原{3} 读盘{4:F1}ms 解压{5:F1}ms",
            path,
            source,
            Kb(packBytes),
            Kb(origBytes),
            readMs,
            inflateMs));
    }

    public static float Ms(long t0)
    {
        return t0 != 0L && On
            ? (float)(TickMs * (Stopwatch.GetTimestamp() - t0))
            : 0f;
    }

    public static void Decode(bool pixels, int w, int h, float ms)
    {
        if (!On)
        {
            return;
        }

        if (pixels)
        {
            _decPxN++;
            _decPxMs += ms;
            _decPxBytes += (long)w * h * 4L;
            return;
        }

        _decGeoN++;
        _decGeoMs += ms;
    }

    public static void GridDone(int actId, int frames, long bytes, float wallMs)
    {
        if (!On)
        {
            return;
        }

        for (int index = 0; index < _gridRows.Count; index++)
        {
            (
                int,
                int,
                string,
                string,
                long,
                int,
                long,
                float,
                float,
                int,
                long) row = _gridRows[index];
            if (row.Item1 == actId && row.Item9 < 0f)
            {
                _gridRows[index] = (
                    row.Item1,
                    row.Item2,
                    row.Item3,
                    row.Item4,
                    row.Item5,
                    row.Item6,
                    row.Item7,
                    row.Item8,
                    wallMs,
                    frames,
                    bytes);
                break;
            }
        }

        Line(string.Format(
            "网格 act{0} 后台长齐 {1}帧/{2} 墙钟{3:F0}ms",
            actId,
            frames,
            string.Format("{0:F1}MB", bytes / 1048576f),
            wallMs));
    }

    public static void Upload(int count, long bytes)
    {
        if (!On)
        {
            return;
        }

        _upN += count;
        _upBytes += bytes;
        _frUp += count;
        _frUpBytes += bytes;
    }

    // Official 0x060001EF LoadTrace.Net(System.Int32 msgId, System.Int32 len, System.Single ms)
    // (arm64 0x35d45c / x86_64 0x36f520). Called by NetClient.Update (0x0600077D) once per
    // dispatched packet, with the trace-timestamp delta in ms and 0f at the call site:
    //   if (!On) return;
    //   _netN++; _netBytes += len; _netMs += ms;
    //   _netBy.TryGetValue(msgId, out cur);
    //   _netBy[msgId] = (cur.Item1 + 1, ms + cur.Item2, cur.Item3 + len);
    //   _frNet++; _frNetMs += ms;
    //   if (SlowNetMs <= ms) Line(string.Format(usage 12889, msgId, len, ms));
    // The native null guard around _netBy is the unreachable readonly guard (the dictionary is
    // initialized in the same class), so it is not reproduced.
    public static void Net(int msgId, int len, float ms)
    {
        if (!On)
        {
            return;
        }

        _netN++;
        _netBytes += len;
        _netMs += ms;

        (int, float, long) by;
        _netBy.TryGetValue(msgId, out by);
        _netBy[msgId] = (by.Item1 + 1, ms + by.Item2, by.Item3 + len);

        _frNet++;
        _frNetMs += ms;

        if (SlowNetMs <= ms)
        {
            Line(string.Format("NET 消息 id={0} {1}B 派发{2:F1}ms", msgId, len, ms));
        }
    }

    // Official 0x060001F0 LoadTrace.Backlog(System.Int32 depth) (arm64 0x35d70c / x86_64
    // 0x36f7e0). Called by NetClient.Update once per frame, before the dispatch stopwatch is
    // restarted, with the inbox count:
    //   if (!On) return;
    //   if (_backlogPeak < depth) _backlogPeak = depth;
    public static void Backlog(int depth)
    {
        if (!On)
        {
            return;
        }

        if (_backlogPeak < depth)
        {
            _backlogPeak = depth;
        }
    }
}

}

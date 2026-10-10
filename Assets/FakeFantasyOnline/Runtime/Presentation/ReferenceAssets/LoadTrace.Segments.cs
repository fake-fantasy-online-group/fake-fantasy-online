using System.Diagnostics;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class LoadTrace
{
    public enum Seg
    {
        远端玩家 = 0,
        怪宠实体 = 1,
        特效 = 2,
        美术管线 = 3,
        其它 = 4,
        玩_摆姿势 = 5,
        玩_名牌血条 = 6,
        玩_路径 = 7,
        玩_头部 = 8,
        玩_骑乘 = 9,
        玩_走动待机 = 10,
        骑_光圈X = 11,
        骑_合成 = 12,
        骑_可见判定 = 13,
        骑_骑手姿势 = 14,
        _Count = 15,
    }

    public static void NoteSegCall(Seg seg)
    {
        if (On) _segCalls[(int)seg]++;
    }

    // 0x060001f4
    public static long SegStamp() => Stopwatch.GetTimestamp();

    // 0x060001f5
    public static void AddSeg(Seg seg, long t0)
    {
        long now = Stopwatch.GetTimestamp();
        long frequency = Stopwatch.Frequency;
        ref double segmentMs = ref _segMs[(int)seg];
        double ms = (double)unchecked(now - t0) * 1000.0 / (double)frequency;
        segmentMs += ms;
        _segTotal[(int)seg] += ms;
    }
}

}

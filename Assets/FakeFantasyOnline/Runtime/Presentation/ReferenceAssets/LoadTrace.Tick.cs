using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class LoadTrace
{
    private static int _frames;
    private static float _maxFrameMs;
    private static int _hitches;
    private static int _gcTotal;
    private static int _srCount;
    private static int _srLast;
    private static double _wallMsTotal;

    // 060001d9; preserve the native unordered/NaN result.
    private static float HitchMs
    {
        get
        {
            float ms = GameSettings.TargetFrameMs * 1.5f;
            return ms < 12f ? 12f : ms;
        }
    }

    // 060001f2.
    public static void Tick()
    {
        if (!On) return;
        AutoStop();
        if (!On) return;

        _frames = unchecked(_frames + 1);
        int gc = unchecked(GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2));
        int gcDelta = _gcPrev < 0 ? 0 : unchecked(gc - _gcPrev);
        _gcPrev = gc;
        _gcTotal = unchecked(_gcTotal + gcDelta);
        float ms = Time.unscaledDeltaTime * 1000f;
        _wallMsTotal += ms;
        if (ms > _maxFrameMs) _maxFrameMs = ms;

        if (ms >= HitchMs)
        {
            _hitches = unchecked(_hitches + 1);
            double scriptMs = 0d;
            for (int index = 0; index < 5; index++) scriptMs += _segMs[index];
            double engineMs = ms - scriptMs;
            string segments = "";
            for (int index = 0; index < _segMs.Length; index++)
            {
                if (_segMs[index] >= 0.5d)
                    segments += string.Format(" {0}{1:F0}", _segName[index], _segMs[index]);
            }

            string line = string.Format(
                "★长帧 {0:F0}ms | 派发{1}条/{2:F0}ms | 纸娃娃{3}个 | 上传{4}张/{5} | 后台解码{6}套 | GC{7}次",
                ms, _frNet, _frNetMs, _frAv, _frUp,
                string.Format("{0:F1}MB", _frUpBytes / 1048576f), MonsterEntity.TraceArtJobs, gcDelta);
            line += string.Format(" | 脚本{0:F0}ms[{1}] | 引擎{2:F0}ms | SR{3}",
                scriptMs, segments.Trim(), engineMs, _srCount);
            Line(line);
            if (_hitchTop.Count < 400)
                _hitchTop.Add(string.Format("{0,6:F0}ms  ", ms) + line.Substring(line.IndexOf('|')));
        }

        _frNet = 0;
        _frNetMs = 0f;
        _frUp = 0;
        _frUpBytes = 0L;
        _frAv = 0;
        for (int index = 0; index < _segMs.Length; index++) _segMs[index] = 0d;
    }

    // 060001f7.
    public static void NoteSpriteRenderers(int n)
    {
        if (!On) return;
        _srCount = n;
        _srLast = n;
    }
}

}

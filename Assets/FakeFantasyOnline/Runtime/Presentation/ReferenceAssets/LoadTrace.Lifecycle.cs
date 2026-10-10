using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class LoadTrace
{
    private static double _stopAtSec;

    // Official 060001dc/060001dd, static string row +0x30; default null.
    public static string LastPath { get; private set; }

    // 060001e1.
    public static void Begin(string tag)
    {
        if (On) Stop("被新的一段打断");

        _tl.Clear();
        _tlFull = false;
        ResetAll();
        _tag = tag ?? "";
        _t0 = Stopwatch.GetTimestamp();
        _stopAtSec = RecordSec;
        On = true;
        UnityEngine.Application.logMessageReceived -= OnUnityLog;
        UnityEngine.Application.logMessageReceived += OnUnityLog;
        Line("录制开始: " + _tag);
    }

    // 060001e2.
    private static void AutoStop()
    {
        if (On && T >= _stopAtSec) Stop("到时");
    }

    // 060001e3. WriteFile is the separate export-owner prerequisite, without a fallback here.
    public static string Stop(string why = "手动")
    {
        if (!On) return null;

        Line("录制结束: " + why);
        On = false;
        UnityEngine.Application.logMessageReceived -= OnUnityLog;
        string path = WriteFile();
        LastPath = path;
        return path;
    }

    // 060001e4.
    public static void Toggle()
    {
        if (On)
        {
            string path = Stop("手动");
            Say(path != null ? "追踪已保存: " + path : "追踪已停止");
        }
        else
        {
            Begin("手动");
            Say(string.Format("追踪开始录制({0:F0}s 后自动保存)", RecordSec));
        }
    }

    // 060001e5. The managed-object catch covers chat only, not the Debug warning.
    private static void Say(string s)
    {
        Debug.LogWarning("[追踪] " + s);
        try
        {
            ChatUI.Add("系统", "[追踪] " + s,
                new Color(160f / 255f, 220f / 255f, 160f / 255f, 1f), "", null);
        }
        catch
        {
        }
    }

    // 060001e7.
    private static void OnUnityLog(string msg, string stack, LogType type)
    {
        if (type == LogType.Log || !On || string.IsNullOrEmpty(msg)) return;

        string key = msg.Length > 90 ? msg.Substring(0, 90) : msg;
        (int, string, LogType) prior;
        _logs.TryGetValue(key, out prior);
        _logs[key] = (unchecked(prior.Item1 + 1), prior.Item2 ?? msg, type);
        if (prior.Item1 != 0) return;

        Line(string.Format("[{0}] {1}", type,
            msg.Length > 200 ? msg.Substring(0, 200) + "…" : msg));
    }

    // 060001f9. The official body leaves segment totals, SR and wall time untouched.
    private static void ResetAll()
    {
        _phases.Clear();
        _pkgN = 0;
        _pkgPack = 0L;
        _pkgOrig = 0L;
        _pkgReadMs = 0f;
        _pkgInflMs = 0f;
        _pkgBy.Clear();
        _pkgTop.Clear();
        _decPxN = 0;
        _decGeoN = 0;
        _decPxMs = 0f;
        _decGeoMs = 0f;
        _decPxBytes = 0L;
        _avN = 0;
        _avMs = 0f;
        _avTop.Clear();
        _gridRows.Clear();
        _gridMainMs = 0f;
        _upN = 0;
        _upBytes = 0L;
        _netN = 0;
        _netBytes = 0L;
        _netMs = 0f;
        _netBy.Clear();
        _backlogPeak = 0;
        _spPlayers = 0;
        _spPets = 0;
        _spMobs = 0;
        _frames = 0;
        _maxFrameMs = 0f;
        _hitches = 0;
        _hitchTop.Clear();
        _gcPrev = -1;
        _gcTotal = 0;
        _logs.Clear();
        _frNet = 0;
        _frNetMs = 0f;
        _frUp = 0;
        _frUpBytes = 0L;
        _frAv = 0;
        CombatWorld.MoveRecv = 0L;
        CombatWorld.MoveSelf = 0L;
        CombatWorld.MovePlayer = 0L;
        CombatWorld.MoveMob = 0L;
        CombatWorld.MoveNoTarget = 0L;
        CombatWorld.MoveNoWorld = 0L;
        CombatWorld.NoTargetSample.Clear();
    }
}

}

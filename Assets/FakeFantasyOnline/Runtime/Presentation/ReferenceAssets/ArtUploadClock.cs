using System.Diagnostics;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static class ArtUploadClock
{
    public const float FrameFrac = 0.24f;
    private const float MinMs = 1.5f;

    private static int _frame = -1;
    private static readonly Stopwatch _sw = new Stopwatch();

    // 0x06000b7c
    public static void BeginFrame()
    {
        if (_frame == Time.frameCount) return;
        _frame = Time.frameCount;
        _sw.Reset();
    }

    // 0x06000b7d
    public static void Resume() => _sw.Start();

    // 0x06000b7e
    public static void Pause() => _sw.Stop();

    // 0x06000b7f
    public static bool Over =>
        _sw.Elapsed.TotalMilliseconds >= Mathf.Max(MinMs, GameSettings.TargetFrameMs * FrameFrac);

    // 0x06000b80
    public static double SpentMs => _sw.Elapsed.TotalMilliseconds;
}

}

using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class GameSettings
{
    public static int FrameRateMode = 1;
    public static float TargetFrameMs { get; private set; } = 16.666666f;

    public static void ApplyFrameRate()
    {
        EnsureLoaded();
        float hz = (float)Screen.currentResolution.refreshRateRatio.value;

        // Reject invalid device refresh reports before converting them to a divisor.
        if (float.IsInfinity(hz)) hz = 60f;

        // Historical baseline below tests NaN, not Infinity; the guard above intentionally
        // hardens that baseline for local display configuration. Official 1.5.8 token 06000e12:
        // arm64 06000e12.c:43-50 sets bVar1 = true (the 60f default) and only overwrites it with
        // `hz < 30f` when `(uint)ABS(hz) < 0x7f800001` (pcode op 81, constant 0x7f800001) holds:
        // that bit test is false for NaN only, so NaN keeps the 60f default while +Infinity takes
        // the `hz < 30f` arm and passes through unchanged. x86_64 06000e12.c:49-62 is the same
        // select in SSE form: auVar10 = (ABS(hz) < 0x7f800001) ? hz : 60f, then
        // blendvps(auVar10, 60f, mask = hz < 30f), i.e. NaN -> 60f and Infinity -> hz.
        if (float.IsNaN(hz) || hz < 30f) hz = 60f;
        int divisor;
        if (FrameRateMode == 0) divisor = Mathf.Clamp((int)Math.Round((double)(hz / 60f)), 1, 4);
        else if (FrameRateMode == 1) divisor = Mathf.Clamp((int)Math.Round((double)(hz / 120f)), 1, 4);
        else divisor = 1;
        QualitySettings.vSyncCount = divisor;
        UnityEngine.Application.targetFrameRate = FrameRateMode == 0 ? 60 : FrameRateMode == 1 ? 120 : -1;
        // Android ignores vSyncCount; -1 means 30 fps there, not display refresh.
        if (UnityEngine.Application.isMobilePlatform && FrameRateMode == 2)
            UnityEngine.Application.targetFrameRate = Mathf.RoundToInt(hz);
        // Official write order is `fVar = 30f; if (30f <= hz / divisor) fVar = hz / divisor;`
        // (arm64 06000e12.c:164-168, x86_64 06000e12.c:174-178). The comparison is written
        // against the quotient first, so the argument order is Mathf.Max(hz / divisor, 30f):
        // identical for every reachable value and 30f instead of NaN if the quotient is ever NaN.
        TargetFrameMs = 1000f / Mathf.Max(hz / divisor, 30f);
    }
}

}

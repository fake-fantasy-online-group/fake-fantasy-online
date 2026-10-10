using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official owner for integral art scale and the shared art-memory budget multiplier.
/// </summary>
public static class ArtDetail
{
    public const bool FollowCameraScale = true;
    public const bool Use4444Upload = true;

    public static int PixelScale { get; private set; }

    public static int AppliedScale { get; private set; }

    public static bool PreferNativeArt => AppliedScale < 2;

    public static float BudgetScale => AppliedScale < 2 ? 1f : 2f;

    public static event Action OnBudgetScaleChanged;

    public static void Adopt()
    {
        if (AppliedScale == PixelScale)
        {
            return;
        }

        int old = AppliedScale;
        AppliedScale = PixelScale;
        Debug.LogWarning(string.Format(
            "[美术档] 实际档次 {0}→{1}(相机 n={2}) ⇒ 怪/宠此后读{3}; 缓存预算×{4:0.0}。",
            old,
            AppliedScale,
            PixelScale,
            AppliedScale > 1 ? "HD件" : "原生1×",
            BudgetScale));
        LoadTrace.Line(string.Format(
            "★美术实际档次 {0}→{1}, 缓存预算×{2:0.0}",
            old,
            AppliedScale,
            BudgetScale));
        OnBudgetScaleChanged?.Invoke();
    }

    public static bool NeedMip(int scale)
    {
        return scale > Math.Max(PixelScale, 1);
    }

    public static void SetPixelScale(int n)
    {
        n = Math.Max(1, Math.Min(8, n));
        if (n == PixelScale)
        {
            return;
        }

        int old = PixelScale;
        PixelScale = n;
        Debug.LogWarning(string.Format(
            "[美术档] 相机像素缩放级 {0}→{1}(实际档次仍 {2}, 下次过图后生效)。",
            old,
            n,
            AppliedScale));
        LoadTrace.Line(string.Format(
            "★相机像素缩放级 {0}→{1}(实际档次 {2}, 待过图生效)",
            old,
            n,
            AppliedScale));
    }

    static ArtDetail()
    {
        PixelScale = 1;
        AppliedScale = 1;
    }
}

}

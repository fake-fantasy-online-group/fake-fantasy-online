using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Historical HP arithmetic and remaining monster-name layout helpers. Live HP views use
/// HeadHpBar directly; the monster-name owner remains separate recovery.
/// </summary>
internal static class ReferenceWorldHeadHpBarMath
{
    public const float SpritePixelsPerUnit = 100f;
    public const float PlayerOffsetPixels = 29f;

    public static int Percentage(int health, int maximumHealth)
    {
        if (maximumHealth <= 0)
        {
            return 0;
        }

        int clamped = Math.Max(0, Math.Min(health, maximumHealth));
        return clamped * 100 / maximumHealth;
    }

    public static int ColorIndex(int percentage)
    {
        return Math.Min(Math.Max(0, percentage) / 33, 2);
    }

    public static int FillWidth(int logicalWidth, int percentage)
    {
        if (logicalWidth <= 0)
        {
            return 0;
        }

        int width = logicalWidth * percentage / 100;
        return Math.Max(0, Math.Min(width, logicalWidth));
    }

    public static float PixelSnap(float value, float pixelsPerUnit)
    {
        if (!(pixelsPerUnit > 0f) || float.IsNaN(value) || float.IsInfinity(value))
        {
            return value;
        }

        return Mathf.Round(value * pixelsPerUnit) / pixelsPerUnit;
    }

    public static float ScreenScale(
        float pixelsPerUnit,
        float uiScale,
        bool orthographic,
        float orthographicSize,
        int screenHeight)
    {
        if (!(uiScale > 0f) || float.IsNaN(uiScale) || float.IsInfinity(uiScale))
        {
            return 1f;
        }

        if (!orthographic || !(orthographicSize > 0f) || screenHeight < 1)
        {
            return uiScale;
        }

        float screenPixelsPerWorldUnit = screenHeight / (orthographicSize * 2f);
        return screenPixelsPerWorldUnit > 0f
            ? pixelsPerUnit * uiScale / screenPixelsPerWorldUnit
            : uiScale;
    }
}

/// <summary>
/// Borrowed access to the original process-wide HeadHpBar resources. The legacy asset handle
/// remains available to existing Editor callers; it does not preload or own a second cache.
/// </summary>
internal sealed class ReferenceWorldHeadHpBarAssets : IDisposable
{
    public const string ResourcePath = "res/effect/hpbar.gsa";
    public const int RequiredFrameCount = 4;
    public int TextureScale => HeadHpBar.TextureScaleForIndependentRuntime;
    public int Width => HeadHpBar.W;
    public int Height => HeadHpBar.H;
    public Sprite Groove => HeadHpBar.Groove();
    public Material Material => HeadHpBar.Mat();

    public static bool TryCreate(ReferenceResourceLoader resources, out ReferenceWorldHeadHpBarAssets assets)
    {
        assets = new ReferenceWorldHeadHpBarAssets();
        return true;
    }

    public Sprite Fill(int colorIndex, int width) => HeadHpBar.Fill(colorIndex, width);

    public void Dispose()
    {
        // The original static cache has process lifetime; this handle borrows its resources.
    }
}

/// <summary>
/// Historical signature adapter. The original view owns all state and consumes its own shared
/// Show and placement owners; old option/camera arguments do not introduce a second decision path.
/// </summary>
internal sealed class ReferenceWorldHeadHpBarView : IDisposable
{
    public const int BackgroundSortingOrder = 31990;
    public const int ForegroundSortingOrder = 31991;
    private readonly HeadHpBarView owner = new HeadHpBarView();

    public ReferenceWorldHeadHpBarView() { }
    public ReferenceWorldHeadHpBarView(ReferenceWorldHeadHpBarAssets assets) { }
    public GameObject Root => owner.Root;
    public SpriteRenderer Background => owner.Background;
    public SpriteRenderer Foreground => owner.Foreground;
    public int LastColorIndex => owner.LastColorIndex;
    public int LastWidth => owner.LastWidth;
    internal bool Configure(ReferenceWorldHeadHpBarAssets value) => value != null;

    public bool TryTick(Transform parent, bool show, bool headHpBarEnabled, int health,
        int maximumHealth, float localY, float offX, Camera worldCamera, float uiScale, int screenHeight)
    {
        owner.Tick(parent, show, health, maximumHealth, localY, offX);
        return true;
    }

    public void Dispose() => owner.Dispose();
}

}

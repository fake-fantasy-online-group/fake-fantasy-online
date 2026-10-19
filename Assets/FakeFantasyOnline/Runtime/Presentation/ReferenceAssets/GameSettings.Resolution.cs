using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class GameSettings
{
    public static readonly (int, int)[] ResOpts =
    {
        (800, 600), (1024, 768), (1280, 800), (1600, 900),
        (1920, 1080), (2560, 1440), (3840, 2160)
    };

    public static int ResIndex()
    {
        EnsureLoaded();
        int sw = displayConfig.Width;
        int sh = displayConfig.Height;
        int index = Array.FindIndex(ResOpts, option => option.Item1 == sw && option.Item2 == sh);
        return index < 0 ? 1 : index;
    }

    public static void ApplyRes(int index)
    {
        EnsureLoaded();
        if (index < 0 || index >= ResOpts.Length) return;
        var option = ResOpts[index];
        SaveLocalDisplayResolution(option.Item1, option.Item2);
        ApplyResolution(option.Item1, option.Item2);
    }

    public static void ApplyResolution(int w, int h)
    {
        if (w <= 0 || h <= 0) return;
        int displayWidth = Display.main.systemWidth;
        int displayHeight = Display.main.systemHeight;
        if (displayWidth > 0 && displayHeight > 0 && (w >= displayWidth || h >= displayHeight))
        {
            Screen.SetResolution(displayWidth, displayHeight, FullScreenMode.FullScreenWindow, 0);
            return;
        }
        if (UnityEngine.Application.isMobilePlatform)
        {
            Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow, 0);
            return;
        }
        if (WindowCenter.TryGetMaxClient(out int maxWidth, out int maxHeight))
        {
            w = Math.Min(w, maxWidth);
            h = Math.Min(h, maxHeight);
        }
        Screen.SetResolution(w, h, FullScreenMode.Windowed, 0);
    }

    public static int DpiK() => 1;
}

}

using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Pure presentation math. No combat, coroutines, wall clock, random seed or replay cursor.</summary>
internal static class RiftEliteVisualPlan
{
    internal const int IceChargeMS = 3000, LavaFrameMS = 70, IceFrameMS = 60;
    internal const float LavaCoreDiameterRatio = .30f;
    internal static int LoopFrame(double ageMS, int frameMS, int count)
        => ageMS < 0 || count <= 0 || frameMS <= 0 ? -1 : (int)(Math.Floor(ageMS / frameMS) % count);
    internal static int OnceFrame(double ageMS, int frameMS, int count)
        => ageMS < 0 || ageMS >= (double)frameMS * count || count <= 0 || frameMS <= 0 ? -1 : (int)(ageMS / frameMS);
    internal static float IceChargeScale(double ageMS)
        => (float)(.46 + .34 * (.5 - .5 * Math.Cos(2 * Math.PI * Math.Max(0, ageMS) / 1000)));
    internal static float LavaCoreScale(float radius, float nativeVisibleWidth)
        => nativeVisibleWidth <= 0 ? 0 : 2 * radius * LavaCoreDiameterRatio / nativeVisibleWidth;
    internal static float DiameterScale(float radius, float nativeVisibleWidth)
        => nativeVisibleWidth <= 0 ? 0 : 2 * radius / nativeVisibleWidth;
    internal static bool Ring(double ageMS, long firstPulseMS, long pulseMS, long propagationMS,
        float maximumRadius, float width, out float inner, out float outer, out double pulseAge)
    {
        inner = outer = 0; pulseAge = -1;
        if (ageMS < firstPulseMS || firstPulseMS <= 0 || pulseMS <= 0 || propagationMS <= 0) return false;
        pulseAge = (ageMS - firstPulseMS) % pulseMS;
        if (pulseAge > propagationMS) return false;
        outer = maximumRadius * (float)(pulseAge / propagationMS);
        inner = Math.Max(0, outer - width);
        return outer > 0;
    }
    internal static void Wall(double x, double y, double x2, double y2, double width,
        out double centerX, out double centerY, out double length, out double angleDegrees)
    {
        centerX = (x + x2) / 2; centerY = (y + y2) / 2;
        length = Math.Sqrt((x2 - x) * (x2 - x) + (y2 - y) * (y2 - y));
        // Map Y points down; world Y points up.
        angleDegrees = Math.Atan2(-(y2 - y), x2 - x) * 180 / Math.PI;
    }
    internal static int MapOrder(double mapY) => (int)Math.Max(0, Math.Min(30000, mapY));
}
}

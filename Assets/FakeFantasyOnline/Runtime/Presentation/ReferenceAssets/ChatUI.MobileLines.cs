using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    // The HUD owns one reusable destination. Retain at most that one list; a new
    // owner/destination rebuilds rather than growing a cache of destroyed views.
    private static List<Line> mobileLinesCacheDestination;
    private static LineCacheKey mobileLinesCacheKey;
    private static int mobileLinesCacheCount;
    // Preserve the existing chronological stream and character-specific channel filter.
    // System feedback belongs in the compact stream too, not in an undrawn partition.
    // This reads existing messages only; it does not emit or restore disabled broadcasts.
    internal static void CollectMobileLines(List<Line> destination)
    {
        LoadFilter();
        LineCacheKey key = CurrentLineCacheKey;
        if (ReferenceEquals(destination, mobileLinesCacheDestination) &&
            destination.Count == mobileLinesCacheCount && mobileLinesCacheKey.Equals(key)) return;
        destination.Clear();
        foreach (Line line in chatLines)
            if (ChanShown(line.ch)) destination.Add(line);
        mobileLinesCacheDestination = destination;
        mobileLinesCacheKey = key;
        mobileLinesCacheCount = destination.Count;
    }
}
}

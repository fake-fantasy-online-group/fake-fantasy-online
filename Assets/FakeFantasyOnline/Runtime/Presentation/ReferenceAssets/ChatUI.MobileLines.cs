using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    // Preserve the existing chronological stream and character-specific channel filter.
    // System feedback belongs in the compact stream too, not in an undrawn partition.
    // This reads existing messages only; it does not emit or restore disabled broadcasts.
    internal static void CollectMobileLines(List<Line> destination)
    {
        LoadFilter();
        destination.Clear();
        foreach (Line line in chatLines)
            if (ChanShown(line.ch)) destination.Add(line);
    }
}
}

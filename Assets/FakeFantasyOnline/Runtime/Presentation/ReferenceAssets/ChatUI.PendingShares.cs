using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    // Original public static accessor; the view observes this same pending-reference root.
    public static bool HasPendingShares => pendingRefs.Count > 0;

    // The manual view borrows the original static root; it owns no list and no reset.
    internal static List<(byte, byte, int, string)> PendingSharesForIndependentRuntime
        => pendingRefs;
}
}

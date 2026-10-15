using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    internal void ClearResolvedAddressRetryNotice(string obsoleteNotice)
    {
        // The resolver owns this exact message instance. A newer login result, server
        // notice or local validation error must survive, even if its text is identical.
        if (obsoleteNotice != null && ReferenceEquals(netMsg, obsoleteNotice))
            netMsg = string.Empty;
    }
}
}

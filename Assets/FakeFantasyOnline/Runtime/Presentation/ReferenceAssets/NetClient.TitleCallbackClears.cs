using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // These existing release capabilities address this core's paired Protocol slots.
    // The title registers them with its callbacks; OnDestroy resolves the current core.
    internal Action clearErrorReceipt;
    internal Action clearLoginResultReceipt;
    internal Action clearRosterReceipt;
    internal Action clearCreateResultReceipt;
    internal Action clearEnterWorldReceipt;
    internal Action clearAccountResultsReceipt;
}
}

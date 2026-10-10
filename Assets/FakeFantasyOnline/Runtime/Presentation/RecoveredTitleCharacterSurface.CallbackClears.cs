using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
internal sealed partial class RecoveredTitleCharacterSurface
{
    // Official OnDestroy clears Error before Login and Register/Reset after EnterWorld.
    // The existing slot setters preserve that order; Notice is deliberately not cleared.
    internal void BindErrorReceiptReleaseForIndependentRuntime(Action clear) =>
        NetClient.I.clearErrorReceipt = clear;

    internal void BindAccountResultsReceiptReleaseForIndependentRuntime(Action clear) =>
        NetClient.I.clearAccountResultsReceipt = clear;
}
}

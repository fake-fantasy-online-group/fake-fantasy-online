using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
internal sealed partial class RecoveredTitleCharacterSurface
{
    internal void BindEnterWorldReceiptReleaseForIndependentRuntime(Action clear) =>
        NetClient.I.clearEnterWorldReceipt = clear;
}
}

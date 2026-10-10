using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
internal sealed partial class RecoveredTitleCharacterSurface
{
    internal void BindLoginResultReceiptReleaseForIndependentRuntime(Action clear) =>
        NetClient.I.clearLoginResultReceipt = clear;

    internal void BindCreateResultReceiptReleaseForIndependentRuntime(Action clear) =>
        NetClient.I.clearCreateResultReceipt = clear;
}
}

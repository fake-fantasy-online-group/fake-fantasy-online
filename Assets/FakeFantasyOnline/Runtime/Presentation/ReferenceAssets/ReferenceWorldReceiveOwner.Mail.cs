using System;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceWorldReceiveOwner : IMailReceiveOwner
{
    // Explicit composition-root projection of the same session owner used by contextual Pump.
    // A default/data-only host has no native owner and reports that fact to the receipt record.
    public Func<IMailReceiveOwner> MailOwnerAtReceiptForIndependentRuntime { private get; set; }

    public bool TryReceiveMailList(WorldMailState state) =>
        MailOwnerAtReceiptForIndependentRuntime?.Invoke()?.TryReceiveMailList(state) ?? false;

    public bool TryReceiveMailNew(bool hasNew) =>
        MailOwnerAtReceiptForIndependentRuntime?.Invoke()?.TryReceiveMailNew(hasNew) ?? false;
}

}

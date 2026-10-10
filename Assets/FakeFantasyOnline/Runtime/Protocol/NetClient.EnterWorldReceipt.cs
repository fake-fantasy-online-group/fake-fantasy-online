using System;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Original OnEnterWorld (+0x60). The existing lossless entry record is the host carrier;
    // this mutable slot connects receipts without a pending task to the live title owner.
    public Action<WorldEntryRecord> OnEnterWorld;
}
}

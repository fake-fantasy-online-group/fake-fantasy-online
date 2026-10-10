using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official instance slot +0x50, field 040009c1. The existing application record is the
    // Unity-free CharBrief carrier; this is one mutable callback, not a subscription list.
    public Action<IReadOnlyList<ServerCharacterRecord>> OnCharList;
}
}

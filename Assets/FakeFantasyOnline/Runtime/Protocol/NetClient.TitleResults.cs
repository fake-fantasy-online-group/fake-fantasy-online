using System;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Original live Action<int,string> slots: +0x38 and +0x58. Dispatch loads each
    // before its I32/Str reads; these are fields, not per-request callback captures.
    public Action<int, string> OnLoginResult;
    public Action<int, string> OnCreateResult;
}
}

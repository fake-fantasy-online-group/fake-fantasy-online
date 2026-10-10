using System;

namespace FakeFantasyOnline.Application
{
public interface IWorldBuffReader
{
    int I32();
    int U8();
    int U16();
    string Str();
    int Remaining { get; }
}

public interface IWorldBuffReceiveOwner
{
    // Null overrides use the real native clock/map suppliers without delegate allocation.
    // Explicit compatibility callers may supply their existing diagnostic capabilities.
    // The return value describes the all-row observer's Empty identity, not the filtered list.
    bool ReceiveBuffState(IWorldBuffReader reader, Func<float> clock, Func<bool> customMap);
}
}

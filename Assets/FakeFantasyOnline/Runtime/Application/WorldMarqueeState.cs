using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.Application
{

public interface IWorldMarqueeReceiveOwner
{
    void ReceiveMarquee(WorldMarqueeState state);
}

public sealed class WorldMarqueeState
{
    public WorldMarqueeState(byte mode, List<ValueTuple<int, string>> lines,
        int speed, int intervalSeconds, byte enabled, int periodMinutes)
    {
        Mode = mode;
        Lines = new ReadOnlyCollection<ValueTuple<int, string>>(new List<ValueTuple<int, string>>(lines));
        Speed = speed;
        IntervalSeconds = intervalSeconds;
        Enabled = enabled;
        PeriodMinutes = periodMinutes;
    }

    public byte Mode { get; }
    public IReadOnlyList<ValueTuple<int, string>> Lines { get; }
    public int Speed { get; }
    public int IntervalSeconds { get; }
    public byte Enabled { get; }
    public int PeriodMinutes { get; }
}

}

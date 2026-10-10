using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceWorldReceiveOwner : IWorldMarqueeReceiveOwner
{
    // Dispatch 0600077f: mode zero tail 258fa4..258fc8 (x86_64 267bbe..267be6);
    // nonzero 255e10..255ea0 (x86_64 265489..26550d), settings precede every announcement.
    public void ReceiveMarquee(WorldMarqueeState state)
    {
        var lines = new List<ValueTuple<int, string>>(state.Lines);
        if (state.Mode == 0)
        {
            RollMarquee.SetServerPool(lines, state.Speed, state.IntervalSeconds,
                state.Enabled, state.PeriodMinutes);
        }
        else
        {
            RollMarquee.SetSettings(state.Speed, state.IntervalSeconds,
                state.Enabled, state.PeriodMinutes);
            foreach (ValueTuple<int, string> line in lines)
                RollMarquee.Announce(line.Item2);
        }
    }
}

}

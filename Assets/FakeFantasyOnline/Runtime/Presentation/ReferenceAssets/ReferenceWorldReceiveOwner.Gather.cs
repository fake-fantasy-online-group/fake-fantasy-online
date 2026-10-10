using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceWorldReceiveOwner : IWorldGatherReceiveOwner
{
    // The existing composition-root owner is available before any world is built.
    // Preserve the one Dispatch tail call; no frame replay or second gather cache.
    public void ReceiveGatherSpots(List<ValueTuple<int, int, int, int, int>> zones)
    {
        GameBootstrap.OnNetGatherSpots(zones);
    }
}

}

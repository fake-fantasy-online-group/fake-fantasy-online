using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{

/// <summary>
/// Synchronous host-thread port for the official network gather-zone replacement.
/// Tuple order is kind, x1, y1, x2, y2; the port adds no snapshot or map filter.
/// </summary>
public interface IWorldGatherReceiveOwner
{
    void ReceiveGatherSpots(List<ValueTuple<int, int, int, int, int>> zones);
}

}

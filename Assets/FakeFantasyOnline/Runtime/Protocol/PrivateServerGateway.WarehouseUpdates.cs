using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : IWarehouseUpdateSource
    {
        private readonly Queue<WorldWarehouseUpdate> _warehouseUpdates = new Queue<WorldWarehouseUpdate>();

        public bool TryDequeueWarehouseUpdate(out WorldWarehouseUpdate update)
        {
            lock (_worldStateSync)
            {
                if (_warehouseUpdates.Count == 0) { update = null; return false; }
                update = _warehouseUpdates.Dequeue();
                return true;
            }
        }

        private void PublishWarehouseDelta(WorldWarehouseState delta, CancellationToken cancellationToken)
        {
            lock (_worldStateSync)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _warehouseUpdates.Enqueue(WorldWarehouseUpdate.PatchHeader(delta.PageCount, delta.Money));
                List<WorldWarehouseItemState> items = new List<WorldWarehouseItemState>(_warehouseState.Items);
                foreach (WorldWarehouseItemState item in delta.Items)
                {
                    // The callback keeps the original command, including deletion rows.
                    _warehouseUpdates.Enqueue(WorldWarehouseUpdate.PatchSlot(item));
                    int index = items.FindIndex(previous => previous.Tab == item.Tab && previous.Slot == item.Slot);
                    if (item.ItemId <= 0)
                    {
                        if (index >= 0) items.RemoveAt(index);
                    }
                    else if (index >= 0) items[index] = item;
                    else items.Add(item);
                }
                Volatile.Write(ref _warehouseState, new WorldWarehouseState(delta.PageCount, delta.Money, items));
                Interlocked.Increment(ref _warehouseStateVersion);
            }
        }
    }
}

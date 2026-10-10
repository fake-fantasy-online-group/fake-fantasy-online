namespace FakeFantasyOnline.Application
{
    public enum WorldWarehouseUpdateKind { SetData, PatchHeader, PatchSlot }

    // Preserve native callback order, including repeated slot changes between GUI frames.
    public sealed class WorldWarehouseUpdate
    {
        private WorldWarehouseUpdate(WorldWarehouseUpdateKind kind, byte pageCount, long money,
            WorldWarehouseState state, WorldWarehouseItemState item)
        { Kind = kind; PageCount = pageCount; Money = money; State = state; Item = item; }

        public WorldWarehouseUpdateKind Kind { get; }
        public byte PageCount { get; }
        public long Money { get; }
        public WorldWarehouseState State { get; }
        public WorldWarehouseItemState Item { get; }

        public static WorldWarehouseUpdate SetData(WorldWarehouseState state) =>
            new WorldWarehouseUpdate(WorldWarehouseUpdateKind.SetData, state.PageCount, state.Money, state, null);
        public static WorldWarehouseUpdate PatchHeader(byte pageCount, long money) =>
            new WorldWarehouseUpdate(WorldWarehouseUpdateKind.PatchHeader, pageCount, money, null, null);
        public static WorldWarehouseUpdate PatchSlot(WorldWarehouseItemState item) =>
            new WorldWarehouseUpdate(WorldWarehouseUpdateKind.PatchSlot, 0, 0, null, item);
    }

    public interface IWarehouseUpdateSource
    {
        bool TryDequeueWarehouseUpdate(out WorldWarehouseUpdate update);
    }
}

using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int, int, int> writeItemMove;
    private Action<int, int, int, int, int, bool> writeDropItem;
    internal void BindInventoryPlacementForIndependentRuntime(Action<int, int, int> move,
        Action<int, int, int, int, int, bool> drop)
    { writeItemMove = move; writeDropItem = drop; }
    public void ItemMoveForIndependentRuntime(int tab, int from, int to) => writeItemMove(tab, from, to);
    public void DropItemForIndependentRuntime(int tab, int slot, int x, int y, int itemId, bool confirmed = false)
        => writeDropItem(tab, slot, x, y, itemId, confirmed);
}
}

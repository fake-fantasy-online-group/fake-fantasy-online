namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 1.5.8 0x06000729; callback 0x060007a7 has an empty body.
    public void BuyWarehouse() => Send(0x102f, b => { });

    public void WarehouseMove(int dir, int fromTab, int fromSlot, int whTab, int count, int toSlot = -1)
    {
        Send(0x102D, b =>
        {
            b.U8(unchecked((byte)dir));
            b.U8(unchecked((byte)fromTab));
            b.I32(fromSlot);
            b.U8(unchecked((byte)whTab));
            b.I32(count);
            b.I32(toSlot);
        });
    }
    // Official 0x06000740, closure 0x0600082f: one U8 warehouse tab.
    public void SortWarehouse(int tab) => Send(0x1087, b => b.U8(unchecked((byte)tab)));
    public void WarehouseMoney(int mode, long amount) => Send(ProtocolOpcodes.WarehouseMoneyRequest, b => { b.U8(unchecked((byte)mode)); b.I64(amount); });
    public void WarehouseSplit(int whTab, int slot, int count) => Send(ProtocolOpcodes.WarehouseSplitRequest, b => { b.U8(unchecked((byte)whTab)); b.I32(slot); b.I32(count); });
    public void WarehouseRearrange(int fromTab, int fromSlot, int toTab, int toSlot) => Send(ProtocolOpcodes.WarehouseRearrangeRequest, b => { b.U8(unchecked((byte)fromTab)); b.I32(fromSlot); b.U8(unchecked((byte)toTab)); b.I32(toSlot); });
}
}

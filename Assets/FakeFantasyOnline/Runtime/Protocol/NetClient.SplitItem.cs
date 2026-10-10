namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 1.5.8 0600074b; the three Int32 captures belong to 06000840/06000841.
    public void SplitItem(int tab, int slot, int count)
    {
        Send(ProtocolOpcodes.SplitInventoryItemRequest, b =>
        {
            b.U8(tab);
            b.I32(slot);
            b.I32(count);
        });
    }
}
}

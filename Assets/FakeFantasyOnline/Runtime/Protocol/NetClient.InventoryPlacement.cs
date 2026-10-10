namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 1.5.8 0600073e / closure 0600082b: U8 tab, I32 from, I32 to.
    public void ItemMove(int tab, int from, int to)
    {
        Send(0x100d, b =>
        {
            b.U8(unchecked((byte)tab));
            b.I32(from);
            b.I32(to);
        });
    }

    // Official 1.5.8 0600074a / closure 0600083f; confirmation is the final U8.
    public void DropItem(int tab, int slot, int x, int y, int itemId, bool confirmed = false)
    {
        Send(0x100f, b =>
        {
            b.U8(unchecked((byte)tab));
            b.I32(slot);
            b.I32(x);
            b.I32(y);
            b.I32(itemId);
            b.U8(confirmed ? (byte)1 : (byte)0);
        });
    }
}
}

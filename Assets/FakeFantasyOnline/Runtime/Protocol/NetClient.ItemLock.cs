namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    public void SetItemLock(int tab, int slot, bool on, string secCode = "", int itemId = 0)
    {
        Send(0x1080, b =>
        {
            b.U8(unchecked((byte)tab));
            b.I32(slot);
            b.U8(on ? (byte)1 : (byte)0);
            b.Str(secCode ?? "");
            b.I32(itemId);
        });
    }
}
}

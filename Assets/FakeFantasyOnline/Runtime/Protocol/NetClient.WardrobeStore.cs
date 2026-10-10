namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    public void WardrobeStore(byte tab, int slot) => Send(0x1054, b =>
    {
        b.U8(tab);
        b.I32(slot);
    });
}
}

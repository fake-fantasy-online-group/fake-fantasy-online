namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    public void WashAffix(byte kind, byte tab, int slot, int index) =>
        Send(0x105c, b =>
        {
            b.U8(kind); b.U8(tab); b.I32(slot); b.I32(index);
        });
}
}

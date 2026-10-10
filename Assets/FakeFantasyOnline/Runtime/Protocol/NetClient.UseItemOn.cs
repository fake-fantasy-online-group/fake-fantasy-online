namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    public void UseItemOn(int toolId, byte targetKind, byte tab, int slot) =>
        Send(0x105b, b =>
        {
            b.I32(toolId);
            b.U8(targetKind);
            b.U8(tab);
            b.I32(slot);
        });
}
}

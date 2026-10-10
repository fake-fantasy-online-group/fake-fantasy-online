namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    public void ApplyAvatar(byte bagTab, int bagSlot, int targetSlot, byte kind = 0, byte targetKind = 1, byte targetTab = 0) =>
        Send(0x1036, b =>
        {
            b.U8(bagTab); b.I32(bagSlot); b.I32(targetSlot);
            b.U8(kind); b.U8(targetKind); b.U8(targetTab);
        });
}
}

namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    public void EquipFuse(byte op, byte mainKind, byte mainTab, int mainSlot, byte subTab, int subSlot, int sig = 0) =>
        Send(0x1099, b =>
        {
            b.U8(op); b.U8(mainKind); b.U8(mainTab); b.I32(mainSlot);
            b.U8(subTab); b.I32(subSlot); b.I32(sig);
        });
}
}

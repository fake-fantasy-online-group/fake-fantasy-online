namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    public void Repair(byte mode, int slot, byte targetKind = 1, byte tab = 0) =>
        Send(0x101d, b => { b.U8(mode); b.I32(slot); b.U8(targetKind); b.U8(tab); });
    public void RepairConfirm(byte mode, int slot, byte targetKind = 1, byte tab = 0) =>
        Send(0x101e, b => { b.U8(mode); b.I32(slot); b.U8(targetKind); b.U8(tab); });
}
}

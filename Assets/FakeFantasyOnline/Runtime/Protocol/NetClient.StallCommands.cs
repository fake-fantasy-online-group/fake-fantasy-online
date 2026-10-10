namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    public void StallCreate(byte type, string name) => Send(0x1060, b =>
    { b.U8(type); b.Str(name); });
    public void StallAdd(byte tab, int slot, int itemId, int count, long price) => Send(0x1062, b =>
    { b.U8(tab); b.I32(slot); b.I32(itemId); b.I32(count); b.I64(price); });
    public void StallBrowse(int ownerId) => Send(0x1064, b => b.I32(ownerId));
    public void StallEnd() => Send(0x1061, b => { });
    public void StallDel(int index) => Send(ProtocolOpcodes.StallDeleteRequest, b => b.I32(index));
    public void StallDelete(int index) => StallDel(index);
}
}

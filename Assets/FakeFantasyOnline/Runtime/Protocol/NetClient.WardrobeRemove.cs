namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 1.5.8 NetClient.WardrobeRemove 0x060006d8, the only caller of this opcode.
    // arm64 0x2670f0 (192 bytes) and x86_64 0x276570 (192 bytes) decode to the same two steps:
    // build the writer closure NetClient+<>c__DisplayClass83_0 and store the Int32 item id in it
    // (metadata_usage_03045 / method NetClient+<>c__DisplayClass83_0.<WardrobeRemove>b__0), then
    // NetClient.Send(this, 0x1056, closure). The body is the single I32 item id and the official
    // body carries no range, null or connectivity guard; the server decides admissibility.
    public void WardrobeRemove(int itemId) => Send(0x1056, b =>
    {
        b.I32(itemId);
    });
}
}

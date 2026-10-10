namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 1.5.8 NetClient.WardrobeWear 0x060006d7, the only caller of this opcode.
    // arm64 0x267028 (192 bytes) and x86_64 0x2764a0 (192 bytes) decode to the same two steps:
    // build the writer closure NetClient+<>c__DisplayClass82_0 and store the Int32 item id in it
    // (metadata_usage_03044 / method NetClient+<>c__DisplayClass82_0.<WardrobeWear>b__0), then
    // NetClient.Send(this, 0x1055, closure). The opcode literal is the immediate moved into the
    // second argument register (arm64 `mov w1,#0x1055` at 0x2670cc / x86_64 `mov esi,0x1055` at
    // 0x276535), not a value derived from a codec. The writer body is the single I32 item id and
    // the official body carries no range, null or connectivity guard; the server decides
    // admissibility, including whether the item is worn, blocked or in another category.
    public void WardrobeWear(int itemId) => Send(0x1055, b =>
    {
        b.I32(itemId);
    });
}
}

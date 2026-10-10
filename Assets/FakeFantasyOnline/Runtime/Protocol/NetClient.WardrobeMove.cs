namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 1.5.8 NetClient.WardrobeMove 0x060006d9, the only caller of this opcode.
    // arm64 0x2671b8 (204 bytes) and x86_64 0x276640 (192 bytes) decode to the same three steps:
    // build the writer closure NetClient+<>c__DisplayClass84_0 and store both Int32 arguments in
    // it (`stp w21,w20,[x22,#0x10]` at 0x267240 / `mov [r12+0x10],r15d` + `mov [r12+0x14],r14d` at
    // 0x2766a9), then NetClient.Send(this, 0x107c, closure) with the opcode immediate
    // (arm64 `mov w1,#0x107c` at 0x267264 / x86_64 `mov esi,0x107c` at 0x2766df). The writer emits
    // I32(itemId) then I32(toIndex) in that order and the official body carries no range, null or
    // connectivity guard; both arguments are the values the grid captured at the drop point.
    public void WardrobeMove(int itemId, int toIndex) => Send(0x107c, b =>
    {
        b.I32(itemId);
        b.I32(toIndex);
    });
}
}

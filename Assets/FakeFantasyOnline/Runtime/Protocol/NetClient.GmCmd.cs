namespace FakeFantasyOnline.Protocol
{

public sealed partial class NetClient
{
    // Official 0x060006c8 NetClient.GmCmd(cmd): frozen arm64 body rva 0x266314 / 212 bytes and
    // x86_64 body rva 0x275710 / 192 bytes (Build/ReferenceReverseGhidra-1.5.8/queries/
    // wbc046-arm64-GmCmd-060006c8 and wbc046-x86_64-GmCmd-060006c8), which agree on every
    // observable point:
    //   * one display class (NetClient+<>c__DisplayClass67_0, TypeInfo usage 3032) is allocated,
    //     cmd is stored at +0x10 behind the write barrier and the cctor gates are the usual
    //     +0xe4 tests (usage 377 / 3032 / 9664);
    //   * NetClient.Send (0x06000772) is called with the opcode 0x1006 - the same constant as
    //     ProtocolOpcodes.GmCommandRequest - and with the writer 0x0600088c
    //     (NetClient+<>c__DisplayClass67_0.<GmCmd>b__0, MethodDef usage 9664);
    //   * the writer (arm64 rva 0x277d88 / 32 bytes) is one Buf.Str (0x0600078e) call on
    //     *(displayClass + 0x10), so cmd is captured by reference and read when the writer runs:
    //     nothing copies, trims or length-checks it on the way out.
    // The caller owns the slash: ChatUI.SendNow (0x060000f7) hands GmCmd text.Substring(1) and
    // only then prints its own 执行 row, so this command stores exactly what it is given.
    public void GmCmd(string cmd) => Send(ProtocolOpcodes.GmCommandRequest, b => b.Str(cmd));
}

}

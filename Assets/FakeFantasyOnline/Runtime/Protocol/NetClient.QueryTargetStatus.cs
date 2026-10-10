namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 1.5.8 060006f8; the writer lambda below is official 060007d7. Both architectures
    // (arm64 0x2690a4, x86_64 0x278470, 192 bytes each) allocate the display class, capture the
    // argument at +0x10 and hand one Action<Buf> to Send(0x1081, ...). No value-range test and no
    // NetClient state read exists in the official body. See Build/WBB-104-report.md for evidence.
    public void QueryTargetStatus(int targetId) =>
        Send(ProtocolOpcodes.QueryTargetStatusRequest, b => b.I32(targetId));
}
}

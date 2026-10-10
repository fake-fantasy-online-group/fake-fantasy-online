namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 1.5.8 060006cd / writer 06000896.
    public void ChangeHair(byte mode, byte targetId) =>
        Send(0x1066, b => { b.U8(mode); b.U8(targetId); });
}
}

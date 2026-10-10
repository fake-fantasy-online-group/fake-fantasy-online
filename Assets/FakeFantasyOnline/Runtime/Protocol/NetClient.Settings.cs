namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    public void SetPkMode(int mode) => Send(0x1075,
        b => b.U8((byte)(mode < 0 ? 0 : mode > 4 ? 4 : mode)));

    public void SetGlowMode(int mode) => Send(0x107b,
        b => b.U8((byte)(mode < 0 ? 0 : mode > 16 ? 16 : mode)));

    public void SetSmartCast(bool enabled) => Send(0x107f,
        b => b.U8(enabled ? (byte)1 : (byte)0));

    public void PetPickQuality(byte quality) => Send(0x1097, b => b.U8(quality));
}
}

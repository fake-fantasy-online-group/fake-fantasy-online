namespace FakeFantasyOnline.Protocol
{

public sealed partial class NetClient
{
    public void LeaveWorld()
    {
        Send(0x1049, b => { });
    }

    public void Revive(byte option)
    {
        Send(0x1018, b => WriteReviveForIndependentRuntime(b, option));
    }

    internal static void WriteReviveForIndependentRuntime(Buf b, byte option)
    {
        b.U8(option);
    }
}

}

namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 1.5.8 06000750; scalar capture and supplied-Buf writer 0600084a/0600084b.
    public void TradeRequest(int targetId)
    {
        Send(ProtocolOpcodes.TradeRequest, b => b.I32(targetId));
    }
}
}

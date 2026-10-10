namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 1.5.8 0600073f; the Int32 capture belongs to 0600082c/0600082d.
    public void SortBag(int tab)
    {
        Send(ProtocolOpcodes.SortInventoryRequest, b => b.U8(tab));
    }
}
}

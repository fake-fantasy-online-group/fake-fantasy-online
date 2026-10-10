namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 1.5.8 0600074d; 06000844/06000845 capture the unchanged Int32 target.
    public void Attack(int targetId)
    {
        Send(ProtocolOpcodes.AttackRequest, b => b.I32(targetId));
    }
}
}

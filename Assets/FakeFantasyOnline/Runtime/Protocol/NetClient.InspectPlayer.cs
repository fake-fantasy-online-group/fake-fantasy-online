namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 1.5.8 060006f7; 060007d4/060007d5 capture the unchanged Int32 target.
    public void InspectPlayer(int targetId)
    {
        Send(ProtocolOpcodes.InspectPlayerRequest, b => b.I32(targetId));
    }
}
}

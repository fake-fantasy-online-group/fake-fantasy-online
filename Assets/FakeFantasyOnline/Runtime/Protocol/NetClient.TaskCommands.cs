namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 1.5.8 0600072c..0600072f and their writers
    // 0600081b/0600081d/0600081f/06000821. Capture the original argument and
    // serialize inside Send, after its connected check, with the native Buf.
    // In particular, the NPC name is not validated or encoded in the gateway.
    public void TaskNpcList(string npcName)
    {
        Send(ProtocolOpcodes.NpcTaskOffersRequest, b => b.Str(npcName));
    }

    public void TaskAccept(int taskId)
    {
        Send(ProtocolOpcodes.TaskAcceptRequest, b => b.I32(taskId));
    }

    public void TaskAbandon(int taskId)
    {
        Send(ProtocolOpcodes.TaskAbandonRequest, b => b.I32(taskId));
    }

    public void TaskComplete(int taskId)
    {
        Send(ProtocolOpcodes.TaskCompleteRequest, b => b.I32(taskId));
    }
}
}

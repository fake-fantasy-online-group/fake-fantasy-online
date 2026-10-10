namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Extracted 0x1015 transport fragment. The complete original UseItem entry and its
    // target/capture/feedback decisions remain in Presentation.NetClient.UseItem.
    internal void SendOrdinaryItemRequestForIndependentRuntime(int itemId, byte tab = 255, int slot = -1) =>
        Send(0x1015, b => { b.I32(itemId); b.U8(tab); b.I32(slot); });
}
}

namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Extracted 0x102b transport fragment, shared by the complete original Capture entry
    // after its animation and by targeted UseItem without that animation.
    internal void SendTargetedItemRequestForIndependentRuntime(int targetId, int itemId) =>
        Send(0x102b, b => { b.I32(targetId); b.I32(itemId); });

    // Official 0x06000722: classify the capture-item id range. Both architectures compute
    // offset = id - 3382 (0xd36) and return ((uint)offset < 8) with bit (offset & 31) of the mask
    // 199 (0b11000111) set, i.e. ids 3382, 3383, 3384, 3388 and 3389 are capture items.
    public static bool IsCaptureItem(int id)
    {
        int offset = id - 3382;
        return (uint)offset < 8u && ((199 >> offset) & 1) != 0;
    }
}
}

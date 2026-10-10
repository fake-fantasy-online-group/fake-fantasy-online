namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 0x060006cb and captured writer 0x06000892. Keep the bag index as an
    // Int32; Send performs its connected check before invoking the Buf writer.
    public void EquipItem(int bagIndex)
    {
        Send(ProtocolOpcodes.EquipInventoryItemRequest, b => b.I32(bagIndex));
    }

    // Official 0x060006e6, instance System.Void UnequipItem(System.Int32 slot): one Send tail
    // call, opcode 0x1008. Writer <UnequipItem>b__0 0x060007bb is one Buf.I32 of the captured
    // value, and the capture is a four-byte store with no write barrier, i.e. by value. No slot
    // range is applied: the Int32 is forwarded unchanged. Bodies: WBD-125-060006e6-{arm64,x86_64}/
    // under the frozen Build/ReferenceReverseGhidra-1.5.8/queries/ tree.
    public void UnequipItem(int slot)
    {
        Send(ProtocolOpcodes.UnequipInventoryItemRequest, b => b.I32(slot));
    }

    // Official 0x0600071a, instance System.Void AllocPoints(System.Int32[] d7): one Send tail
    // call, opcode 0x1009. Writer <AllocPoints>b__0 0x06000803 is seven straight-line Buf.I32 of
    // d7[0]..d7[6]; the array is captured by reference - eight-byte store plus the GC write
    // barrier - so its values are read inside NetClient.Send, after its connected test. No extra
    // argument validation: the accesses keep the runtime range check native has, and a negative
    // delta is forwarded unchanged. Bodies: WBD-125-0600071a-{arm64,x86_64}/ under the frozen
    // Build/ReferenceReverseGhidra-1.5.8/queries/ tree.
    public void AllocPoints(int[] d7)
    {
        Send(ProtocolOpcodes.AllocateAttributePointsRequest, b =>
        {
            b.I32(d7[0]);
            b.I32(d7[1]);
            b.I32(d7[2]);
            b.I32(d7[3]);
            b.I32(d7[4]);
            b.I32(d7[5]);
            b.I32(d7[6]);
        });
    }
}
}

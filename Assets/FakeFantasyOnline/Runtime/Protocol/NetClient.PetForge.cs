namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Original PetForge 060006ff and writer 060007e5.
    public void PetForge(byte op, byte kind, byte petTab, int petSlot, byte itemTab,
        int itemSlot, int skillId, byte pet2Tab = 0, int pet2Slot = -1)
    {
        Send(0x1096, b =>
        {
            b.U8(op);
            b.U8(kind);
            b.U8(petTab);
            b.I32(petSlot);
            b.U8(itemTab);
            b.I32(itemSlot);
            b.I32(skillId);
            b.U8(pet2Tab);
            b.I32(pet2Slot);
        });
    }
}
}

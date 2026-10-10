using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int> writeEquipItem;

    internal void BindEquipItemWriterForIndependentRuntime(Action<int> writer) => writeEquipItem = writer;

    // Official 0x060006cb, instance System.Void NetClient.EquipItem(System.Int32).
    // The official body is the opcode 0x1007 writer (ProtocolOpcodes.EquipInventoryItemRequest) whose
    // only payload is the bag index; the Application-side gateway command is the same request, so the
    // server stays authoritative and the client predicts nothing.
    public void EquipItemForIndependentRuntime(int bagIdx) => writeEquipItem(bagIdx);

    // Official 0x06000723, static System.Boolean NetClient.IsMiningTool(System.Int32).
    // Two item ids are mining tools and therefore skip the bind confirmation: 1901 and 15034.
    public static bool IsMiningTool(int id) => id == 1901 || id == 15034;
}
}

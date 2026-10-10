using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceWorldReceiveOwner : IInspectedEquipmentReceiptOwner, IInspectedEquipmentReaderOwner
{
    public void ReceiveInspectedEquipment(WorldInspectedPlayerEquipmentState state) =>
        ExEquipDlg.ReceiveForIndependentRuntime(state);

    WorldInspectedPlayerEquipmentState IInspectedEquipmentReaderOwner.ReceiveInspectedEquipment(
        IInspectedEquipmentReader reader) => ReferenceInspectedEquipmentReceipt.Read(reader);
}

}

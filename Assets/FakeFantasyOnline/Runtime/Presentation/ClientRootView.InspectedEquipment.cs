using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{

public sealed partial class ClientRootView : IInspectedEquipmentReceiptOwner, IInspectedEquipmentReaderOwner
{
    void IInspectedEquipmentReceiptOwner.ReceiveInspectedEquipment(WorldInspectedPlayerEquipmentState state) =>
        ExEquipDlg.ReceiveForIndependentRuntime(state);

    WorldInspectedPlayerEquipmentState IInspectedEquipmentReaderOwner.ReceiveInspectedEquipment(
        IInspectedEquipmentReader reader) => ReferenceInspectedEquipmentReceipt.Read(reader);
}

}

namespace FakeFantasyOnline.Application
{

/// <summary>Host-thread execution of one successfully decoded inspected-equipment reply.</summary>
public interface IInspectedEquipmentReceiptOwner
{
    void ReceiveInspectedEquipment(WorldInspectedPlayerEquipmentState state);
}

}

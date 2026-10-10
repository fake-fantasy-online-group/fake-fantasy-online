namespace FakeFantasyOnline.Application
{
public interface IInspectedEquipmentReader
{
    int I32();
    long I64();
    int U8();
    int U16();
    int I16();
    string Str();
    int Remaining { get; }
}

public interface IInspectedEquipmentReaderOwner
{
    WorldInspectedPlayerEquipmentState ReceiveInspectedEquipment(IInspectedEquipmentReader reader);
}
}

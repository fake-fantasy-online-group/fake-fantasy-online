namespace FakeFantasyOnline.Application
{
// The one existing Buf crosses the assembly boundary without a new cursor or row DTO.
public interface IWorldTaskReceiptReader
{
    int U16();
    int I32();
    int U8();
    string Str();
}

public interface IWorldTaskReceiptOwner
{
    // The result describes only whether the received lists had rows, for the optional
    // session observation's reset. The native setter and callback run before it returns.
    bool ReceiveTaskLog(IWorldTaskReceiptReader reader);
    void ReceiveTaskOffers(IWorldTaskReceiptReader reader);
}
}

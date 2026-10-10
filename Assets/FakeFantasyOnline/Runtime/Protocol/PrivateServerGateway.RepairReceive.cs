using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private static void ReceiveRepairQuote(byte[] payload, IRepairReceiveContext context)
    {
        var reader = new NetClient.Buf(payload);
        byte mode = (byte)reader.U8();
        int slot = reader.I32();
        long fee = reader.I64();
        int count = reader.I32();
        int itemId = reader.I32();
        string info = reader.Str();
        byte targetKind = (byte)reader.U8();
        byte tab = (byte)reader.U8();
        context.ReceiveRepairQuote(mode, slot, fee, count, itemId, info, targetKind, tab);
    }
}
}

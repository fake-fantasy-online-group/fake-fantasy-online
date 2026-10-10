namespace FakeFantasyOnline.Application
{
public interface IRepairReceiveContext
{
    void ReceiveRepairQuote(byte mode, int slot, long fee, int count, int itemId, string info, byte targetKind, byte tab);
}
}

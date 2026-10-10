namespace FakeFantasyOnline.Application
{
public enum TradeReceiptFlag { MyLocked, OtherLocked, MyConfirmed, OtherConfirmed }

// Existing trade authority, mutated at the original 0x8031 read/clear/add points.
public interface ITradeStateReceiptOwner
{
    void StoreTradeFlag(TradeReceiptFlag field, bool value);
    void StoreTradeMoney(bool ownSide, long value);
    void ClearTradeItems(bool ownSide);
    void AddTradeItem(bool ownSide, int itemId, int count, string name);
    void ClearTradeItemInfo(bool ownSide);
    void AddTradeItemInfo(bool ownSide, string text, byte quality);
    void ClearTradePets(bool ownSide);
    void StoreTradePet(bool ownSide, int index, PetBrief pet);
    void StoreTradePetPoints(bool ownSide, int index, int aiUsed, int aiCapacity, int shengUsed, int shengCapacity);
    WorldActiveTradeState CaptureTradeState();
}
}

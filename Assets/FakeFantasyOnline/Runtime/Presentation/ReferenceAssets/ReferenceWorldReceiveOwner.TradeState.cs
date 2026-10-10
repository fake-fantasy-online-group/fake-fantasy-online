using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : ITradeStateReceiptOwner
{
    void ITradeStateReceiptOwner.StoreTradeFlag(TradeReceiptFlag field, bool value) => ReferenceTradeReceipt.StoreFlag(field, value);
    void ITradeStateReceiptOwner.StoreTradeMoney(bool ownSide, long value) => ReferenceTradeReceipt.StoreMoney(ownSide, value);
    void ITradeStateReceiptOwner.ClearTradeItems(bool ownSide) => ReferenceTradeReceipt.ClearItems(ownSide);
    void ITradeStateReceiptOwner.AddTradeItem(bool ownSide, int itemId, int count, string name) => ReferenceTradeReceipt.AddItem(ownSide, itemId, count, name);
    void ITradeStateReceiptOwner.ClearTradeItemInfo(bool ownSide) => ReferenceTradeReceipt.ClearItemInfo(ownSide);
    void ITradeStateReceiptOwner.AddTradeItemInfo(bool ownSide, string text, byte quality) => ReferenceTradeReceipt.AddItemInfo(ownSide, text, quality);
    void ITradeStateReceiptOwner.ClearTradePets(bool ownSide) => ReferenceTradeReceipt.ClearPets(ownSide);
    void ITradeStateReceiptOwner.StoreTradePet(bool ownSide, int index, PetBrief pet) => ReferenceTradeReceipt.StorePet(ownSide, index, pet);
    void ITradeStateReceiptOwner.StoreTradePetPoints(bool ownSide, int index, int aiUsed, int aiCapacity, int shengUsed, int shengCapacity) => ReferenceTradeReceipt.StorePetPoints(ownSide, index, aiUsed, aiCapacity, shengUsed, shengCapacity);
    WorldActiveTradeState ITradeStateReceiptOwner.CaptureTradeState() => ReferenceTradeReceipt.CaptureState();
}
}

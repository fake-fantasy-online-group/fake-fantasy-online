namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    public void OpenShop(int shopId) => Send(0x1019, b => b.I32(shopId));
    public void BuyItem(int itemId, int count) => Send(0x101a, b => { b.I32(itemId); b.I32(count); });
    public void SellItem(byte tab, int slot, int count, int itemId, bool confirmed = false) => Send(0x101b, b =>
    {
        b.U8(tab); b.I32(slot); b.I32(count); b.I32(itemId); b.U8(confirmed ? (byte)1 : (byte)0);
    });
    public void StallDeal(int ownerId, int index, int count, byte bagTab, int bagSlot, int expectItemId = 0, long expectPrice = 0) =>
        Send(0x1065, b =>
        {
            b.I32(ownerId); b.I32(index); b.I32(count); b.U8(bagTab);
            b.I32(bagSlot); b.I32(expectItemId); b.I64(expectPrice);
        });
}
}

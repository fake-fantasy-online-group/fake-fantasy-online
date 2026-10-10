namespace FakeFantasyOnline.Application
{
public interface IShopItemsReceiveContext
{
    IShopItemsReceiveBuffer CreateShopItemsBuffer(int capacity);
}
public interface IShopItemsReceiveBuffer
{
    void Add(int id, int price, string name, string description);
    void SetBlocked(int index, byte blocked);
    void Commit(int shopId);
}
}

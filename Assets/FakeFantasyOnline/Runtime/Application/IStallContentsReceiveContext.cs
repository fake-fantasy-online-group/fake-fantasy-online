namespace FakeFantasyOnline.Application
{
public interface IStallContentsReceiveContext
{
    IStallContentsReceiveBuffer CreateStallContentsBuffer(int capacity);
}
public interface IStallContentsReceiveBuffer
{
    void AddRow(int itemId, int count, long price, string name, string description);
    void BeginSales(int capacity);
    void AddSale(long time, string item, int count, long money);
    void SetBlocked(int index, byte blocked);
    void SetPet(int index, PetBrief pet);
    void SetQuality(int index, byte quality);
    bool HasPet(int index);
    void SetPetPoints(int index, int aiUsed, int aiCapacity, int shengUsed, int shengCapacity);
    void Commit(int ownerId, string ownerName, byte type, string shopName);
}
}

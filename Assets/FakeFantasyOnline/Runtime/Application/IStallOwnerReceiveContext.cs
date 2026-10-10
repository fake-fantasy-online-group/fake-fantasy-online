namespace FakeFantasyOnline.Application
{
public interface IStallOwnerReceiveContext
{
    void ReceiveStallOwner(int ownerId, byte type, string name);
    void ReceiveStallClosed(int ownerId);
}
}

namespace FakeFantasyOnline.Application
{
public interface IGroundItemReceiveContext
{
    bool SpawnGroundItem(WorldGroundItemState state);
    void RemoveGroundItem(int id);
}
}

namespace FakeFantasyOnline.Application
{
public interface IItemIconOverrideReceiveContext
{
    void EnsureIconMap();
    void ApplyIconOverride(int itemId, int iconId);
    void ClearGroundIconCache();
}
}

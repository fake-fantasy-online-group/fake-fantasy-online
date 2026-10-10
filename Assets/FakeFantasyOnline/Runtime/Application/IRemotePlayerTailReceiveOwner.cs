namespace FakeFantasyOnline.Application
{
// Calls the current native owner on the existing host-thread Dispatch stack.
public interface IRemotePlayerTailReceiveOwner : IPlayerHairReceiveContext
{
    void ReceiveRemoteHair(int id, int head, int hair);
    void ReceiveLocalAppearance(WorldPlayerAppearanceState state);
    void ReceiveRemoteAppearance(WorldPlayerAppearanceState state);
    void ReceiveRemoteRiding(WorldRemoteRidingState state);
    void ReceiveRemoteInvisibility(int id, bool invisible);
    void ReceiveSkillCast(WorldSkillCastState state);
}
}

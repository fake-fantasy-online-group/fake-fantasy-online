namespace FakeFantasyOnline.Application
{
// The actual Dispatch calls. Classification and failures belong to the existing native owners.
public interface ICombatEntityTailReceiveOwner
{
    void ReceiveEntityPath(int id, int[] xs, int[] ys, int speed);
    void ReceiveCombatResult(int attackerId, int targetId, int damage, int flags, int targetHp);
    void ReceiveEntityOwner(int id, int ownerId);
}
}

using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : ICombatEntityTailReceiveOwner
{
    public void ReceiveEntityPath(int id, int[] xs, int[] ys, int speed) =>
        CombatWorld.MovePath(id, xs, ys, speed);

    public void ReceiveCombatResult(int attackerId, int targetId, int damage, int flags, int targetHp) =>
        CombatWorld.OnCombat(attackerId, targetId, damage, flags, targetHp);

    public void ReceiveEntityOwner(int id, int ownerId) => CombatWorld.SetOwner(id, ownerId);
}
}

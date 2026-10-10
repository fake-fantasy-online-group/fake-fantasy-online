using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class CombatWorld
{
    /// <summary>
    /// Recovered official CombatWorld.SetOwner (0x06000132, arm64 0x23efd0 / x86_64 0x24cdb0).
    /// </summary>
    /// <remarks>
    /// Body of Dispatch case 0x8043: Unity-null test on I, monsters.TryGetValue, Unity-null test
    /// on the found entity, then one store into lootOwnerId (MonsterEntity +0x50, field token
    /// 67111067). petOwnerId (+0x54, 67111068) keeps the value Spawn assigned. An unregistered id
    /// and a destroyed entity write nothing. Evidence:
    /// <c>Build/ReferenceReverseGhidra-1.5.8/queries/WBD-122-06000132-{arm64,x86_64}/</c>.
    /// </remarks>
    public static void SetOwner(int entId, int ownerId)
    {
        MonsterEntity monster;
        if (I && I.monsters.TryGetValue(entId, out monster) && monster)
        {
            monster.lootOwnerId = ownerId;
        }
    }
}
}

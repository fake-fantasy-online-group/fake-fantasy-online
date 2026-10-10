using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    public static void ReleaseOtherMounts()
    {
        if (!I) return;
        foreach (var pair in I.players)
            if (!ReferenceEquals(pair.Value, null)) pair.Value.DropMountArt();
    }

    public static void DespawnOtherPets()
    {
        if (!I) return;
        var remove = new List<int>();
        foreach (var pair in I.monsters)
        {
            MonsterEntity value = pair.Value;
            if (!value)
            {
                remove.Add(pair.Key);
                continue;
            }
            if (value.friendly && value.petOwnerId != NetClient.SelfEntityId)
            {
                remove.Add(pair.Key);
                Destroy(value.gameObject);
            }
        }
        foreach (int id in remove) I.monsters.Remove(id);
    }
}

}

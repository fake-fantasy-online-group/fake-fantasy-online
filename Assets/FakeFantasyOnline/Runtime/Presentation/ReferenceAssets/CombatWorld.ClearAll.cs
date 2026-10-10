using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class CombatWorld
{
    public static void ClearAll()
    {
        if (I == null) return;
        foreach (MonsterEntity monster in I.monsters.Values)
            if (monster != null) Destroy(monster.gameObject);
        I.monsters.Clear();
        foreach (RemotePlayer player in I.players.Values)
            if (player != null) Destroy(player.gameObject);
        I.players.Clear();
        foreach (GameObject trap in I.trapObj.Values)
            if (trap != null) Destroy(trap);
        I.trapPos.Clear();
        I.trapSkill.Clear();
        I.trapObj.Clear();
        I.trapScale.Clear();
        I.target = null;
        I.pvpTarget = null;
        TargetSelect.Clear();
        SocialClient.SelectedMemberId = 0;
        MonsterEntity.ClearGridCache();
    }

    internal void ClearForTeardownForIndependentRuntime()
    {
        if (I == this) ClearAll();
    }
}
}

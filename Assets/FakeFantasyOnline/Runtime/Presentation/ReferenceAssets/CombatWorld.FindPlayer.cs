using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class CombatWorld
{
    public static int FindPlayerIdByName(string name)
    {
        if (I == null || string.IsNullOrEmpty(name)) return 0;
        foreach (var pair in I.players)
            if (pair.Value != null && pair.Value.pname == name) return pair.Key;
        return 0;
    }
}
}

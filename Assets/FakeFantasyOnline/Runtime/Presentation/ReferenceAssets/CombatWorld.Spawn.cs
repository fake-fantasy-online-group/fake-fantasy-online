using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    public static void Spawn(int id, int model, string name, int level, int x, int y,
        int hp, int maxHp, int type = 3, int ownerId = 0, int elite = 0, int friendlyFlag = 0)
    {
        Ensure();
        if (I.monsters.ContainsKey(id)) return;
        if (type == 4)
        {
            GameSettings.EnsureLoaded();
            if (GameSettings.HideOtherPets && ownerId != NetClient.SelfEntityId) return;
        }
        bool ally = friendlyFlag > 0;
        string prefix = type == 4 ? "Pet" : ally ? "Ally" : "Mon";
        GameObject root = new GameObject(string.Format("{0}:{1}:{2}", prefix, name, id));
        MonsterEntity entity = root.AddComponent<MonsterEntity>();
        entity.ally = ally;
        entity.petOwnerId = type == 4 ? ownerId : 0;
        if (LoadTrace.On) LoadTrace.Spawn(type == 4 ? "宠" : ally ? "友军" : "怪", id, model, name);
        entity.Init(id, model, name, level, x, y, hp, maxHp, I.ppu, I.worldW, I.worldH, type == 4, elite);
        entity.lootOwnerId = type == 4 ? 0 : ownerId;
        I.monsters[id] = entity;
    }

    internal static MonsterEntity MonsterForIndependentRuntime(int id) =>
        I && I.monsters.TryGetValue(id, out MonsterEntity value) ? value : null;
}

}

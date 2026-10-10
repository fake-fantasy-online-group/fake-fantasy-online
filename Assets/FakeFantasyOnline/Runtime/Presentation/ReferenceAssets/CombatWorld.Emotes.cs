namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    public static void ShowEmote(int id, int face)
    {
        if (I != null && I.monsters.TryGetValue(id, out MonsterEntity monster) && monster != null)
        { monster.ShowEmote(face); return; }
        if (id == NetClient.SelfEntityId && id != 0 && PlayerEmote.Local != null)
            PlayerEmote.Local.ShowEmote(face);
    }

    // Official 1.5.8 CombatWorld.ShowBubble (0x06000139, arm64 0x240538 / 416 bytes,
    // x86_64 0x24e080 / 320 bytes), reached only from the 0x8019 dispatch case:
    //   uVar = CombatWorld.I (static field, arm64 ldr x0,[x8] with x8 = TypeInfo + 0xb8)
    //   if (Object.op_Equality(I, null)) return;            -> arm64 bl op_Equality, x86_64 FUN_01038840
    //   lVar = *(I + 0x20)                                  -> CombatWorld.monsters (atlas field offset 32)
    //   if (I == null || lVar == 0) FUN_00177a74()          -> non-returning null helper
    //   TryGetValue(lVar, id, out local)                     -> arm64 FUN_0066f32c / x86_64 FUN_006835e0
    //   if (!found) return;
    //   if (Object.op_Equality(local, null)) return;
    //   MonsterEntity.ShowBubble(local, text)                -> 0x06000660
    // There is no local-player branch, no fallback to CombatWorld.players and no clamp: an id
    // that is not a live monster produces no bubble, which is what the neighbouring
    // ShowPlayerBubble (0x0600013a, 0x8039 channel 0) exists for.
    public static void ShowBubble(int id, string text)
    {
        if (I != null && I.monsters.TryGetValue(id, out MonsterEntity monster) && monster != null)
        { monster.ShowBubble(text); return; }
    }

    public static void ShowPlayerBubble(int id, string text, string theme)
    {
        if (id == NetClient.SelfEntityId && PlayerEmote.Local != null)
        { PlayerEmote.Local.ShowBubble(text, theme); return; }
        if (I != null && I.players.TryGetValue(id, out RemotePlayer player) && player != null)
            player.ShowChatBubble(text, theme);
    }
}

}

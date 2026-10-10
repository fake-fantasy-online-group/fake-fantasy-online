using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    /// <summary>
    /// Official 1.5.8 CombatWorld.SpawnPlayer (06000133): the live dictionary owns both branches.
    /// ARM 0x23f168/1100 and x86_64 0x24cee0/1072 independently agree on this managed sequence.
    /// </summary>
    public static void SpawnPlayer(int id, string name, string title, int level, int x, int y,
        int gender, int head, int hair, int atkVariant, int body, int cap, int backpack,
        int wr, int wl, int face, bool dead, string aura, string soulFx, int faction,
        int hp, int maxHp, string titleFx, int glowMode)
    {
        Ensure();
        RemotePlayer existing;
        if (I.players.TryGetValue(id, out existing) && existing != null)
        {
            existing.faction = faction;
            existing.maxHp = maxHp;
            existing.SetHp(hp);
            return;
        }

        if (LoadTrace.On) LoadTrace.Spawn("玩家", id, body, name);
        GameObject root = new GameObject(string.Format("Player:{0}:{1}", name, id));
        RemotePlayer player = root.AddComponent<RemotePlayer>();

        // These are physical-reference failures, not Unity destroyed-object comparisons.
        // Native code reloads I after AddComponent, then checks I before the component.
        CombatWorld initializationWorld = I;
        if (ReferenceEquals(initializationWorld, null)) throw new NullReferenceException();
        if (ReferenceEquals(player, null)) throw new NullReferenceException();
        player.Init(id, name, title, level, (float)x, (float)y, gender, head, hair,
            atkVariant, new PlayerAvatar.Appear
            {
                Body = body,
                Cap = cap,
                Backpack = backpack,
                WeaponR = wr,
                WeaponL = wl,
                Face = face
            }, initializationWorld.ppu, initializationWorld.worldW, initializationWorld.worldH);
        player.SetAvatarAura(aura);
        if (glowMode >= 0) player.SetGlowMode(glowMode);
        player.SetSoulFx(soulFx);
        player.SetTitleFx(titleFx);
        player.faction = faction;
        player.maxHp = maxHp > 1 ? maxHp : 1;
        player.SetHp(hp);

        // Read the current world/dictionary again after all initializer/effect/HP calls.
        // Do not capture the lookup dictionary, use Add, or roll back a partial failure.
        I.players[id] = player;
        if (dead) player.PlayDie();
    }
}

}

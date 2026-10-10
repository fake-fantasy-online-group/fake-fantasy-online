using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// The three 1.5.8 CombatWorld presenters that NetClient.Dispatch reaches for entity status
/// text, entity status icons and one-shot entity effects.
/// </summary>
/// <remarks>
/// Dual-architecture evidence (frozen 1.5.8 slices, read-only persistent Ghidra project):
/// <c>ShowEntityStatus</c> 0x06000158 arm64 0x24590c / x86_64 0x252f80, reached from case 0x801b
/// (arm64 case block 0x24e7a8, bl at 0x24e85c) and per row from case 0x8069 (arm64 case block
/// 0x25574c, bl at 0x25bd14); <c>SetEntityStatusIcons</c> 0x06000159 arm64 0x245cc4 / x86_64
/// 0x2532a0, reached from case 0x8064 (arm64 case block 0x24c248, tail branch at 0x25a070) and
/// from case 0x8069 (bl at 0x25bd80); <c>PlayEntityEffect</c> 0x0600015a arm64 0x245e64 /
/// x86_64 0x2533e0, reached from case 0x8045 (arm64 case block 0x254abc, bl at 0x254b48).
/// Field offsets are the frozen 1.5.8 atlas ones: CombatWorld.monsters +0x20, CombatWorld.players
/// +0x28, CombatWorld.ppu +0x48, NetClient.SelfEntityId static +0x7c and GameBootstrap.PlayerT
/// static +0xb8.
/// </remarks>
public partial class CombatWorld
{
    /// <summary>
    /// Official 0x06000158. The local-player arm precedes both dictionaries and is the only arm
    /// that reaches EffectPlayer.SpawnOn; mode and duration are not used there.
    /// </summary>
    public static void ShowEntityStatus(int id, string eff, int posMode, int durMs)
    {
        if (id == NetClient.SelfEntityId && NetClient.SelfEntityId != 0)
        {
            // The official body reads the prefix with the effect string as the receiver, so an
            // empty prefix test on a null name raises instead of skipping.
            if (!eff.StartsWith("WF")) return;
            EffectPlayer.SpawnOn(0, eff);
            return;
        }

        MonsterEntity monster;
        if (I && I.monsters.TryGetValue(id, out monster) && monster)
        {
            monster.ShowStatusFx(eff, posMode, durMs);
            return;
        }

        RemotePlayer player;
        if (I && I.players.TryGetValue(id, out player) && player)
        {
            player.ShowStatusFx(eff, posMode, durMs);
        }
    }

    /// <summary>
    /// Official 0x06000159. The official body resolves monsters only: remote players and the
    /// local player have no status-icon arm.
    /// </summary>
    public static void SetEntityStatusIcons(int id, List<MonsterEntity.StatusIcon> list)
    {
        MonsterEntity monster;
        if (I && I.monsters.TryGetValue(id, out monster) && monster)
        {
            monster.SetStatusIcons(list);
        }
    }

    /// <summary>
    /// Official 0x0600015a. Monster, then remote player, then the local fallback for id zero or
    /// the self id; every arm plays once through EffectPlayer.PlayNamed with this owner's ppu.
    /// </summary>
    /// <remarks>
    /// <c>ReferenceFirstWorldWindowOwner.SyncSkillHitEffects</c> is the single 0x8045 entry and
    /// calls this owner once per edge after pointing <c>CombatWorld.I</c> at the frame's own world.
    /// The local arm reads <see cref="GameBootstrap.PlayerT"/>, which the independent-runtime frame
    /// entry binds from the same doll root the official <c>GameBootstrap.BuildWorld</c> uses.
    /// </remarks>
    public static void PlayEntityEffect(int id, string eff)
    {
        if (!I) return;
        if (string.IsNullOrEmpty(eff)) return;

        Vector3 position;
        MonsterEntity monster;
        RemotePlayer player;
        if (I.monsters.TryGetValue(id, out monster) && monster)
        {
            position = monster.transform.position;
        }
        else if (I.players.TryGetValue(id, out player) && player)
        {
            position = player.transform.position;
        }
        else
        {
            if (id != 0 && id != NetClient.SelfEntityId) return;
            Transform playerTransform = GameBootstrap.PlayerT;
            if (!playerTransform) return;
            position = playerTransform.position;
        }

        EffectPlayer.PlayNamed(position, eff, I.ppu, 1, -1, false, false);
    }
}

}

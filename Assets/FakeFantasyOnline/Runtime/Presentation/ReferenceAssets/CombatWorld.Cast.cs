using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official CombatWorld skill-cast owners for the 0x800e cast-state message.
/// </summary>
/// <remarks>
/// CombatWorld.PlayerCast (0x06000138, arm64 0x23fcf4, x86_64 0x24d8a0),
/// CombatWorld.MonsterPlayPrepare (0x06000157, arm64 0x245780, x86_64 0x252e50),
/// CombatWorld.MonsterPlayAttack (0x06000156, arm64 0x2455f4, x86_64 0x252d20) and the
/// MonsterTransform helper (0x06000155, arm64 0x240390, x86_64 0x24df30) are restored from the
/// frozen 1.5.8 atlas signatures (static System.Boolean CombatWorld.PlayerCast(System.Int32
/// casterId, System.Int32 skillId, System.Int32 phase, System.Int32 targetId = 0,
/// System.Nullable`1&lt;UnityEngine.Vector2&gt; aimWorld = null)) and from the read-only
/// decompilation of both architectures in
/// Build/ReferenceReverseGhidra-1.5.8/queries/wb027-skill-cast-20260928.
///
/// PlayerCast order, identical on arm64 and x86_64:
///   1. Object.op_Equality(I, null) -&gt; return false (arm64 0x23fd80, x86_64 0x24d8f6).
///   2. I.players (+0x28) TryGetValue(casterId, out player); a false result returns the false of
///      the lookup itself (arm64 0x23fe88, x86_64 0x24d95d).
///   3. Object.op_Equality(player, null) -&gt; return false (arm64 0x23feac, x86_64 0x24d993).
///   4. phase != 1 -&gt; return true. A remote player only ever releases at phase 1 and has no
///      preparation call at all: arm64 0x23feb4 compares w22 with 1 and x86_64 0x24d9b3 tests
///      local_58 against 1, and both return 1 without touching PlayCast, SkillCaster.Cast or
///      SkillSoundDB.PlayFire.
///   5. RemotePlayer.PlayCast() (0x06000b0b) - before the target, the aim and the effect work.
///   6. Target: when targetId != 0 and targetId == NetClient.SelfEntityId (static +0x7c of the
///      NetClient type) the official reads the local-player Transform through the small accessor
///      at arm64 0x2401d4 / x86_64 0x24ddd0, which class-initialises the type the atlas lists as
///      CombatNumber and reads its static +0x18 - the only static UnityEngine.Transform at that
///      offset in the frozen atlas is CombatNumber.playerT, so this is
///      CombatNumber.PlayerTransform. Otherwise the target stays null for targetId == 0 and for
///      targetId == casterId, and only otherwise becomes FindPlayer(targetId).transform when
///      that remote player is live, or MonsterTransform(targetId).
///   7. Aim: only when the Nullable&lt;Vector2&gt; has a value, groundPos becomes
///      new Vector3((aim.x - worldW * 0.5f) / ppu, (worldH * 0.5f - aim.y) / ppu, 0f) from the
///      CombatWorld instance fields ppu (+0x48), worldW (+0x4c) and worldH (+0x50)
///      (arm64 0x23ff74..0x23fff8, x86_64 0x24da9f..0x24db41).
///   8. Win05.SkillInfo(skillId) (0x06001021); playerSupportCast is
///      target != null &amp;&amp; info != null &amp;&amp; info.canFriendly == 1 &amp;&amp; info.canEnemy == 0
///      (arm64 0x2400bc/0x2400c8, x86_64 0x24dc05), matching the SkInfo fields at +0x64 and
///      +0x68.
///   9. caster Transform = player.transform; a null target is replaced by the caster Transform
///      only after playerSupportCast has been computed (arm64 0x2400e8..0x240124).
///  10. SkillCaster.Cast(skillId, caster, target, ppu, groundPos, playerSupportCast)
///      (0x06000cd4, arm64 0x240168, argument order taken from the metadata signature) then
///      SkillSoundDB.PlayFire(skillId) (0x06000d07) then return true.
/// A null I or a null players dictionary is the compiler-generated null check that ends in the
/// non-returning throw (arm64 0x2401d0 FUN_00177a74, x86_64 0x24ddca FUN_0016c1b0), so it is an
/// exception path and not a silently skipped branch.
///
/// MonsterPlayPrepare and MonsterPlayAttack share one two-step body over I.monsters (+0x20):
/// Object.op_Inequality(I, null) first, then monsters.TryGetValue(id), then
/// Object.op_Inequality(monster, null), then MonsterEntity.PlayPrepare (0x06000646) or
/// MonsterEntity.PlayAttack (0x06000645) with no argument. NetClient.Dispatch calls
/// MonsterPlayPrepare when the phase byte is 0 (arm64 0x25a654 cbz w25 -&gt; 0x25c564) and
/// MonsterPlayAttack only after SkillCaster.Cast and SkillSoundDB.PlayFire for every non-zero
/// phase (arm64 0x25c6d0, 0x25c6dc, 0x25c6f8), so for a monster the effect and the fire sound
/// are released before the attack pose, which is the opposite of the remote order above.
/// </remarks>
public partial class CombatWorld
{
    public static bool PlayerCast(
        int casterId,
        int skillId,
        int phase,
        int targetId = 0,
        Vector2? aimWorld = null)
    {
        if (!I)
        {
            return false;
        }

        RemotePlayer player;
        if (!I.players.TryGetValue(casterId, out player))
        {
            return false;
        }

        if (!player)
        {
            return false;
        }

        if (phase != 1)
        {
            return true;
        }

        player.PlayCast();

        Transform target = null;
        if (targetId != 0 && targetId == NetClient.SelfEntityId)
        {
            target = CombatNumber.PlayerTransform;
        }
        else if (targetId != 0 && targetId != casterId)
        {
            RemotePlayer targetPlayer = FindPlayer(targetId);
            target = targetPlayer ? targetPlayer.transform : MonsterTransform(targetId);
        }

        Vector3? groundPos = null;
        if (aimWorld.HasValue)
        {
            groundPos = new Vector3(
                (aimWorld.Value.x - I.worldW * 0.5f) / I.ppu,
                (I.worldH * 0.5f - aimWorld.Value.y) / I.ppu,
                0f);
        }

        Win05.SkInfo info = Win05.SkillInfo(skillId);
        bool playerSupportCast = target != null && info != null &&
            info.canFriendly == 1 && info.canEnemy == 0;
        Transform casterTransform = player.transform;
        SkillCaster.Cast(
            skillId,
            casterTransform,
            target != null ? target : casterTransform,
            I.ppu,
            groundPos,
            playerSupportCast);
        SkillSoundDB.PlayFire(skillId);
        return true;
    }

    public static void MonsterPlayPrepare(int id)
    {
        if (!I)
        {
            return;
        }

        MonsterEntity monster;
        if (!I.monsters.TryGetValue(id, out monster))
        {
            return;
        }

        if (!monster)
        {
            return;
        }

        monster.PlayPrepare();
    }

    public static void MonsterPlayAttack(int id)
    {
        if (!I)
        {
            return;
        }

        MonsterEntity monster;
        if (!I.monsters.TryGetValue(id, out monster))
        {
            return;
        }

        if (!monster)
        {
            return;
        }

        monster.PlayAttack();
    }

    public static Transform MonsterTransform(int id)
    {
        if (!I)
        {
            return null;
        }

        MonsterEntity monster;
        if (!I.monsters.TryGetValue(id, out monster))
        {
            return null;
        }

        if (!monster)
        {
            return null;
        }

        return monster.transform;
    }
}

}

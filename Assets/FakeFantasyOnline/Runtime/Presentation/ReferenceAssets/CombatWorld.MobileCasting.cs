using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class CombatWorld
{
    // Mobile casts are immediate intents. Never create approach/queued intents or change
    // selection/auto-attack: the player's existing movement belongs to the movement owner.
    internal static float MobileSkillRange(int skillId, float fallback)
    {
        if (AuthoritativeProfessionSkill.Uses(skillId))
        {
            var value = AuthoritativeProfessionSkill.Current(skillId);
            // Preserve the negotiated contract's existing nonpositive-distance semantics.
            return value != null && value.CastDistance > 0 ? value.CastDistance : float.PositiveInfinity;
        }
        return fallback > 0 ? fallback : 100f;
    }

    // A direction point is an orientation, not a cast destination. Acquire and
    // revalidate its target against the current footprint, not CastDistance (e.g.
    // multi-throw has Dist 50 but a 500-pixel cone). Aiming at a rectangle's target
    // puts it on the centerline, so length, not the corner search radius, is reach.
    private static float MobileTargetRange(int skillId, float fallback)
    {
        var aim = SkillTargeting.Resolve(skillId, out _, out float radius, out _,
            out float length, out _, out _);
        if (aim == SkillTargeting.Aim.Direction)
            return Mathf.Max(0f, length > 0f ? length : radius);
        return MobileSkillRange(skillId, fallback);
    }

    private static bool MobileReady(int skillId)
    {
        if (PlayerController.I == null) return false;
        if (AuthoritativeProfessionSkill.Uses(skillId) && !AuthoritativeProfessionSkill.LearnedActive(skillId)) return false;
        return true;
    }

    private static bool MobileWithin(Vector2 point, float range)
    {
        var pc = PlayerController.I;
        return pc != null && (point - new Vector2(pc.worldX, pc.worldY)).sqrMagnitude <= range * range;
    }

    private static bool MobileCandidate(int skillId, object source, float range, out int id, out Vector2 point)
    {
        id = 0; point = default;
        if (AuthoritativeProfessionSkill.Current(skillId)?.InputMode == "trapEntity") return false;
        if (source is MonsterEntity monster)
        {
            if (monster == null || !monster.gameObject.activeInHierarchy || monster.hp <= 0 || monster.NoAttack) return false;
            if (AuthoritativeProfessionSkill.Uses(skillId))
            {
                if (AuthoritativeProfessionSkill.ValidateTarget(skillId, monster) != null) return false;
            }
            else
            {
                var info = Win05.SkillInfo(skillId);
                if (info == null || info.canEnemy != 1) return false;
            }
            id = monster.id; point = new Vector2(monster.worldX, monster.worldY);
        }
        else if (source is RemotePlayer remote)
        {
            if (remote == null || !remote.gameObject.activeInHierarchy ||
                (remote.PickBoxForIndependentRuntime != null && !remote.PickBoxForIndependentRuntime.enabled)) return false;
            if (AuthoritativeProfessionSkill.Uses(skillId))
            {
                // Current authority advertises party targets, not hostile-player PvP.
                if (AuthoritativeProfessionSkill.ValidateTarget(skillId, remote) != null) return false;
            }
            else
            {
                var info = Win05.SkillInfo(skillId);
                if (remote.IsDead || info == null) return false;
                bool friendly = SocialClient.InParty(remote.id) || !remote.HostileToLocal;
                if (friendly ? info.canFriendly != 1 : info.canEnemy != 1) return false;
                if (friendly && remote.SupportHostileToLocal && !SocialClient.InParty(remote.id)) return false;
            }
            id = remote.id; point = new Vector2(remote.worldX, remote.worldY);
        }
        else return false;
        return id != 0 && MobileWithin(point, range);
    }

    internal static bool TryMobileSkillTarget(int skillId, float rangePx, bool allowNearest,
        out int targetId, out Vector2 worldPixels)
    {
        targetId = 0; worldPixels = default;
        if (!MobileReady(skillId) || I == null) return false;
        float range = MobileTargetRange(skillId, rangePx);
        object selected = TargetSelect.CurrentSourceForIndependentRuntime;
        if (MobileCandidate(skillId, selected, range, out targetId, out worldPixels)) return true;
        if (selected == null && SocialClient.SelectedMemberId != 0 &&
            I.players.TryGetValue(SocialClient.SelectedMemberId, out var member) &&
            MobileCandidate(skillId, member, range, out targetId, out worldPixels)) return true;
        if (!allowNearest) { targetId = 0; worldPixels = default; return false; }
        var pc = PlayerController.I;
        return MobileNearest(skillId, range, new Vector2(pc.worldX, pc.worldY), float.PositiveInfinity,
            out targetId, out worldPixels);
    }

    private static bool MobileNearest(int skillId, float range, Vector2 origin, float hitRadius,
        out int targetId, out Vector2 point)
    {
        targetId = 0; point = default;
        float best = hitRadius * hitRadius;
        foreach (var monster in I.monsters.Values)
            MobileConsider(skillId, monster, range, origin, ref best, ref targetId, ref point);
        foreach (var player in I.players.Values)
            MobileConsider(skillId, player, range, origin, ref best, ref targetId, ref point);
        return targetId != 0;
    }

    private static void MobileConsider(int skillId, object source, float range, Vector2 origin,
        ref float best, ref int targetId, ref Vector2 point)
    {
        if (!MobileCandidate(skillId, source, range, out int id, out Vector2 candidate)) return;
        float distance = (candidate - origin).sqrMagnitude;
        if (distance < best || (distance == best && (targetId == 0 || id < targetId)))
        { best = distance; targetId = id; point = candidate; }
    }

    internal static bool TryMobileSkillTargetAt(int skillId, int gx, int gy, float rangePx,
        out int targetId, out Vector2 worldPixels)
    {
        targetId = 0; worldPixels = default;
        return MobileReady(skillId) && I != null && MobileNearest(skillId, MobileSkillRange(skillId, rangePx),
            new Vector2(gx, gy), 48f, out targetId, out worldPixels);
    }

    private static bool MobileTargetById(int skillId, int id, float range, out Vector2 point)
    {
        point = default;
        if (I == null) return false;
        if (I.monsters.TryGetValue(id, out var monster) && MobileCandidate(skillId, monster, range, out _, out point)) return true;
        return I.players.TryGetValue(id, out var player) && MobileCandidate(skillId, player, range, out _, out point);
    }

    internal static bool TryMobileUnitCast(int skillId, float rangePx, bool allowNearest)
    {
        if (!MobileReady(skillId) || NetClient.I == null) return false;
        var value = AuthoritativeProfessionSkill.Current(skillId);
        if (value != null && value.InputMode != "entity" && value.InputMode != "self") return false;
        if (value != null && value.InputMode == "self") { NetClient.I.UseSkill(skillId, 0); return true; }
        if (TryMobileSkillTarget(skillId, rangePx, allowNearest, out int id, out _) &&
            MobileTargetById(skillId, id, MobileSkillRange(skillId, rangePx), out _))
        { NetClient.I.UseSkill(skillId, id); return true; }
        bool selfFallback = value != null
            ? value.TargetSelf && value.SelfFallback && !value.DeadTargetOnly
            : Win05.SkillInfo(skillId)?.canSelf == 1;
        if (selfFallback && CharData.hp > 0)
        { NetClient.I.UseSkill(skillId, 0); return true; }
        return false;
    }

    internal static bool TryMobileUnitCastAt(int skillId, int gx, int gy, float rangePx)
    {
        if (!MobileReady(skillId) || NetClient.I == null) return false;
        var value = AuthoritativeProfessionSkill.Current(skillId);
        if (value != null && value.InputMode != "entity") return false;
        bool found = TryMobileSkillTargetAt(skillId, gx, gy, rangePx, out int id, out Vector2 targetPoint);
        var pc = PlayerController.I;
        Vector2 pointer = new Vector2(gx, gy);
        float selfDistance = (pointer - new Vector2(pc.worldX, pc.worldY)).sqrMagnitude;
        bool canSelf = value != null ? value.TargetSelf && !value.DeadTargetOnly : Win05.SkillInfo(skillId)?.canSelf == 1;
        if (canSelf && CharData.hp > 0 && selfDistance <= 48f * 48f && (!found || selfDistance < (pointer - targetPoint).sqrMagnitude))
        { NetClient.I.UseSkill(skillId, 0); return true; }
        if (!found || !MobileTargetById(skillId, id, MobileSkillRange(skillId, rangePx), out _)) return false;
        NetClient.I.UseSkill(skillId, id);
        return true;
    }

    internal static Vector2 ClampMobileGround(int skillId, Vector2 point, float rangePx)
    {
        var pc = PlayerController.I;
        if (pc == null) return point;
        Vector2 origin = new Vector2(pc.worldX, pc.worldY);
        return origin + Vector2.ClampMagnitude(point - origin, MobileSkillRange(skillId, rangePx));
    }

    internal static bool TryMobileGroundCast(int skillId, int gx, int gy, float rangePx,
        bool direction, int requiredTargetId = 0)
    {
        if (!MobileReady(skillId) || NetClient.I == null) return false;
        var value = AuthoritativeProfessionSkill.Current(skillId);
        if (value != null && value.InputMode != (direction ? "direction" : "point")) return false;
        Vector2 point = new Vector2(gx, gy);
        if (requiredTargetId != 0 && !MobileTargetById(skillId, requiredTargetId,
            (direction ? MobileTargetRange(skillId, rangePx) : MobileSkillRange(skillId, rangePx)), out point)) return false;
        // Direction points encode an orientation, not a destination. Never shorten or
        // replace that direction with the selected unit when manually aiming.
        if (!direction) point = ClampMobileGround(skillId, point, rangePx);
        int x = Mathf.RoundToInt(point.x), y = Mathf.RoundToInt(point.y);
        if (!direction)
        {
            float range = MobileSkillRange(skillId, rangePx);
            // Quantization can push a clamped circle edge outside its legal radius.
            // Move rounded components inward, then verify the actual wire coordinates.
            var pc = PlayerController.I;
            for (int step = 0; step < 2 && !MobileWithin(new Vector2(x, y), range); step++)
            {
                float dx = x - pc.worldX, dy = y - pc.worldY;
                if (Mathf.Abs(dx) >= Mathf.Abs(dy)) x += dx > 0 ? -1 : 1;
                else y += dy > 0 ? -1 : 1;
            }
            if (!MobileWithin(new Vector2(x, y), range)) return false;
        }
        NetClient.I.UseSkillAt(skillId, x, y, 0);
        return true;
    }

    internal static bool TryMobileNearestTrapCast(int skillId, float rangePx)
    {
        if (!MobileReady(skillId) || NetClient.I == null || I == null) return false;
        var value = AuthoritativeProfessionSkill.Current(skillId);
        if (value == null || value.InputMode != "trapEntity") return false;
        int nearest = 0;
        float best = float.PositiveInfinity;
        var pc = PlayerController.I;
        Vector2 origin = new Vector2(pc.worldX, pc.worldY);
        float range = MobileSkillRange(skillId, rangePx);
        foreach (var entry in I.trapPos)
        {
            if (!MobileTrapPoint(entry.Key, range, out Vector2 point)) continue;
            float distance = (point - origin).sqrMagnitude;
            if (distance < best || (distance == best && (nearest == 0 || entry.Key < nearest)))
            { nearest = entry.Key; best = distance; }
        }
        if (nearest == 0 || !MobileTrapPoint(nearest, range, out _)) return false;
        NetClient.I.UseSkill(skillId, nearest);
        return true;
    }

    private static bool MobileTrapPoint(int id, float range, out Vector2 point)
    {
        point = default;
        if (I == null || !I.trapPos.TryGetValue(id, out Vector3 position) ||
            !I.trapObj.TryGetValue(id, out GameObject visual) || visual == null || !visual.activeInHierarchy) return false;
        var pc = PlayerController.I;
        if (pc == null) return false;
        point = new Vector2(position.x * pc.ppu + pc.worldW * .5f, pc.worldH * .5f - position.y * pc.ppu);
        return MobileWithin(point, range);
    }

    internal static bool TryMobileTrapCast(int skillId, int gx, int gy, float rangePx)
    {
        if (!MobileReady(skillId) || NetClient.I == null || I == null) return false;
        var value = AuthoritativeProfessionSkill.Current(skillId);
        if (value != null && value.InputMode != "trapEntity") return false;
        if (!TryTrapAtPointerForSkill(gx, gy, out int id) ||
            !MobileTrapPoint(id, MobileSkillRange(skillId, rangePx), out _)) return false;
        NetClient.I.UseSkill(skillId, id);
        return true;
    }
}
}

using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Negotiated gameplay metadata. Resource rows remain art/legacy inputs and are never mutated here.</summary>
internal static class AuthoritativeProfessionSkill
{
    private static EndgameClientState cachedState;
    private static long cachedGeneration = -1, cachedVersion = -1;
    private static readonly Dictionary<int, Win05.SkInfo> infos = new Dictionary<int, Win05.SkInfo>();
    internal static bool Uses(int id)
    {
        var state = EndgamePanel.GrowthState;
        // A local loadout does not describe other professions' observed casts. Keep their
        // legacy art metadata; explicit cross-profession grant rows still use the contract.
        // Older servers still hold 10212 without a contract. Adopt restored theft only
        // when its authoritative row exists; keep stale rows owned here so casts wait.
        return (id != 10212 || state?.SkillRank(id)?.Contract != null) && state?.SkillBehaviorV1 == true && EndgameSkillRankView.IsProfessionSkill(id) &&
            ((id - 10000) / 100 == CharData.race || state.SkillRank(id) != null);
    }
    internal static EndgameSkillBehaviorView Current(int id) => Uses(id) ? EndgamePanel.GrowthState.CurrentSkillBehavior(id) : null;
    internal static bool LearnedActive(int id) => Uses(id) && EndgamePanel.GrowthState.SkillRank(id)?.CanCast == true && Current(id)?.Usable == true;
    internal static bool TryInfo(int id, out Win05.SkInfo info)
    {
        info = null;
        if (!Uses(id)) return false;
        var state = EndgamePanel.GrowthState;
        if (!ReferenceEquals(cachedState, state) || cachedGeneration != state.Generation || cachedVersion != state.Version)
        { infos.Clear(); cachedState = state; cachedGeneration = state.Generation; cachedVersion = state.Version; }
        if (infos.TryGetValue(id, out info)) return true;
        // Identity is generation-scoped and remains useful while an updated behavior is pending.
        var contract = state.SkillRank(id)?.Contract;
        if (contract == null) return true;
        var behavior = contract.Current ?? contract.Next;
        info = new Win05.SkInfo { id = id, name = contract.Name, icon = contract.Icon,
            maxlv = state.SkillHardCap, pre = contract.PrerequisiteSkillIDs.Count > 0 ? contract.PrerequisiteSkillIDs[0] : 0,
            type = contract.Kind == "passive" ? 18 : 0, tgt = contract.Kind == "passive" ? 18 : 1,
            desc = string.Empty };
        if (behavior != null)
        {
            info.castkind = !behavior.Usable ? 0 : behavior.InputMode == "entity" || behavior.InputMode == "trapEntity" ? 1 : 2;
            info.canSelf = behavior.TargetSelf ? 1 : 0;
            info.canFriendly = behavior.TargetTeam ? 1 : 0;
            info.canEnemy = behavior.TargetEnemy ? 1 : 0;
            info.dist = behavior.CastDistance;
            info.shape = Shape(behavior.Area.Shape); info.radius = behavior.Area.Radius;
            info.rectl = behavior.Area.Length; info.rectw = behavior.Area.Width; info.rwidth = behavior.Area.Angle;
            if (contract.Kind == "area" || behavior.InputMode == "direction" || behavior.InputMode == "point") info.tgt = 2;
            if (!behavior.Usable) info.tgt = 18;
        }
        else info.tgt = 18;
        infos[id] = info;
        return true;
    }
    private static int Shape(string value) => value == "rectangle" ? 1 : value == "circle" ? 2 : value == "cone" ? 3 : 0;
    internal static bool TryResolve(int id, out SkillTargeting.Aim aim, out float range, out float radius,
        out float angle, out float length, out float width, out bool trap)
    {
        aim = SkillTargeting.Aim.Unavailable; range = radius = angle = length = width = 0; trap = false;
        if (!Uses(id)) return false;
        var value = Current(id);
        if (value == null || !LearnedActive(id)) return true;
        range = value.CastDistance; radius = value.Area.Radius; angle = value.Area.Angle;
        length = value.Area.Length; width = value.Area.Width;
        switch (value.InputMode)
        {
            case "entity": aim = SkillTargeting.Aim.Single; break;
            case "point": aim = SkillTargeting.Aim.Circle; break;
            case "direction": aim = SkillTargeting.Aim.Direction; break;
            case "trapEntity": aim = SkillTargeting.Aim.Trap; trap = true; break;
            case "self": aim = SkillTargeting.Aim.Instant; break;
        }
        return true;
    }
    internal static bool SelfCentered(int id) => Current(id)?.Area.Center == "caster" && Current(id)?.Area.Shape == "circle";
    internal static string ValidateTarget(int id, ISelectable target)
    {
        var value = Current(id);
        if (value == null) return "正在等待服务器技能资料";
        if (target == null) return value.SelfFallback && value.TargetSelf ? null : "请选择技能目标";
        object source = target is ReferenceWorldRemoteSelectable remoteView ? (object)remoteView.SourceIdentityForIndependentRuntime : target;
        if (source is RemotePlayer player)
        {
            if (!value.TargetTeam) return "不能对该目标使用技能";
            if (!SocialClient.InParty(player.id)) return "该技能只能对同队成员使用";
            if (player.IsDead && !value.AllowsDeadTarget) return "不能对死亡目标使用该技能";
            if (!player.IsDead && value.DeadTargetOnly) return "请选择需要复活的队友";
            return null;
        }
        if (source is MonsterEntity monster)
            return value.TargetEnemy && !monster.NoAttack && monster.hp > 0 && !value.DeadTargetOnly ? null : "不能对该目标使用技能";
        return "不能对该目标使用技能";
    }
    internal static bool TryFriendlyOrSelf(int id, out int targetId, out string error)
    {
        targetId = 0; error = null;
        var value = Current(id);
        if (value == null) { error = "正在等待服务器技能资料"; return true; }
        if (value.InputMode == "self") return true;
        if (value.InputMode != "entity") return false;
        var source = TargetSelect.CurrentSourceForIndependentRuntime;
        if (source is RemotePlayer player)
        {
            error = ValidateTarget(id, TargetSelect.Current);
            if (error == null) targetId = player.id;
            return true;
        }
        if (source != null)
        {
            // Support hotkeys historically ignore a selected monster/NPC and cast on self.
            // Only retain that fallback when the server explicitly advertises it.
            if (!value.TargetEnemy && value.TargetSelf && value.SelfFallback) return true;
            return false;
        }
        int memberId = SocialClient.SelectedMemberId;
        if (value.TargetTeam && memberId > 0 && memberId != NetClient.SelfEntityId && SocialClient.InParty(memberId))
        { targetId = memberId; return true; } // Server performs final alive/dead/range validation.
        if (value.TargetSelf && value.SelfFallback) return true;
        return false;
    }
    internal static void Waiting() => UITip.Show("正在等待服务器技能资料", UIScale.Factor);
}
}

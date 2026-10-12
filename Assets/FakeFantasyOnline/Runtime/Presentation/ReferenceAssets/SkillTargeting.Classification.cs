using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class SkillTargeting
{
    // Official 1.5.8 06000d0c..06000d13: predicates keep the source exception boundaries.
    private static bool DescHas(string d, string k) => !string.IsNullOrEmpty(d) && d.Contains(k);
    private static bool IsTrapSkill(Win05.SkInfo i) => DescHas(i.desc, "放置");
    private static bool IsBlinkSkill(Win05.SkInfo i) => DescHas(i.desc, "移动至指定");
    private static bool IsLeapSkill(Win05.SkInfo i) => DescHas(i.desc, "跳跃至选中");

    public static Aim Resolve(
        int skillId,
        out float range,
        out float radius,
        out float angle,
        out float rectL,
        out float rectW,
        out bool isTrap)
    {
        range = radius = angle = rectL = rectW = 0f;
        isTrap = false;

        Win05.SkInfo skill = Win05.SkillInfo(skillId);
        if (skillId == 10211 || skill == null)
        {
            return Aim.Instant;
        }

        // The original sword contract takes precedence over descriptions/shapes
        // supplied by later resource variants, including false ground-target AoEs.
        if (NativeSwordSkillTargeting.TryGet(skillId, out var original))
        {
            range = original.CanEnemy != 0 && !original.SelfCentered
                ? original.Distance : skill.dist;
            radius = original.SelfCentered ? skill.radius : 0f;
            return original.CanEnemy != 0 && !original.SelfCentered
                ? Aim.Single : Aim.Instant;
        }

        isTrap = IsTrapSkill(skill);
        if (isTrap)
        {
            range = Mathf.Max((float)skill.dist, 150f);
            radius = skill.radius > 0 ? skill.radius : 100f;
            return Aim.Circle;
        }
        if (IsBlinkSkill(skill))
        {
            range = Mathf.Max((float)skill.dist, 500f);
            radius = skill.radius > 0 ? skill.radius : 40f;
            return Aim.Circle;
        }
        if (IsLeapSkill(skill))
        {
            range = skill.dist > 0 ? skill.dist : 500f;
            return Aim.Single;
        }

        if (skill.castkind == 0 || skill.canEnemy == 0)
        {
            return Aim.Instant;
        }

        range = skill.dist;
        switch (skill.shape)
        {
            case 1:
                rectL = skill.rectl > 0 ? skill.rectl : 150f;
                rectW = skill.rectw > 0 ? skill.rectw : 70f;
                if (range <= 0f)
                {
                    range = rectL;
                }
                return Aim.Direction;
            case 2:
                if (skill.dist > 0 && !SelfCenteredByDesc(skill.desc))
                {
                    radius = (skill.radius > 0 ? skill.radius : DefaultRadius) +
                        ArenaRadiusBonus;
                    return Aim.Circle;
                }
                return Aim.Instant;
            case 3:
                angle = skill.rwidth > 0 ? skill.rwidth : 120f;
                radius = (skill.radius > 0 ? skill.radius : DefaultRadius) +
                    ArenaRadiusBonus;
                if (range <= 0f)
                {
                    range = radius;
                }
                return Aim.Direction;
            default:
                if (range <= 0f)
                {
                    range = ServerSingleRangeFallback;
                }
                return Aim.Single;
        }
    }

    public static bool SelfCenteredCircle(Win05.SkInfo info)
    {
        if (info != null && NativeSwordSkillTargeting.TryGet(info.id, out var original))
            return original.SelfCentered;
        if (info == null || info.shape != 2 || info.id == 10211)
        {
            return false;
        }
        if (IsTrapSkill(info) || IsBlinkSkill(info) || IsLeapSkill(info))
        {
            return false;
        }
        return info.dist <= 0 || SelfCenteredByDesc(info.desc);
    }

    private static bool SelfCenteredByDesc(string desc)
    {
        if (string.IsNullOrEmpty(desc))
        {
            return false;
        }

        return desc.Contains("以自己为中心") ||
               desc.Contains("自己为中心") ||
               desc.Contains("自身为中心") ||
               desc.Contains("自己周围") ||
               desc.Contains("自身周围") ||
               desc.Contains("身边") ||
               desc.Contains("周围");
    }

    // 06000d12: the map classification is the sole gate; do not clamp or test Showing.
    private static float ArenaRadiusBonus =>
        CustomMaps.IsCustom(GameBootstrap.CurMapId) ? ZoneOverlay.faShenPx : 0f;

    // 06000d19: recover the complete Color return, including alpha in the fourth register.
    private static Color OutlineColor(int skillId) => SkillElementDB.Element(skillId) switch
    {
        0 => new Color(0.3f, 0.7f, 1f, 0.85f),
        1 => new Color(1f, 0.4f, 0.2f, 0.85f),
        2 => new Color(0.8f, 0.5f, 1f, 0.85f),
        3 => new Color(0.85f, 0.62f, 0.32f, 0.85f),
        4 => new Color(0.6f, 0.3f, 0.7f, 0.85f),
        5 => new Color(0.4f, 1f, 0.55f, 0.85f),
        _ => new Color(0.45f, 0.95f, 1f, 0.85f)
    };
}
}

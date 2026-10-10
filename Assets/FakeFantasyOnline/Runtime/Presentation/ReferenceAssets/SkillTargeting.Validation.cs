namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class SkillTargeting
{
    public static string ValidateTarget(int skillId, ISelectable target)
    {
        if (target == null) return "请选择攻击目标";
        if (target is NpcInteract) return "不能对该目标使用技能";
        if (!target.SelFriendly) return null;
        Win05.SkInfo skill = Win05.SkillInfo(skillId);
        return skill != null && skill.canFriendly == 1 ? null : "不能对友方使用该技能";
    }
}

}

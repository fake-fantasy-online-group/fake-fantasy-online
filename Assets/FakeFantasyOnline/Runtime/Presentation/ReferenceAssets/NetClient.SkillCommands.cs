using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// This existing Presentation facet owns the original UI/gameplay members; the Protocol
// facet owns the native writer closures and transport. Neither imports the other assembly.
internal partial class NetClient : MonoBehaviour
{
    public static NetClient I;
    public static Vector3? PendingCastGround;

    private Action<string, float> showTip;
    private Action<int, int> writeSkill;
    private Action<bool> writeSit;
    private Action writeEndWork;
    private Action<int, int, int, int> writeSkillAt;

    internal void BindSkillCommandsForIndependentRuntime(
        Action<string, float> tip,
        Action<int, int> directWriter,
        Action<int, int, int, int> pointWriter,
        Action<bool> sittingWriter,
        Action endWorkWriter)
    {
        showTip = tip;
        writeSit = sittingWriter;
        writeEndWork = endWorkWriter;
        writeSkill = directWriter;
        writeSkillAt = pointWriter;
    }

    public void SitForIndependentRuntime(bool on) => writeSit(on);
    public void EndWorkForIndependentRuntime() => writeEndWork();

    private static bool LocalNotEnoughMp(int skillId)
    {
        // Growth skills are resolved at effective rank, after server modifiers. Legacy
        // resource costs must never veto them, including the pre-contract V5 transition.
        if (EndgamePanel.GrowthState != null && FakeFantasyOnline.Application.EndgameSkillRankView.IsProfessionSkill(skillId))
        {
            var behavior = AuthoritativeProfessionSkill.Current(skillId);
            return behavior != null && behavior.MPCost > 0 && CharData.mp < behavior.MPCost;
        }
        int level;
        if (CharData.skills == null || !CharData.skills.TryGetValue(skillId, out level) || level <= 0)
        {
            return false;
        }
        Win05.SkInfo info = Win05.SkillInfo(skillId);
        if (info == null || info.levels == null)
        {
            return false;
        }
        foreach (Win05.SkLevel row in info.levels)
        {
            if (row != null && row.lv == level)
            {
                return row.mp > 0 && CharData.mp < row.mp;
            }
        }
        return false;
    }

    public void UseSkill(int skillId, int targetId = 0)
    {
        if (AuthoritativeProfessionSkill.Uses(skillId) && !AuthoritativeProfessionSkill.LearnedActive(skillId))
        { AuthoritativeProfessionSkill.Waiting(); return; }
        var behavior = AuthoritativeProfessionSkill.Current(skillId);
        if (behavior != null && behavior.PointRequired)
        { showTip("请选择技能位置", UIScale.Factor); return; }
        if (LocalNotEnoughMp(skillId))
        {
            showTip("法力不足", UIScale.Factor);
            return;
        }

        GameBootstrap.FlushMoveReport();
        if (PlayerController.I != null)
        {
            PlayerController.I.BreakSit();
        }
        PendingCastSkill = skillId;
        PendingCastGround = null;
        CombatWorld.NotifyCast(0.7f);
        writeSkill(skillId, targetId);
    }

    public void UseSkillAt(int skillId, int x, int y, int targetId = 0)
    {
        if (AuthoritativeProfessionSkill.Uses(skillId) && !AuthoritativeProfessionSkill.LearnedActive(skillId))
        { AuthoritativeProfessionSkill.Waiting(); return; }
        if (LocalNotEnoughMp(skillId))
        {
            showTip("法力不足", UIScale.Factor);
            return;
        }

        GameBootstrap.FlushMoveReport();
        if (PlayerController.I != null)
        {
            PlayerController.I.BreakSit();
        }
        PendingCastSkill = skillId;
        PlayerController player = PlayerController.I;
        PendingCastGround = player != null
            ? new Vector3((x - player.worldW * 0.5f) / player.ppu,
                (player.worldH * 0.5f - y) / player.ppu, 0f)
            : (Vector3?)null;
        CombatWorld.NotifyCast(0.7f);
        writeSkillAt(skillId, x, y, targetId);
    }
}

}

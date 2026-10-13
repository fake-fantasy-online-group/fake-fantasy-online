using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    private static float _lastCdMsgT;

    // Official 1.5.8 0600043c, including the uncaptured party-selection predicate 0600046b.
    public static void UseHotSlot(int idx)
    {
        if ((uint)idx > 29 || hotSkill[idx] == 0 || NetClient.I == null) return;
        if (!HotSlotUsable(idx)) return;
        if (SkillCdFrac(hotSkill[idx]) > 0f)
        {
            if (hotKind[idx] == 2) return;
            if (Time.unscaledTime - _lastCdMsgT > 0.8f)
            {
                ChatUI.Add("系统", "该技能正在冷却中", new Color(1f, 180f / 255f, 90f / 255f, 1f), "", null);
                _lastCdMsgT = Time.unscaledTime;
            }
            return;
        }
        if (hotKind[idx] == 1)
        {
            NetClient.I.PetUseSkillForIndependentRuntime(hotSkill[idx]);
            CharData.petActiveSkill = hotSkill[idx];
            return;
        }
        if (hotKind[idx] == 2)
        {
            int itemId = hotSkill[idx];
            var item = InventoryDlg.FindItem(itemId);
            if (ItemActions.Handle(itemId, item.Item2, item.Item3, item.Item4, item.Item5, item.Item6, null))
                return;
            NetClient.I.UseItem(itemId, item.Item2, item.Item3);
            StartItemCd(itemId);
            return;
        }
        int skillId = hotSkill[idx];
        GameSettings.EnsureLoaded();
        SkillTargeting.Aim aim = SkillTargeting.Resolve(skillId, out float range, out float radius,
            out float angle, out float rectL, out float rectW, out bool isTrap);
        if (aim == SkillTargeting.Aim.Unavailable) { AuthoritativeProfessionSkill.Waiting(); return; }
        if (aim == SkillTargeting.Aim.Single)
        {
            CombatWorld.RequestSkill(skillId, range);
            return;
        }
        if (aim != SkillTargeting.Aim.Instant)
        {
            if (GameSettings.SmartCast)
                SkillTargeting.CastImmediate(skillId, aim, range, radius, angle, rectL, rectW, isTrap);
            else
                SkillTargeting.Begin(skillId, aim, range, radius, angle, rectL, rectW, isTrap);
            return;
        }
        if (AuthoritativeProfessionSkill.Uses(skillId))
        {
            NetClient.I.UseSkill(skillId, 0); // Typed instant/self mode never borrows a selected enemy.
            return;
        }
        Win05.SkInfo info = Win05.SkillInfo(skillId);
        int targetId = 0;
        if (info != null)
        {
            if (info.canFriendly == 1 && info.canEnemy == 0)
            {
                // The existing bridge only unwraps legacy selectable representations.
                object current = TargetSelect.CurrentSourceForIndependentRuntime;
                if (current != null)
                {
                    if (current is RemotePlayer remote)
                    {
                        if (remote.SupportHostileToLocal && !SocialClient.InParty(remote.id))
                            UITip.Show("不能对敌对阵营使用该技能", UIScale.Factor);
                        else targetId = remote.id;
                    }
                }
                else if (SocialClient.SelectedMemberId != 0 && SocialClient.SelectedMemberId != NetClient.SelfEntityId &&
                    SocialClient.Party.Exists(member => member.Item1 == SocialClient.SelectedMemberId))
                {
                    targetId = SocialClient.SelectedMemberId;
                }
            }
            if (targetId == 0 && info.canEnemy == 1 && info.shape != 0 &&
                TargetSelect.CurrentSourceForIndependentRuntime is MonsterEntity monster)
                targetId = monster.id;
        }
        NetClient.I.UseSkill(skillId, targetId);
    }
}
}

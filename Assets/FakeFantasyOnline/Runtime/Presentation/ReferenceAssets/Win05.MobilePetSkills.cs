using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    private readonly MobilePanelTouch mobilePetTouch = new MobilePanelTouch();
    private void ReadMobilePetSkills(Event ev, System.Func<Vector2, int> hit)
    {
        if (mobileSkillInputBlocked) return;
        bool selected = mobilePetTouch.Read(ev, s, hit, out int id, out bool held);
        if (mobilePetTouch.Started && !UITip.MobileContainsPointer(mobilePetTouch.StartPoint) &&
            (!mobileSkillActions || !mobileSkillActionsRect.Contains(mobilePetTouch.StartPoint)))
        { mobileSkills.Reset(); mobileSkillActions = false; }
        if (selected)
        {
            mobileSkills.Select(id); mobileSkillActions = held;
            mobileSkillAnchor = mobilePetTouch.Anchor;
            mobileSkillActionsRect = MobilePanelLayout.Actions(mobileSkillAnchor, 2, out _);
            PlayerController.SuppressWorldClick();
        }
        if (mobilePetTouch.Canceled) ResetMobileSkills();
    }
    private void DrawMobilePetSkillSelection()
    {
        if (!petSkillPanelOpen || mobileSkills.Skill <= 0) return;
        CharData.PetSkill live = default;
        if (CharData.petSkills != null)
            foreach (CharData.PetSkill skill in CharData.petSkills)
                if (skill.id == mobileSkills.Skill) { live = skill; break; }
        if (live.id <= 0 || live.level <= 0) { ResetMobileSkills(); return; }
        if (!mobileSkillActions)
        {
            DrawTooltip((live.active ? "[主动] " : "[被动] ") + live.name + "  Lv." + live.level + "/" + live.max,
                live.desc ?? string.Empty, mobileSkillAnchor);
            return;
        }
        bool usable = MobileShortcutEligibility.Pet(live.id, live.level, live.active);
        Rect box = MobilePanelLayout.Actions(mobileSkillAnchor, mobileSkills.ChoosingSlot ? 9 : 2, out float scale);
        mobileSkillActionsRect = box;
        int depth = GUI.depth; GUI.depth = 0;
        if (mobileSkills.ChoosingSlot)
        {
            int slot = MobilePanelLayout.HotbarSlot(box, scale, ref mobileBindingPage, out bool cancel);
            if (slot >= 0 && mobileSkills.Commit(slot, usable,
                (target, id) => GameHud.BindMobileShortcut(target, id, PetSkillIcon(live.id, live.icon), 1))) ResetMobileSkills();
            if (cancel) mobileSkills.CancelSlots();
        }
        else
        {
            if (MobilePanelLayout.Action(box, 0, "放入快捷栏", scale, usable))
            { mobileBindingPage = 0; mobileSkills.Choose(usable); }
            if (MobilePanelLayout.Action(box, 1, "取消", scale)) ResetMobileSkills();
        }
        Event ev = Event.current;
        if (box.Contains(ev.mousePosition) && (ev.type == EventType.MouseDown || ev.type == EventType.MouseUp || ev.type == EventType.MouseDrag)) ev.Use();
        GUI.depth = depth;
    }
}
}

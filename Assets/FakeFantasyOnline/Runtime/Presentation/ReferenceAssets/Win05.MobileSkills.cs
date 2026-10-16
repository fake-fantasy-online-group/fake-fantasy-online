using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    internal bool MobileOverlayContains(Vector2 point) => IsOpen &&
        mobileSkillActions && mobileSkillActionsRect.Contains(point);

    private readonly MobileSkillSelection mobileSkills = new MobileSkillSelection();
    private Rect mobileSkillViewport, mobileSkillSafeArea, mobileSkillActionsRect;
    private int mobileSkillTab = -1, mobileBindingPage, mobileLifeSkill, mobileSkillFinger = -1;
    private float mobileSkillDownAt;
    private bool mobileSkillHeld, mobileSkillActions, mobileSkillInputBlocked;
    private string mobileSkillWindow;
    private Vector2 mobileSkillAnchor;
    private bool IsMobileSkillWindow => MobileClientPresentation.Enabled && (cur == "skilldlg" || cur == "studydlg" || cur == "petdlg");
    private void PrepareMobileSkillWindow(Def def)
    {
        if (!MobileClientPresentation.Enabled) { MobileClientPresentation.RemovePanel(this); return; }
        if (mobileSkillWindow != cur) { ResetMobileSkills(); mobileSkillWindow = cur; }
        Rect safe = MobileClientPresentation.SafeAreaGui;
        if (safe != mobileSkillSafeArea || Input.touchCount > 1 ||
            (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Canceled))
        { ResetMobileSkills(); mobileSkillSafeArea = safe; }
        if (!positioned)
        {
            winX = safe.x + (safe.width - def.w * s) * .5f;
            winY = safe.y + (safe.height - def.h * s) * .5f;
            positioned = true;
        }
        float width = def.w + (cur == "petdlg" && petSkillPanelOpen ? 166f : 0f);
        float height = cur == "petdlg" && petSkillPanelOpen ? Mathf.Max(def.h, 297f + PetSkillExtraH()) : def.h;
        Rect panel = MobilePanelLayout.Fit(new Rect(0, 0, width, height), safe, ref s, ref winX, ref winY);
        MobileClientPresentation.RegisterPanel(this, panel);
    }
    private void ResetMobileSkills()
    {
        mobilePetTouch.Reset();
        mobileSkills.Reset(); mobileSkillActions = mobileSkillHeld = false; mobileLifeSkill = 0; mobileSkillFinger = -1;
    }
    private bool MobileBookSkillUsable(SkInfo info)
    {
        if (!SkillUsableForHotbar(info)) return false;
        return UsesGrowthSkill(info.id) ? EndgamePanel.GrowthState.SkillRank(info.id)?.CanCast == true :
            CharData.skills.TryGetValue(info.id, out int rank) && rank > 0;
    }
    private bool MobileSkillTreeInput(Rect viewport, Event ev, float maximum)
    {
        mobileSkillHeld = false;
        if (mobileSkillViewport != viewport || mobileSkillTab != skillTab)
        { ResetMobileSkills(); mobileSkillViewport = viewport; mobileSkillTab = skillTab; }
        if (Input.touchCount > 1 || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Canceled))
        { ResetMobileSkills(); return false; }
        if (mobileSkills.Pressed && mobileSkillFinger >= 0 &&
            (Input.touchCount == 0 || Input.GetTouch(0).fingerId != mobileSkillFinger))
        { ResetMobileSkills(); return false; }
        if (mobileSkillActions || UITip.MobileContainsPointer(ev.mousePosition)) return false;
        if (ev.type == EventType.MouseDown && ev.button == 0 && viewport.Contains(ev.mousePosition))
        {
            mobileSkills.Reset(); mobileSkills.Begin(ev.mousePosition.x, ev.mousePosition.y);
            mobileSkillDownAt = Time.unscaledTime; mobileSkillAnchor = ev.mousePosition;
            mobileSkillFinger = Input.touchCount == 1 ? Input.GetTouch(0).fingerId : -1;
        }
        if (ev.type == EventType.MouseDrag)
        {
            skillScroll = Mathf.Clamp(skillScroll + mobileSkills.Move(ev.mousePosition.x, ev.mousePosition.y, 8 * s) / s, 0, maximum);
            ev.Use();
        }
        if (ev.type == EventType.Repaint && mobileSkills.Pressed && !mobileSkills.Moved &&
            Time.unscaledTime - mobileSkillDownAt >= .5f && viewport.Contains(ev.mousePosition))
        {
            mobileSkills.ConsumeHold(); mobileSkillHeld = true;
            return true;
        }
        return ev.type == EventType.MouseUp && ev.button == 0 &&
            mobileSkills.End(ev.mousePosition.x, ev.mousePosition.y, 8 * s, !viewport.Contains(ev.mousePosition));
    }
    private void ShowMobileSkillTooltip()
    {
        if (mobileSkillActions || mobileSkills.Skill <= 0) return;
        SkInfo info = SkillInfo(mobileSkills.Skill);
        if (info == null) { ResetMobileSkills(); return; }
        CharData.skills.TryGetValue(info.id, out int level);
        Vector2 pointer = Event.current.mousePosition;
        Event.current.mousePosition = mobileSkillAnchor;
        string text = BuildSkillTip(info, level);
        if (UsesGrowthSkill(info.id)) UITip.ShowSkill(text, s);
        else RequestSkillTipForIndependentRuntime(text, s);
        Event.current.mousePosition = pointer;
    }
    private void DrawMobileSkillActions(System.Func<float,float,float,float,Rect> L, Def def)
    {
        if (cur == "petdlg") { DrawMobilePetSkillSelection(); return; }
        if (!mobileSkillActions) return;
        SkInfo info = SkillInfo(mobileSkills.Skill);
        if (info == null) { ResetMobileSkills(); return; }
        bool usable = MobileBookSkillUsable(info);
        Rect box = MobilePanelLayout.Actions(mobileSkillAnchor, mobileSkills.ChoosingSlot ? 9 : 3, out float scale);
        mobileSkillActionsRect = box;
        int depth = GUI.depth; GUI.depth = 0;
        if (mobileSkills.ChoosingSlot)
        {
            int slot = MobilePanelLayout.HotbarSlot(box, scale, ref mobileBindingPage, out bool cancel);
            if (slot >= 0 && mobileSkills.Commit(slot, usable,
                (target, id) => GameHud.BindMobileBookSkill(target, id, LoadSkillIcon(info.icon)))) ResetMobileSkills();
            if (cancel) mobileSkills.CancelSlots();
        }
        else
        {
            if (MobilePanelLayout.Action(box, 0, "学习 / 升级", scale, CanShowUpgrade(info.id)) && NetClient.I != null)
            { NetClient.I.UpgradeSkillForIndependentRuntime(info.id); ResetMobileSkills(); }
            if (MobilePanelLayout.Action(box, 1, "放入快捷栏", scale, usable))
            { mobileBindingPage = 0; mobileSkills.Choose(usable); }
            if (MobilePanelLayout.Action(box, 2, "取消", scale)) ResetMobileSkills();
        }
        Event ev = Event.current;
        if (box.Contains(ev.mousePosition) && (ev.type == EventType.MouseDown || ev.type == EventType.MouseUp || ev.type == EventType.MouseDrag)) ev.Use();
        GUI.depth = depth;
    }
    private void OnApplicationFocus(bool focus) { if (!focus) ResetMobileSkills(); }
    private void OnApplicationPause(bool pause) { if (pause) ResetMobileSkills(); }
    private void OnDisable() { ResetMobileSkills(); MobileClientPresentation.RemovePanel(this); }
}
}

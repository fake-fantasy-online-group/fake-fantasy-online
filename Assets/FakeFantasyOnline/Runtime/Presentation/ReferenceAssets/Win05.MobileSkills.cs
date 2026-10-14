using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    private readonly MobileSkillSelection mobileSkills = new MobileSkillSelection();
    private Rect mobileSkillViewport;
    private int mobileSkillTab = -1;
    private int mobileSkillFirstRow;
    private int mobileBindingPage;
    private Rect mobileSkillSafeArea;
    private bool IsMobileSkillWindow => MobileClientPresentation.Enabled && (cur=="skilldlg" || cur=="studydlg");
    private void PrepareMobileSkillWindow(Def def)
    {
        if (!IsMobileSkillWindow) return;
        Rect safe=MobileClientPresentation.SafeAreaGui;
        if (safe!=mobileSkillSafeArea || Input.touchCount>1 ||
            (Input.touchCount==1 && Input.GetTouch(0).phase==TouchPhase.Canceled))
        { mobileSkills.Reset(); mobileSkillFirstRow=0; mobileSkillSafeArea=safe; }
        s=Mathf.Min(s,Mathf.Min(safe.width/def.w,safe.height/def.h));
        winX=safe.x+(safe.width-def.w*s)*.5f;
        winY=safe.y+(safe.height-def.h*s)*.5f;
        positioned=true;
        dragging=false;
    }
    private bool MobileBookSkillUsable(SkInfo info)
    {
        if (!SkillUsableForHotbar(info)) return false;
        return UsesGrowthSkill(info.id) ? EndgamePanel.GrowthState.SkillRank(info.id)?.CanCast == true :
            CharData.skills.TryGetValue(info.id, out int rank) && rank > 0;
    }
    private bool MobileSkillTreeInput(Rect viewport, Event ev, float maximum)
    {
        if (mobileSkillViewport != viewport || mobileSkillTab != skillTab)
        { mobileSkills.Reset(); mobileSkillViewport=viewport; mobileSkillTab=skillTab; }
        if (Input.touchCount>1) { mobileSkills.Reset(); return false; }
        if (Input.touchCount==1 && Input.GetTouch(0).phase==TouchPhase.Canceled)
        { mobileSkills.Reset(); return false; }
        if (ev.type==EventType.MouseDown && ev.button==0 && viewport.Contains(ev.mousePosition))
            mobileSkills.Begin(ev.mousePosition.x,ev.mousePosition.y);
        if (ev.type==EventType.MouseDrag)
        {
            skillScroll=Mathf.Clamp(skillScroll+mobileSkills.Move(ev.mousePosition.x,ev.mousePosition.y,8*s)/s,0,maximum);
            ev.Use();
        }
        return ev.type==EventType.MouseUp && ev.button==0 &&
            mobileSkills.End(ev.mousePosition.x,ev.mousePosition.y,8*s,!viewport.Contains(ev.mousePosition));
    }
    private void DrawMobileSkillActions(System.Func<float,float,float,float,Rect> L, Def def)
    {
        SkInfo info=SkillInfo(mobileSkills.Skill);
        if (info==null) { mobileSkills.Reset(); return; }
        bool usable=MobileBookSkillUsable(info);
        if (mobileSkills.ChoosingSlot)
        {
            GUI.Box(L(18,65,230,299),"选择快捷栏位置（替换该格内容）");
            for (int button=0;button<4;button++)
            {
                // Exact HUD page*4+button mapping; the last page only contains two slots.
                int slot=mobileBindingPage*4+button;
                if (slot>=30) continue;
                Rect rect=L(22+(button%2)*110,105+(button/2)*74,106,64);
                if (GUI.Button(rect,(mobileBindingPage+1)+"页"+(button+1)))
                    mobileSkills.Commit(slot,usable,(target,id)=>GameHud.BindMobileBookSkill(target,id,LoadSkillIcon(info.icon)));
            }
            if (GUI.Button(L(22,265,106,44),"上一页")) mobileBindingPage=(mobileBindingPage+7)%8;
            if (GUI.Button(L(132,265,106,44),"下一页")) mobileBindingPage=(mobileBindingPage+1)%8;
            if (GUI.Button(L(22,333,218,28),"取消选槽")) mobileSkills.CancelSlots();
            return;
        }
        CharData.skills.TryGetValue(info.id,out int level);
        UITip.DrawMobilePage(L(18,65,230,265),BuildSkillTip(info,level),s,ref mobileSkillFirstRow);
        if (GUI.Button(L(22,333,218,28),"返回技能树")) { mobileSkills.Reset(); return; }
        bool enabled=GUI.enabled;
        try
        {
            GUI.enabled=enabled && usable;
            if (GUI.Button(L(132,def.h-31,136,28),"放入快捷栏")) { mobileBindingPage=0; mobileSkills.Choose(usable); }
            GUI.enabled=enabled && CanShowUpgrade(info.id);
            if (GUI.Button(L(14,def.h-31,112,28),"学习 / 升级") && NetClient.I!=null)
                NetClient.I.UpgradeSkillForIndependentRuntime(info.id);
        }
        finally { GUI.enabled=enabled; }
    }
    private void OnApplicationFocus(bool focus) { if (!focus) mobileSkills.Reset(); }
    private void OnApplicationPause(bool pause) { if (pause) mobileSkills.Reset(); }
    private void OnDisable() { mobileSkills.Reset(); }
}
}

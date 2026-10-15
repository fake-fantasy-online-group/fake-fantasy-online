using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    private int mobileFinger = -1;
    private int mobilePressed = -1;
    private int mobilePage;
    private Rect mobileViewport;
    private Vector2 mobilePressPosition;

    private void Update()
    {
        if (!MobileClientPresentation.Enabled) { CancelMobileGesture(); return; }
        Rect viewport = MobileClientPresentation.SafeAreaGui;
        if (mobileViewport != viewport) { CancelMobileGesture(); mobileViewport = viewport; }
        if (MobileClientPresentation.ModalOpen) { CancelMobileGesture(); return; }
        bool found = false;
        for (int i=0;i<Input.touchCount;i++)
        {
            Touch touch=Input.GetTouch(i);
            HandleMobilePointer(touch.fingerId,touch.position,touch.phase);
            if (touch.fingerId==mobileFinger) found=true;
        }
        if (!UnityEngine.Application.isMobilePlatform && Input.touchCount==0 &&
            (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)))
        {
            HandleMobilePointer(-2,Input.mousePosition,Input.GetMouseButtonDown(0) ? TouchPhase.Began :
                Input.GetMouseButtonUp(0) ? TouchPhase.Ended : TouchPhase.Moved);
            if (mobileFinger==-2) found=true;
        }
        if (mobileFinger!=-1 && !found) CancelMobileGesture();
    }

    private void HandleMobilePointer(int finger,Vector2 position,TouchPhase phase)
    {
        Vector2 point=MobileClientPresentation.GuiPoint(position);
        if (phase==TouchPhase.Began && mobileFinger==-1 && !MobileClientPresentation.IsPanelPointer(position))
        {
            int button=showHotbar && !diagramOpen && MobileClientPresentation.AttackRect.Contains(point) ? 4 : -1;
            if (showHotbar && !diagramOpen) for (int k=0;k<4;k++) if (MobileClientPresentation.SkillRect(k).Contains(point)) button=k;
            if (showMinimap && MobileClientPresentation.MinimapRect.Contains(point))
            {
                button=10;
                for (int k=0;k<4;k++) if (MobileClientPresentation.MapActionRect(k).Contains(point)) button=11+k;
            }
            if (button>=0)
            {
                mobileFinger=finger; mobilePressed=button; mobilePressPosition=position;
                if (button<4) SkillTargeting.BeginMobileHotSlot(mobilePage*4+button);
            }
        }
        if (finger!=mobileFinger) return;
        bool cancel=phase==TouchPhase.Canceled || MobileClientPresentation.CancelRect.Contains(point);
        if (mobilePressed<4 && SkillTargeting.Active) ClientInput.SetSkillAim(MobileAimPosition(position),false,false);
        if (phase==TouchPhase.Ended || phase==TouchPhase.Canceled)
        {
            if (cancel) SkillTargeting.CancelMobileHotSlot();
            else if (mobilePressed<4) SkillTargeting.ReleaseMobileHotSlot(MobileAimPosition(position));
            else if (mobilePressed==4 && MobileClientPresentation.AttackRect.Contains(point)) MobileAttack();
            else if (mobilePressed>=10 && MobileClientPresentation.MinimapRect.Contains(point))
            {
                if (mobilePressed==10 || mobilePressed==14) guiMinimap?.ToggleDiagram();
                else if (MobileClientPresentation.MapActionRect(mobilePressed-11).Contains(point))
                {
                    if (mobilePressed==11) guiMinimap?.ZoomMinimap(true);
                    if (mobilePressed==12) guiMinimap?.ZoomMinimap(false);
                    if (mobilePressed==13) { guiMinimap?.CloseDiagram(); WorldMapDlg.Toggle(); guiHost?.ObserveNativeWorldMapRequestForIndependentRuntime(); }
                }
            }
            mobileFinger=-1; mobilePressed=-1;
        }
    }

    private Vector2 MobileAimPosition(Vector2 pointer)
    {
        // Skill-thumb displacement aims from the player, not at the button under the thumb.
        Vector2 origin = new Vector2(Screen.width*.5f,Screen.height*.5f);
        if (Camera.main != null && PlayerController.I != null)
            origin = Camera.main.WorldToScreenPoint(PlayerController.I.transform.position);
        Vector2 delta = pointer-mobilePressPosition;
        return origin+Vector2.ClampMagnitude(delta,120f*MobileClientPresentation.Scale)*3f;
    }
    private void CancelMobileGesture()
    {
        if (mobileFinger!=-1) SkillTargeting.CancelMobileHotSlot();
        mobileFinger=-1; mobilePressed=-1;
    }
    private void OnApplicationFocus(bool focused) { if (!focused) CancelMobileGesture(); }
    private void OnApplicationPause(bool paused) { if (paused) CancelMobileGesture(); }
    private void OnDisable() { CancelMobileGesture(); }
    private static void MobileAttack()
    {
        if (!(TargetSelect.CurrentSourceForIndependentRuntime is MonsterEntity) &&
            !(TargetSelect.CurrentSourceForIndependentRuntime is RemotePlayer)) CombatWorld.CycleNearbyMonster();
        object target = TargetSelect.CurrentSourceForIndependentRuntime;
        if (target is MonsterEntity monster) CombatWorld.SetTarget(monster);
        else if (target is RemotePlayer remote && remote.SupportHostileToLocal) CombatWorld.SetPvpTarget(remote);
    }
    private void DrawMobileHud()
    {
        float scale=MobileClientPresentation.Scale;
        Matrix4x4 previous=GUI.matrix;
        float oldScale=s,oldUiScale=uiScale;
        bool ready=true;
        try
        {
            s=scale; uiScale=scale;
            Rect safe=MobileClientPresentation.SafeAreaGui;
            GUI.matrix=Matrix4x4.Translate(new Vector3(safe.x,safe.y,0))*previous;
            if (guiPlayerStatus!=null) ready &= guiPlayerStatus.DrawAvatarForIndependentRuntime(Time.time,(int)safe.height);
            if (guiTarget!=null)
            {
                ready &= guiTarget.Draw(out ReferenceFirstWorldTargetHudAction action,nativeInvocation:true);
                guiHost?.ObserveNativeTargetHudForIndependentRuntime(action);
            }
            if (guiBuffs!=null) ready &= guiBuffs.DrawForIndependentRuntime(Time.time,scale,(int)safe.width,this,nativeInvocation:true);
            if (guiPlayerStatus!=null) ready &= guiPlayerStatus.DrawExperienceForIndependentRuntime(Time.time,(int)safe.width,(int)safe.height);
            // The retained map renderer receives the full safe viewport, never a minimap-sized viewport.
            if (showMinimap) miniOpen=true; // Mobile keeps the compact map visible; its separate touch buttons own expansion.
            if (guiMinimap!=null) ready &= guiMinimap.DrawForNativeOwner(scale,(int)safe.width-5,
                (int)safe.height,touchControls:true);
            if (guiMinimap!=null && guiMinimap.ConsumeWorldMapRequest()) guiHost?.ObserveNativeWorldMapRequestForIndependentRuntime();
            Rect band=MarqueeBand();
            GUI.matrix=Matrix4x4.Translate(new Vector3(safe.center.x-band.width*.5f-band.x,safe.y+4*scale-band.y,0))*previous;
            DrawRaidTimer(scale); DrawZoneBar(scale);
        }
        finally { GUI.matrix=previous; s=oldScale; uiScale=oldUiScale; }
        // Reuse original resources, icons, cooldown masks and dispatch; no replacement artwork.
        if (showMinimap)
        {
            DrawMobileButton(MobileHudArtwork.Inset(MobileClientPresentation.MapActionRect(0),6f*scale),skPlus,"",mobilePressed==11);
            DrawMobileButton(MobileHudArtwork.Inset(MobileClientPresentation.MapActionRect(1),6f*scale),skMinus,"",mobilePressed==12);
            DrawMobileButton(MobileHudArtwork.Inset(MobileClientPresentation.MapActionRect(2),6f*scale),skWorldBtn,"",mobilePressed==13);
            DrawMobileButton(MobileHudArtwork.Inset(MobileClientPresentation.MapActionRect(3),6f*scale),skToggle,"",mobilePressed==14);
        }
        if (showHotbar && !diagramOpen)
        {
            MobileHudArtwork.AttackFace(MobileClientPresentation.AttackRect,scale,mobilePressed==4);
            for (int k=0;k<4;k++)
            {
                int slot=mobilePage*4+k;
                Rect rect=MobileClientPresentation.SkillRect(k);
                MobileHudArtwork.SkillFace(rect,scale,slot<30 && hotSkill[slot]!=0,
                    mobileFinger!=-1 && mobilePressed==k);
                if (slot<30)
                {
                    Texture2D icon=HotIconForIndependentRuntime(slot);
                    MobileHudArtwork.Image(MobileHudArtwork.Inset(rect,9f*scale),icon,
                        mobileFinger!=-1 && mobilePressed==k);
                    DrawCdOverlay(rect,hotSkill[slot]);
                }
            }
            if (MobileHudArtwork.TextButton(MobileClientPresentation.PageRect,(mobilePage+1)+"/8",scale)) { CancelMobileGesture(); mobilePage=(mobilePage+1)%8; }
            MobileHudArtwork.Joystick(MobileClientPresentation.JoystickRect,ClientInput.MovementAxis,scale);
        }
        if (showMenu && !diagramOpen)
        {
            if (MobileHudArtwork.TextButton(MobileClientPresentation.MenuToggleRect,
                MobileClientPresentation.MenuExpanded ? "收起" : "功能",scale,MobileClientPresentation.MenuExpanded))
            {
                MobileClientPresentation.MenuExpanded=!MobileClientPresentation.MenuExpanded;
                if (MobileClientPresentation.MenuExpanded) MobileClientPresentation.ChatExpanded=false;
            }
            if (MobileClientPresentation.MenuExpanded)
            {
                for (int index=0;index<CtrlBtns.Length;index++)
                {
                    Rect rect=MobileClientPresentation.MenuItemRect(index);
                    bool hovered=GUI.enabled && rect.Contains(Event.current.mousePosition);
                    MobileHudArtwork.ControlFace(rect,hb24Ctrl,ctrlTex,index,scale,hovered,
                        hovered && Input.GetMouseButton(0));
                    if (GUI.Button(rect,GUIContent.none,GUIStyle.none)) ActivateControlButtonForIndependentRuntime(index);
                }
            }
        }
        if (mobileFinger!=-1 && mobilePressed<4)
            ArtworkSkin.ButtonFace(MobileClientPresentation.CancelRect,"移到这里取消",scale,
                selected:MobileClientPresentation.CancelRect.Contains(MobileClientPresentation.GuiPoint(Input.mousePosition)));
        LastGuiDrawSucceededForIndependentRuntime=ready;
    }
    private static void DrawMobileButton(Rect rect,Texture2D art,string text,bool pressed=false)
    {
        MobileHudArtwork.Image(rect,art,pressed);
        MobileHudArtwork.Label(rect,text,MobileClientPresentation.Scale);
    }
}
}

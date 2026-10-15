using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    private int mobileFinger = -1;
    private int mobilePressed = -1;
    private int mobilePage;
    private readonly MobileSkillGesture mobileSkillGesture = new MobileSkillGesture();
    private bool mobileCancelHighlighted;
    internal static bool MobileHotSlotUsable(int slot) => (uint)slot <= 29 && hotSkill[slot] != 0 &&
        NetClient.I != null && HotSlotUsable(slot) && SkillCdFrac(hotSkill[slot]) <= 0f;
    private readonly MobilePageSwipe mobilePageSwipe = new MobilePageSwipe();
    private readonly MobileShortcutPointerOwnership mobileShortcutPointers = new MobileShortcutPointerOwnership();
    // Chat must not interpret synthetic mouse/right-click events from skill holds.
    internal static bool MobileShortcutPointerOwned => MobileClientPresentation.Enabled && I != null &&
        I.mobileShortcutPointers.Owns(Time.frameCount);

    private static bool MobilePointerActive(int pointer)
    {
        if (pointer == -2) return Input.GetMouseButton(0);
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.fingerId == pointer && touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
                return true;
        }
        return false;
    }
    private Rect mobileViewport;
    private Vector2 mobilePressPosition;

    private void Update()
    {
        mobileShortcutPointers.Refresh(Time.frameCount,MobilePointerActive);
        if (!MobileClientPresentation.Enabled) { CancelMobileGesture(); return; }
        Rect viewport = MobileClientPresentation.SafeAreaGui;
        if (mobileViewport != viewport) { CancelMobileGesture(); mobileViewport = viewport; }
        if (MobileClientPresentation.ModalOpen || MobileClientPresentation.DiagramOpen || !showHotbar)
        { CancelMobileGesture(); return; }
        if (Input.touchCount > 1)
        {
            // Keep ownership of every shortcut finger even when the gesture is rejected.
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                Vector2 point = MobileClientPresentation.GuiPoint(touch.position);
                bool shortcut = MobileClientPresentation.AttackRect.Contains(point) || MobileClientPresentation.PageRect.Contains(point);
                for (int k = 0; k < 4; k++) shortcut |= MobileClientPresentation.SkillRect(k).Contains(point);
                if (shortcut) mobileShortcutPointers.Claim(touch.fingerId);
            }
            CancelMobileGesture(); return;
        }
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
        bool panelPointer=MobileClientPresentation.IsPanelPointer(position);
        if (phase==TouchPhase.Began && !panelPointer && showHotbar && !diagramOpen)
        {
            // Even an ignored second shortcut finger owns its synthesized events.
            bool shortcut=MobileClientPresentation.AttackRect.Contains(point) || MobileClientPresentation.PageRect.Contains(point);
            for (int k=0;k<4;k++) shortcut |= MobileClientPresentation.SkillRect(k).Contains(point);
            if (shortcut) mobileShortcutPointers.Claim(finger);
        }
        if (phase==TouchPhase.Began && mobileFinger==-1 && !panelPointer)
        {
            int button=showHotbar && !diagramOpen && MobileClientPresentation.AttackRect.Contains(point) ? 4 : -1;
            if (showHotbar && !diagramOpen) for (int k=0;k<4;k++) if (MobileClientPresentation.SkillRect(k).Contains(point)) button=k;
            if (showHotbar && !diagramOpen && MobileClientPresentation.PageRect.Contains(point)) button=5;
            if (showMinimap && MobileClientPresentation.MinimapRect.Contains(point))
            {
                button=10;
                for (int k=0;k<4;k++) if (MobileClientPresentation.MapActionRect(k).Contains(point)) button=11+k;
            }
            if (button>=0)
            {
                mobileFinger=finger; mobilePressed=button; mobilePressPosition=position;
                if (button<4)
                {
                    mobileSkillGesture.Begin(position.x,position.y,MobileClientPresentation.Scale);
                    MobileClientPresentation.ActiveSkillButton=button;
                    SkillTargeting.BeginMobileHotSlot(mobilePage*4+button);
                }
                if (button==5) mobilePageSwipe.Begin(finger,position.x,position.y);
            }
        }
        if (finger!=mobileFinger) return;
        mobileSkillGesture.Sample(position.x,position.y);
        mobileCancelHighlighted=mobilePressed>=0 && mobilePressed<4 && MobileClientPresentation.CancelContains(point);
        bool cancel=phase==TouchPhase.Canceled || mobileCancelHighlighted;
        if (mobilePressed==5)
        {
            int change=mobilePageSwipe.Sample(finger,position.x,position.y,24f*MobileClientPresentation.Scale,
                phase==TouchPhase.Ended,cancel);
            if (change!=0) mobilePage=MobilePageSwipe.PageAfter(mobilePage,change);
        }
        if (mobilePressed<4 && SkillTargeting.Active) ClientInput.SetSkillAim(MobileAimPosition(position),false,false);
        if (phase==TouchPhase.Ended || phase==TouchPhase.Canceled)
        {
            if (cancel) SkillTargeting.CancelMobileHotSlot();
            else if (mobilePressed<4) SkillTargeting.ReleaseMobileHotSlot(MobileAimPosition(position),mobileSkillGesture.Dragged);
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
            mobileSkillGesture.Reset(); mobileCancelHighlighted=false;
            MobileClientPresentation.ActiveSkillButton=-1;
        }
    }

    private Vector2 MobileAimPosition(Vector2 pointer)
    {
        return SkillTargeting.MobileAimPosition(pointer-mobilePressPosition);
    }
    private void CancelMobileGesture()
    {
        if (mobileFinger!=-1) SkillTargeting.CancelMobileHotSlot();
        mobilePageSwipe.Reset();
        mobileFinger=-1; mobilePressed=-1;
        mobileSkillGesture.Reset(); mobileCancelHighlighted=false;
        MobileClientPresentation.ActiveSkillButton=-1;
    }
    private void OnApplicationFocus(bool focused) { if (!focused) CancelMobileGesture(); }
    private void OnApplicationPause(bool paused) { if (paused) CancelMobileGesture(); }
    private void OnDisable()
    {
        CancelMobileGesture();
        mobileShortcutPointers.Clear(Time.frameCount);
        MobileHudArtwork.ClearRoundTextures();
    }
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
            GUI.matrix=Matrix4x4.Translate(new Vector3(MobileClientPresentation.AvatarRect.x,MobileClientPresentation.AvatarRect.y,0))*previous;
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
            if (guiMinimap!=null) ready &= guiMinimap.DrawForNativeOwner(scale,MobileClientPresentation.MinimapViewportWidth,
                (int)safe.height,touchControls:true);
            if (guiMinimap!=null && guiMinimap.ConsumeWorldMapRequest()) guiHost?.ObserveNativeWorldMapRequestForIndependentRuntime();
            Rect band=MarqueeBand();
            GUI.matrix=Matrix4x4.Translate(new Vector3(safe.center.x-band.width*.5f-band.x,safe.y+4*scale-band.y,0))*previous;
            DrawRaidTimer(scale); DrawZoneBar(scale);
        }
        finally { GUI.matrix=previous; s=oldScale; uiScale=oldUiScale; }
        // Reuse original resources, icons, cooldown masks and dispatch; no replacement artwork.
        if (showHotbar && !diagramOpen)
        {
            MobileHudArtwork.AttackFace(MobileClientPresentation.AttackRect,scale,mobilePressed==4);
            for (int k=0;k<4;k++)
            {
                int slot=mobilePage*4+k;
                Rect rect=MobileClientPresentation.SkillRect(k);
                DrawMobileRoundFace(rect,scale,slot<30 && hotSkill[slot]!=0,
                    mobileFinger!=-1 && mobilePressed==k);
                if (slot<30)
                {
                    Texture2D icon=HotIconForIndependentRuntime(slot);
                    // Both artwork and cooldown are alpha-masked circles, not squares inside a rim.
                    Rect iconRect=MobileHudArtwork.Inset(rect,13f*scale);
                    MobileHudArtwork.Image(iconRect,MobileHudArtwork.RoundTexture(icon),
                        mobileFinger!=-1 && mobilePressed==k);
                    DrawMobileRoundCooldown(iconRect,hotSkill[slot]);
                }
            }
            DrawMobileRoundFace(MobileClientPresentation.PageRect,scale,true,mobilePressed==5);
            MobileHudArtwork.Label(MobileClientPresentation.PageRect,(mobilePage+1)+"/8",scale);
            MobileHudArtwork.Joystick(MobileClientPresentation.JoystickRect,ClientInput.MovementAxis,scale);
        }
        if (showMenu && !diagramOpen)
        {
            if (MobileHudArtwork.MenuButton(MobileClientPresentation.MenuToggleRect,
                scale,MobileClientPresentation.MenuExpanded))
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
            {
            Rect cancelRect=MobileClientPresentation.CancelRect;
            Color old=GUI.color;
            try
            {
                GUI.color=mobileCancelHighlighted ? new Color(1f,.3f,.25f,1f) : Color.white;
                DrawMobileRoundFace(cancelRect,scale,true,mobileCancelHighlighted);
                MobileHudArtwork.Label(cancelRect,mobileCancelHighlighted ? "松开取消" : "取消",scale);
            }
            finally { GUI.color=old; }
        }
        LastGuiDrawSucceededForIndependentRuntime=ready;
    }
    private void DrawMobileRoundCooldown(Rect rect, int skill)
    {
        if (cooldownFrames == null) return;
        float fraction = SkillCooldownFractionForIndependentRuntime(skill);
        if (fraction <= 0f) return;
        int index = CooldownFrameIndexForIndependentRuntime(fraction);
        Texture2D frame = MobileHudArtwork.RoundTexture(cooldownFrames[index]);
        if (frame == null) return;
        Color previous = GUI.color;
        try
        {
            GUI.color = new Color(1f, 1f, 1f, 180f / 255f);
            GUI.DrawTexture(rect, frame);
        }
        finally { GUI.color = previous; }
    }

    private static void DrawMobileRoundFace(Rect rect,float scale,bool occupied,bool pressed)
    {
        Texture2D[] frames=ArtworkSkin.Frames(MobileHudArtwork.RoundControlPath,1);
        Texture2D frame=frames!=null && frames.Length>0 ? frames[0] : null;
        MobileHudArtwork.Image(MobileHudArtwork.Inset(rect,2f*scale),frame,pressed,
            opacity:occupied ? .92f : .48f);
    }
}
}

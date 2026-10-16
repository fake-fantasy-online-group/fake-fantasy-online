using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    private int mobileFinger = -1;
    private int mobilePressed = -1;
    private int mobilePage;
    private int mobileWheelClosedFrame = -1;
    internal static bool MobileGroupWheelClosing => I!=null && I.mobileWheelClosedFrame==Time.frameCount;
    private readonly MobileSkillGesture mobileSkillGesture = new MobileSkillGesture();
    private bool mobileCancelHighlighted;
    internal static bool MobileHotSlotUsable(int slot) => (uint)slot < 24 && hotSkill[slot] != 0 &&
        NetClient.I != null && HotSlotUsable(slot) && SkillCdFrac(hotSkill[slot]) <= 0f;
    private readonly MobileGroupWheel mobileGroupWheel = new MobileGroupWheel();
    private readonly MobileShortcutPointerOwnership mobileShortcutPointers = new MobileShortcutPointerOwnership();
    // Chat must not interpret synthetic mouse/right-click events from skill holds.
    internal static bool MobileShortcutPointerOwned => MobileClientPresentation.Enabled && I != null &&
        I.mobileShortcutPointers.Owns(Time.frameCount);

    // Preflight the whole touch batch so world input sees the wheel's reserved
    // area even when it samples before the HUD on the opening frame.
    internal static bool MobileGroupWheelOpening
    {
        get
        {
            if (I==null || I.mobileFinger!=-1 || !I.showHotbar ||
                !MobileClientPresentation.Enabled || MobileClientPresentation.ModalOpen) return false;
            for (int i=0;i<Input.touchCount;i++)
            {
                Touch touch=Input.GetTouch(i);
                if (touch.phase==TouchPhase.Began && MobileGroupWheelStart(touch.position)) return true;
            }
            return !UnityEngine.Application.isMobilePlatform && Input.touchCount==0 &&
                Input.GetMouseButtonDown(0) && MobileGroupWheelStart(Input.mousePosition);
        }
    }
    private static bool MobileGroupWheelStart(Vector2 position) =>
        MobileClientPresentation.PageRect.Contains(MobileClientPresentation.GuiPoint(position)) &&
        !MobileClientPresentation.IsPanelPointer(position);

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
        if (MobileClientPresentation.ModalOpen)
        { CancelMobileGesture(); return; }
        if (!showHotbar && mobileFinger != -1 && mobilePressed < 10) CancelMobileGesture();
        // Movement and HUD gestures own different finger IDs. A second (or third)
        // touch must not cancel the active skill; HandleMobilePointer ignores non-owners.
        // Give the group opener priority over another same-frame touch inside
        // the appearing wheel, independent of platform touch-array ordering.
        if (mobileFinger==-1 && MobileGroupWheelOpening)
            for (int i=0;i<Input.touchCount;i++)
            {
                Touch touch=Input.GetTouch(i);
                if (touch.phase!=TouchPhase.Began || !MobileGroupWheelStart(touch.position)) continue;
                HandleMobilePointer(touch.fingerId,touch.position,touch.phase);
                break;
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
        if (phase==TouchPhase.Began && !panelPointer && showHotbar)
        {
            // Even an ignored second shortcut finger owns its synthesized events.
            bool shortcut=MobileClientPresentation.AttackRect.Contains(point) || MobileClientPresentation.PickupRect.Contains(point) || MobileClientPresentation.PageRect.Contains(point) || MobileClientPresentation.GroupWheelContains(point);
            for (int k=0;k<6;k++) shortcut |= MobileClientPresentation.SkillRect(k).Contains(point);
            if (shortcut) mobileShortcutPointers.Claim(finger);
        }
        if (phase==TouchPhase.Began && mobileFinger==-1 && !panelPointer &&
            !(MobileGroupWheelClosing && MobileClientPresentation.GroupWheelContains(point)))
        {
            int button=showHotbar && MobileClientPresentation.AttackRect.Contains(point) ? 6 : -1;
            if (showHotbar) for (int k=0;k<6;k++) if (MobileClientPresentation.SkillRect(k).Contains(point)) button=k;
            if (showHotbar && MobileClientPresentation.PageRect.Contains(point)) button=7;
            if (showHotbar && MobileClientPresentation.PickupRect.Contains(point)) button=8;
            if (showMinimap && MobileClientPresentation.MinimapRect.Contains(point))
            {
                button=10;
                for (int k=0;k<4;k++) if (MobileClientPresentation.MapActionRect(k).Contains(point)) button=11+k;
            }
            if (button>=0)
            {
                mobileFinger=finger; mobilePressed=button; mobilePressPosition=position;
                if (button<6)
                {
                    mobileSkillGesture.Begin(position.x,position.y,MobileClientPresentation.Scale);
                    MobileClientPresentation.ActiveSkillButton=button;
                    SkillTargeting.BeginMobileHotSlot(MobileGroupWheel.HotbarSlot(mobilePage,button));
                }
                if (button==7)
                {
                    Vector2 center=MobileClientPresentation.GroupWheelCenter;
                    mobileGroupWheel.Begin(finger,center.x,center.y,MobileClientPresentation.GroupWheelDeadZone,point.x,point.y);
                    MobileClientPresentation.GroupWheelOpen=true;
                }
            }
        }
        if (finger!=mobileFinger) return;
        // An overlay may appear or move over a held HUD gesture. Cancel rather
        // than firing through it; this finger cannot reacquire until a new Began.
        if (panelPointer) { CancelMobileGesture(); return; }
        mobileSkillGesture.Sample(position.x,position.y);
        mobileCancelHighlighted=mobilePressed>=0 && mobilePressed<6 && MobileClientPresentation.CancelContains(point);
        bool cancel=phase==TouchPhase.Canceled || mobileCancelHighlighted;
        if (mobilePressed==7)
        {
            int selected=phase==TouchPhase.Began ? -1 : mobileGroupWheel.Sample(finger,point.x,point.y,
                phase==TouchPhase.Ended,cancel);
            if (selected>=0) mobilePage=selected;
        }
        if (mobilePressed<6 && SkillTargeting.Active) ClientInput.SetSkillAim(MobileAimPosition(position),false,false);
        if (phase==TouchPhase.Ended || phase==TouchPhase.Canceled)
        {
            if (cancel) SkillTargeting.CancelMobileHotSlot();
            else if (mobilePressed<6) SkillTargeting.ReleaseMobileHotSlot(MobileAimPosition(position),mobileSkillGesture.Dragged);
            else if (mobilePressed==6 && MobileClientPresentation.AttackRect.Contains(point)) MobileAttack();
            else if (mobilePressed==8 && MobileClientPresentation.PickupRect.Contains(point))
                PlayerController.I?.PickupNearestForIndependentRuntime();
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
            if (mobilePressed==7) mobileWheelClosedFrame=Time.frameCount;
            mobileFinger=-1; mobilePressed=-1;
            MobileClientPresentation.GroupWheelOpen=false;
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
        if (mobilePressed==7) mobileWheelClosedFrame=Time.frameCount;
        mobileGroupWheel.Reset();
        mobileFinger=-1; mobilePressed=-1;
        MobileClientPresentation.GroupWheelOpen=false;
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
}
}

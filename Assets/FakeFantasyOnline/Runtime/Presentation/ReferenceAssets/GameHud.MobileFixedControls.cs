using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    private readonly MobileLayoutUnlockGesture[] mobileFixedPointers = {
        new MobileLayoutUnlockGesture(), new MobileLayoutUnlockGesture(), new MobileLayoutUnlockGesture(), new MobileLayoutUnlockGesture()
    };

    private void DrawMobileFixedControls(float scale)
    {
        for (int index=0;index<mobileFixedPointers.Length;index++)
        {
            var action=(MobileFixedAction)index;
            Rect rect=MobileClientPresentation.FixedActionRect(action);
            float controlScale=scale*rect.width/(52f*scale);
            MobileFixedActionState state=MobileFixedState(action);
            bool pressed=mobileFixedPointers[index].Pressed && !MobileClientPresentation.LayoutEditing;
            DrawMobileRoundFace(rect,controlScale,true,pressed);
            if (state.Icon!=null)
                MobileHudArtwork.Image(MobileHudArtwork.Inset(rect,6f*controlScale),
                    MobileHudArtwork.RoundTexture(state.Icon),pressed,state.Available || state.Active);
            else MobileHudArtwork.Label(rect,state.Label,controlScale);
            if (action==MobileFixedAction.HealthPotion || action==MobileFixedAction.ManaPotion)
            {
                DrawMobileCooldownFraction(MobileHudArtwork.Inset(rect,6f*controlScale),state.CooldownFraction);
                MobileHudArtwork.Label(new Rect(rect.x+rect.width*.45f,rect.yMax-18f*controlScale,
                    rect.width*.55f,18f*controlScale),state.Count.ToString(),controlScale*.8f);
            }
            if (state.Active) DrawMobileLayoutOutline(rect,new Color(.5f,1f,.58f,1f),2f*scale);
            float progress=index<3 ? mobileFixedPointers[index].Progress(Time.unscaledTime) : 0f;
            if (progress>0f)
                DrawMobileLayoutFill(new Rect(rect.x,rect.yMax+2f*scale,rect.width*progress,3f*scale),
                    new Color(1f,.84f,.4f,1f));
        }
    }

    private void DrawMobileCooldownFraction(Rect rect,float fraction)
    {
        if (cooldownFrames==null || fraction<=0f) return;
        int index=CooldownFrameIndexForIndependentRuntime(fraction);
        if ((uint)index >= cooldownFrames.Length) return;
        MobileHudArtwork.Image(rect,MobileHudArtwork.RoundTexture(cooldownFrames[index]),opacity:180f/255f);
    }

    private bool HandleMobileFixedPointer(int finger,Vector2 position,TouchPhase phase,bool panelPointer)
    {
        Vector2 point=MobileClientPresentation.GuiPoint(position);
        if (phase==TouchPhase.Began && showHotbar && !panelPointer)
            for (int index=0;index<mobileFixedPointers.Length;index++)
                if (MobileClientPresentation.FixedActionRect((MobileFixedAction)index).Contains(point))
                {
                    mobileShortcutPointers.Claim(finger);
                    mobileFixedPointers[index].Begin(finger,point.x,point.y,Time.unscaledTime,MobileClientPresentation.Scale);
                    return true;
                }
        for (int index=0;index<mobileFixedPointers.Length;index++)
        {
            MobileLayoutUnlockGesture gesture=mobileFixedPointers[index];
            if (gesture.Owner!=finger) continue;
            var action=(MobileFixedAction)index;
            MobileLayoutUnlockResult result=gesture.Sample(finger,point.x,point.y,Time.unscaledTime,
                phase==TouchPhase.Ended,phase==TouchPhase.Canceled || panelPointer,
                MobileClientPresentation.FixedActionRect(action).Contains(point));
            if (result==MobileLayoutUnlockResult.Unlock && index<3) EnterMobileLayoutEditor();
            else if (result==MobileLayoutUnlockResult.Tap && !MobileClientPresentation.GameplayInputBlocked)
                UseMobileFixedAction(action);
            return true;
        }
        return false;
    }
    // Resolve a hold before any other finger's release in the same physical batch.
    // Touch-array order must not decide whether entering the editor also casts a skill.
    private bool TryMobileLayoutUnlockBeforeActions()
    {
        for (int index=0;index<3;index++)
        {
            MobileLayoutUnlockGesture gesture=mobileFixedPointers[index];
            if (gesture.Owner==-1) continue;
            for (int touchIndex=0;touchIndex<Input.touchCount;touchIndex++)
            {
                Touch touch=Input.GetTouch(touchIndex);
                if (touch.fingerId!=gesture.Owner || touch.phase==TouchPhase.Canceled) continue;
                Vector2 point=MobileClientPresentation.GuiPoint(touch.position);
                bool inside=!MobileClientPresentation.IsPanelPointer(touch.position) &&
                    MobileClientPresentation.FixedActionRect((MobileFixedAction)index).Contains(point);
                if (!gesture.UnlockDue(touch.fingerId,point.x,point.y,Time.unscaledTime,inside)) continue;
                HandleMobileFixedPointer(touch.fingerId,touch.position,touch.phase,false);
                if (MobileClientPresentation.LayoutEditing) return true;
            }
            if (gesture.Owner==-2 && !UnityEngine.Application.isMobilePlatform && Input.touchCount==0 &&
                (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)))
            {
                Vector2 point=MobileClientPresentation.GuiPoint(Input.mousePosition);
                bool inside=!MobileClientPresentation.IsPanelPointer(Input.mousePosition) &&
                    MobileClientPresentation.FixedActionRect((MobileFixedAction)index).Contains(point);
                if (!gesture.UnlockDue(-2,point.x,point.y,Time.unscaledTime,inside)) continue;
                HandleMobileFixedPointer(-2,Input.mousePosition,Input.GetMouseButtonUp(0) ? TouchPhase.Ended : TouchPhase.Moved,false);
                if (MobileClientPresentation.LayoutEditing) return true;
            }
        }
        return false;
    }

    private void RefreshMobileFixedPointers()
    {
        for (int index=0;index<mobileFixedPointers.Length;index++)
            if (mobileFixedPointers[index].Owner!=-1 && !MobilePointerActive(mobileFixedPointers[index].Owner))
                mobileFixedPointers[index].Reset();
    }
    private void ResetMobileFixedPointers()
    {
        for (int index=0;index<mobileFixedPointers.Length;index++) mobileFixedPointers[index].Reset();
    }

    // Preflight ownership also protects IMGUI callbacks that run before GameHud.Update.
    private static bool MobileShortcutStarting
    {
        get
        {
            if (I==null || !I.showHotbar || !MobileClientPresentation.Enabled) return false;
            for (int index=0;index<Input.touchCount;index++)
            {
                Touch touch=Input.GetTouch(index);
                if (touch.phase==TouchPhase.Began && MobileShortcutStart(touch.position)) return true;
            }
            return !UnityEngine.Application.isMobilePlatform && Input.touchCount==0 &&
                Input.GetMouseButtonDown(0) && MobileShortcutStart(Input.mousePosition);
        }
    }
    private static bool MobileShortcutStart(Vector2 position)
    {
        if (MobileClientPresentation.IsPanelPointer(position)) return false;
        Vector2 point=MobileClientPresentation.GuiPoint(position);
        if (MobileClientPresentation.MenuToggleRect.Contains(point) ||
            (MobileClientPresentation.MenuExpanded && MobileClientPresentation.MenuRect.Contains(point))) return false;
        if (MobileClientPresentation.JoystickRect.Contains(point) || MobileClientPresentation.AttackRect.Contains(point) ||
            MobileClientPresentation.PickupRect.Contains(point) || MobileClientPresentation.PageRect.Contains(point) ||
            MobileClientPresentation.MountRect.Contains(point) || MobileClientPresentation.HealthPotionRect.Contains(point) ||
            MobileClientPresentation.ManaPotionRect.Contains(point) || MobileClientPresentation.SitRect.Contains(point)) return true;
        for (int index=0;index<6;index++) if (MobileClientPresentation.SkillRect(index).Contains(point)) return true;
        return false;
    }
}
}

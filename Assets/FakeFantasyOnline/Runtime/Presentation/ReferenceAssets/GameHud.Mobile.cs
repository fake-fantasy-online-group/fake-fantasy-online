using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    // Resolve before OnGUI as well as during drawing, so opening/closing and viewport
    // changes never leave stale panel bounds. Compact map controls remain the close route.
    internal static Rect MobileDiagramRect
    {
        get
        {
            if (I==null || !I.showMinimap || !I.diagramOpen || I.guiMinimap==null)
                return default(Rect);
            Rect safe=MobileClientPresentation.SafeAreaGui;
            Rect rect=ReferenceFirstWorldMinimapHud.DiagramRect(
                MobileClientPresentation.MinimapViewportWidth,(int)safe.height,
                I.guiMinimap.DiagramTexture,MobileClientPresentation.Scale);
            rect.x+=safe.x; rect.y+=safe.y;
            return rect;
        }
    }

    private void DrawMobileHud()
    {
        float scale=MobileClientPresentation.Scale;
        Matrix4x4 previous=GUI.matrix;
        float oldScale=s,oldUiScale=uiScale;
        bool ready=true;
        bool previousEnabled=GUI.enabled;
        GUI.enabled=previousEnabled && !MobileClientPresentation.GameplayInputBlocked;
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
        finally { GUI.matrix=previous; s=oldScale; uiScale=oldUiScale; GUI.enabled=previousEnabled; }
        // Reuse original resources, icons, cooldown masks and dispatch; no replacement artwork.
        if (showHotbar)
        {
            MobileHudArtwork.AttackFace(MobileClientPresentation.AttackRect,scale*MobileClientPresentation.ControlScale(MobileControlId.Attack),mobilePressed==6,
                CursorManager.I != null ? CursorManager.I.MobileAttackTexture(TargetSelect.CurrentSourceForIndependentRuntime) : null);
            MobileHudArtwork.PickupFace(MobileClientPresentation.PickupRect,scale*MobileClientPresentation.ControlScale(MobileControlId.Pickup),mobilePressed==8,
                CursorManager.I != null ? CursorManager.I.MobilePickupTexture : null);
            for (int k=0;k<6;k++)
            {
                int slot=MobileGroupWheel.HotbarSlot(mobilePage,k);
                Rect rect=MobileClientPresentation.SkillRect(k);
                float controlScale=scale*MobileClientPresentation.ControlScale((MobileControlId)((int)MobileControlId.Skill0+k));
                DrawMobileRoundFace(rect,controlScale,(uint)slot<24 && hotSkill[slot]!=0,
                    mobileFinger!=-1 && mobilePressed==k);
                if ((uint)slot<24)
                {
                    Texture2D icon=HotIconForIndependentRuntime(slot);
                    // Both artwork and cooldown are alpha-masked circles, not squares inside a rim.
                    Rect iconRect=MobileHudArtwork.Inset(rect,MobileClientPresentation.SkillIconPadding*controlScale);
                    MobileHudArtwork.Image(iconRect,MobileHudArtwork.RoundTexture(icon),
                        mobileFinger!=-1 && mobilePressed==k);
                    DrawMobileRoundCooldown(iconRect,hotSkill[slot]);
                }
            }
            if (!MobileClientPresentation.GroupWheelOpen)
            {
                DrawMobileRoundFace(MobileClientPresentation.PageRect,scale*MobileClientPresentation.ControlScale(MobileControlId.GroupWheel),true,mobilePressed==7);
                MobileHudArtwork.Label(MobileClientPresentation.PageRect,(mobilePage+1)+"/4",scale*MobileClientPresentation.ControlScale(MobileControlId.GroupWheel));
            }
            MobileHudArtwork.Joystick(MobileClientPresentation.JoystickRect,ClientInput.MovementAxis,
                scale*MobileClientPresentation.ControlScale(MobileControlId.Joystick));
            DrawMobileFixedControls(scale);
        }
        if (showMenu && !MobileClientPresentation.GameplayInputBlocked)
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
        if (mobileFinger!=-1 && mobilePressed<6)
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
        if (MobileClientPresentation.GroupWheelOpen && showHotbar)
            DrawMobileGroupWheel(scale);
        DrawMobileLayoutEditor(scale);
        LastGuiDrawSucceededForIndependentRuntime=ready;
    }
    private GUIStyle mobileGroupWheelStyle;
    private void DrawMobileGroupWheel(float scale)
    {
        scale*=MobileClientPresentation.ControlScale(MobileControlId.GroupWheel);
        if (mobileGroupWheelStyle==null)
            mobileGroupWheelStyle=new GUIStyle(GUIStyle.none) {
                font=NpcChatter.CnFont(), alignment=TextAnchor.MiddleCenter,
                clipping=TextClipping.Overflow, richText=false, wordWrap=false
            };
        Vector2 center=MobileClientPresentation.GroupWheelCenter;
        // Flat translucent disc, thin perimeter and selected sector match the design preview.
        // Numbers remain unboxed; the center boundary matches the actual cancel radius.
        float diameter=(MobileClientPresentation.GroupWheelRadius+24f*scale)*2f;
        int pending=mobileGroupWheel.Hovered;
        MobileHudArtwork.Image(new Rect(center.x-diameter*.5f,center.y-diameter*.5f,diameter,diameter),
            MobileGroupWheelArtwork.Face(pending));
        if (pending>=0)
        {
            MobileGroupWheel.Direction(pending,out float arrowX,out float arrowY);
            Vector2 arrow=center+new Vector2(arrowX,arrowY)*42f*scale;
            string glyph=pending==0 ? "↑" : pending==1 ? "→" : pending==2 ? "↓" : "←";
            DrawMobileGroupText(new Rect(arrow.x-12f*scale,arrow.y-12f*scale,24f*scale,24f*scale),
                glyph,20f,new Color(1f,.88f,.48f,1f),scale);
        }
        for (int group=0;group<4;group++)
        {
            MobileGroupWheel.Direction(group,out float x,out float y);
            Vector2 position=center+new Vector2(x,y)*MobileClientPresentation.GroupWheelRadius;
            bool hovered=mobileGroupWheel.Hovered==group;
            Rect label=new Rect(position.x-24f*scale,position.y-24f*scale,48f*scale,48f*scale);
            Color color=hovered ? new Color(1f,.88f,.48f,1f) :
                group==mobilePage ? new Color(.82f,.69f,.40f,1f) : Color.white;
            DrawMobileGroupText(label,(group+1).ToString(),hovered ? 30f : 22f,color,scale);
            if (group==mobilePage)
                DrawMobileGroupText(new Rect(position.x-10f*scale,position.y+12f*scale,20f*scale,12f*scale),
                    "•",12f,new Color(.82f,.69f,.40f,1f),scale);
        }
        DrawMobileGroupText(new Rect(center.x-28f*scale,center.y-16f*scale,56f*scale,32f*scale),
            "取消",12f*MobileClientPresentation.TextSizeMultiplier,new Color(.8f,.8f,.8f,1f),scale);
    }
    private void DrawMobileGroupText(Rect rect,string text,float size,Color color,float scale)
    {
        mobileGroupWheelStyle.fontSize=Mathf.Max(1,Mathf.RoundToInt(size*scale));
        mobileGroupWheelStyle.normal.textColor=new Color(0f,0f,0f,.8f);
        GUI.Label(new Rect(rect.x+scale,rect.y+scale,rect.width,rect.height),text,mobileGroupWheelStyle);
        mobileGroupWheelStyle.normal.textColor=color;
        GUI.Label(rect,text,mobileGroupWheelStyle);
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

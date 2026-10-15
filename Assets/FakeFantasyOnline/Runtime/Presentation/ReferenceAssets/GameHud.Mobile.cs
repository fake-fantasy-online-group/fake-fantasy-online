using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
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
            if (!MobileClientPresentation.GroupWheelOpen)
            {
                DrawMobileRoundFace(MobileClientPresentation.PageRect,scale,true,mobilePressed==5);
                MobileHudArtwork.Label(MobileClientPresentation.PageRect,(mobilePage+1)+"/8",scale);
            }
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
        if (MobileClientPresentation.GroupWheelOpen && showHotbar && !diagramOpen)
            DrawMobileGroupWheel(scale);
        LastGuiDrawSucceededForIndependentRuntime=ready;
    }
    private GUIStyle mobileGroupWheelStyle;
    private void DrawMobileGroupWheel(float scale)
    {
        if (mobileGroupWheelStyle==null)
            mobileGroupWheelStyle=new GUIStyle(GUIStyle.none) {
                font=NpcChatter.CnFont(), alignment=TextAnchor.MiddleCenter,
                clipping=TextClipping.Overflow, richText=false, wordWrap=false
            };
        Vector2 center=MobileClientPresentation.GroupWheelCenter;
        // Only text: no number frames, ring, sector edges, or spokes.
        for (int group=0;group<8;group++)
        {
            MobileGroupWheel.Direction(group,out float x,out float y);
            Vector2 position=center+new Vector2(x,y)*MobileClientPresentation.GroupWheelRadius;
            bool hovered=mobileGroupWheel.Hovered==group;
            Rect label=new Rect(position.x-24f*scale,position.y-24f*scale,48f*scale,48f*scale);
            Color color=hovered ? new Color(1f,.88f,.48f,1f) :
                group==mobilePage ? new Color(.73f,.84f,.92f,1f) : Color.white;
            DrawMobileGroupText(label,(group+1).ToString(),hovered ? 30f : 22f,color,scale);
        }
        DrawMobileGroupText(new Rect(center.x-28f*scale,center.y-16f*scale,56f*scale,32f*scale),
            "取消",12f,new Color(.8f,.8f,.8f,1f),scale);
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

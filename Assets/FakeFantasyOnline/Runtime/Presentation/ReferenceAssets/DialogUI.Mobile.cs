using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    private int mobileOptionPage, mobileTextPage, mobilePressed = -1, mobileActionFrame = -1;
    private Vector2 mobileDown;
    private string mobileGreeting, mobileNpc;
    private readonly List<Action> mobileActions = new List<Action>();
    private List<string> mobileTextPages;
    private float mobileTextWidth, mobileTextHeight, mobileScale;

    private bool mobileTouchBlocked, mobileFocused = true, mobilePaused;
    private Action mobileLastAction;
    private float mobileLastActionTime = float.NegativeInfinity;

    private void Update()
    {
        if (!MobileClientPresentation.Enabled) return;
        bool cancelled = false;
        for (int i=0;i<Input.touchCount;i++)
            if (Input.GetTouch(i).phase==TouchPhase.Canceled) cancelled=true;
        ObserveMobileTouches(Input.touchCount,cancelled);
    }
    private void ObserveMobileTouches(int count,bool cancelled)
    {
        if (count>1 || cancelled)
        { mobilePressed=-1; mobileTouchBlocked=true; }
        else if (count==0) mobileTouchBlocked=false;
        if (!Visible || !mobileFocused || mobilePaused) mobilePressed=-1;
    }
    private void OnApplicationFocus(bool focused)
    {
        mobileFocused=focused;
        if (MobileClientPresentation.Enabled) { mobilePressed=-1; mobileTouchBlocked=true; }
    }
    private void OnApplicationPause(bool paused)
    {
        mobilePaused=paused;
        if (MobileClientPresentation.Enabled) { mobilePressed=-1; mobileTouchBlocked=true; }
    }

    private void ResetMobileDialog()
    {
        mobileOptionPage=mobileTextPage=0; mobilePressed=-1; mobileTextPages=null;
        mobileActions.Clear(); mobileGreeting=null; mobileNpc=null;
    }
    private MobileNpcDialogLayout SyncMobileDialog()
    {
        var layout=new MobileNpcDialogLayout(MobileClientPresentation.SafeAreaGui,
            MobileClientPresentation.Scale,optLabels.Count);
        bool changed=mobileNpc!=NpcName || mobileGreeting!=Greeting || mobileActions.Count!=optActs.Count;
        if(!changed) for(int i=0;i<optActs.Count;i++) if(!ReferenceEquals(mobileActions[i],optActs[i])) { changed=true; break; }
        if(changed)
        {
            ResetMobileDialog(); mobileNpc=NpcName; mobileGreeting=Greeting; mobileActions.AddRange(optActs);
        }
        if(mobileTextWidth!=layout.Body.width || mobileTextHeight!=layout.Body.height || mobileScale!=layout.Scale)
        { mobileTextPages=null; mobileTextPage=0; mobilePressed=-1; }
        mobileTextWidth=layout.Body.width; mobileTextHeight=layout.Body.height; mobileScale=layout.Scale;
        mobileOptionPage=Mathf.Clamp(mobileOptionPage,0,layout.PageCount-1);
        return layout;
    }
    private int MobileHit(MobileNpcDialogLayout layout, Vector2 point)
    {
        if(layout.Close.Contains(point)) return 0;
        if(layout.TextPrevious.Contains(point)) return 1;
        if(layout.TextNext.Contains(point)) return 2;
        if(layout.Previous.Contains(point)) return 3;
        if(layout.Next.Contains(point)) return 4;
        for(int slot=0;slot<layout.RowsPerPage;slot++)
        {
            int index=mobileOptionPage*layout.RowsPerPage+slot;
            if(index<optLabels.Count && layout.Row(slot).Contains(point)) return index+5;
        }
        return -1;
    }
    private bool ActivateMobileControl(int control, MobileNpcDialogLayout layout)
    {
        if(control<0 || mobileActionFrame==Time.frameCount || mobileTouchBlocked || !mobileFocused || mobilePaused) return false;
        mobileActionFrame=Time.frameCount; mobilePressed=-1;
        switch(control)
        {
            case 0: Close(); return true;
            case 1: mobileTextPage=Math.Max(0,mobileTextPage-1); return true;
            case 2: mobileTextPage=Math.Min((mobileTextPages?.Count??1)-1,mobileTextPage+1); return true;
            case 3: mobileOptionPage=Math.Max(0,mobileOptionPage-1); return true;
            case 4: mobileOptionPage=Math.Min(layout.PageCount-1,mobileOptionPage+1); return true;
            default:
                int index=control-5;
                if(index>=optActs.Count) return false;
                Action action=optActs[index];
                // Short mobile double-tap suppression, not a task pending state.
                // The existing action may retry normally after 350ms; no server
                // acknowledgement or protocol behavior is invented here.
                if(ReferenceEquals(action,mobileLastAction) && Time.unscaledTime-mobileLastActionTime<.35f) return false;
                mobileLastAction=action; mobileLastActionTime=Time.unscaledTime;
                return ActivateOption(index);
        }
    }
    private bool HandleMobilePrimaryDown(Vector2 pointer)
    {
        var layout=SyncMobileDialog();
        return ActivateMobileControl(MobileHit(layout,pointer),layout);
    }
    private void DrawMobileDialog()
    {
        var layout=SyncMobileDialog(); float scale=layout.Scale;
        BuildStyles(scale); bfs=scale;
        nameTextStyle.fontSize=greetingTextStyle.fontSize=optionTextStyle.fontSize=Mathf.Max(1,Mathf.RoundToInt(14f*scale));
        optionTextStyle.wordWrap=true;
        if(mobileTextPages==null)
            mobileTextPages=MobileNpcDialogLayout.TextPages(Greeting,layout.Body.width,
                Mathf.Max(1,Mathf.FloorToInt(layout.Body.height/(20f*scale))),
                c=>BitmapFont.Ready ? BitmapFont.Measure(c.ToString(),scale) : greetingTextStyle.CalcSize(new GUIContent(c.ToString())).x);
        GUI.DrawTexture(layout.Panel,win);
        GUI.Label(new Rect(layout.Body.x,layout.Panel.y,layout.Body.width-layout.RowHeight,layout.RowHeight),NpcName,nameTextStyle);
        if(closeTex!=null) GUI.DrawTexture(layout.Close,closeTex); else GUI.Label(layout.Close,"×",optionTextStyle);
        DrawWrapped(layout.Body.x,layout.Body.y,layout.Body.width,layout.Body.height,mobileTextPages[mobileTextPage],greetingTextStyle,20f*scale);
        DrawMobileControl(layout.TextPrevious,"‹"); DrawMobileControl(layout.TextNext,"›");
        GUI.Label(new Rect(layout.TextPrevious.xMax,layout.TextPrevious.y,layout.Body.width-2*layout.RowHeight,layout.RowHeight),
            "正文 "+(mobileTextPage+1)+" / "+mobileTextPages.Count,optionTextStyle);
        for(int slot=0;slot<layout.RowsPerPage;slot++)
        {
            int index=mobileOptionPage*layout.RowsPerPage+slot;
            if(index>=optLabels.Count) break;
            DrawMobileControl(layout.Row(slot),optLabels[index]);
        }
        DrawMobileControl(layout.Previous,"‹"); DrawMobileControl(layout.Next,"›");
        GUI.Label(new Rect(layout.Previous.xMax,layout.Previous.y,layout.Body.width-2*layout.RowHeight,layout.RowHeight),
            "选项 "+(mobileOptionPage+1)+" / "+layout.PageCount,optionTextStyle);
        HandleMobileEvent(layout,Event.current);
    }
    private void HandleMobileEvent(MobileNpcDialogLayout layout,Event e)
    {
        if (mobileTouchBlocked || !mobileFocused || mobilePaused)
        {
            mobilePressed=-1;
            if(e.type==EventType.MouseDown || e.type==EventType.MouseUp || e.type==EventType.MouseDrag || e.type==EventType.ScrollWheel) e.Use();
            return;
        }
        // One release routes one original action. A rebuild, resize, drag or cancelled
        // press cannot activate a new row under an old finger. GUI buttons do not
        // dispatch a second synthetic mouse action alongside this route.
        if(e.type==EventType.MouseDown && e.button==0)
        { mobilePressed=MobileHit(layout,e.mousePosition); mobileDown=e.mousePosition; e.Use(); }
        else if(e.type==EventType.MouseDrag)
        { if((e.mousePosition-mobileDown).sqrMagnitude>100f*layout.Scale*layout.Scale) mobilePressed=-1; e.Use(); }
        else if(e.type==EventType.MouseUp && e.button==0)
        {
            int hit=MobileHit(layout,e.mousePosition), pressed=mobilePressed; mobilePressed=-1;
            if(hit==pressed) ActivateMobileControl(hit,layout);
            e.Use();
        }
        else if(e.type==EventType.ScrollWheel) e.Use();
    }
    private void DrawMobileControl(Rect rect,string text)
    {
        if(selTex!=null) GUI.DrawTexture(rect,selTex);
        GUI.Label(rect,text,optionTextStyle);
    }
}
}

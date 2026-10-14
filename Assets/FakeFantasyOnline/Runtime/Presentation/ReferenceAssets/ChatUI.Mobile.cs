using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    private List<Line> mobileLines;

    private void DrawMobileChat()
    {
        s=MobileClientPresentation.Scale;
        Rect panel=MobileClientPresentation.ChatRect;
        float button=44f*s;
        DrawPanel(panel);
        Rect toggle=new Rect(panel.xMax-button,panel.y,button,button);
        if (GUI.Button(toggle,MobileClientPresentation.ChatExpanded ? "−" : "+"))
        {
            MobileClientPresentation.ChatExpanded=!MobileClientPresentation.ChatExpanded;
            if (MobileClientPresentation.ChatExpanded) MobileClientPresentation.MenuExpanded=false;
        }
        Rect channelRect=new Rect(panel.x,panel.y,64f*s,button);
        if (GUI.Button(channelRect,channel)) { chIdx=(Array.IndexOf(Channels,channel)+1)%Channels.Length; channel=Channels[chIdx]; }
        if (mobileLines==null) mobileLines=new List<Line>();
        CollectMobileLines(mobileLines);
        DrawLines(mobileLines,new Rect(panel.x,panel.y+button,panel.width,panel.height-button*2),true);
        Rect bar=new Rect(panel.x,panel.yMax-button,panel.width,button);
        if (inputBar!=null) GUI.DrawTexture(bar,inputBar);
        EnsureEditStyle();
        string before=inputText;
        inputText=GuardShareEdit(before,UiInput.Field(new Rect(bar.x+4*s,bar.y+2*s,bar.width-62*s,bar.height-4*s),
            "chatinput",before,120,editSt,s),pendingRefs);
        if (_pendingFocus) { GUI.FocusControl("chatinput"); _pendingFocus=false; }
        TypingNow=GUI.GetNameOfFocusedControl()=="chatinput" || HornDialog.Typing || DuelDlg.Typing;
        if (GUI.Button(new Rect(bar.xMax-56*s,bar.y,56*s,button),"发送")) { SendNow(); GUI.FocusControl(null); }
        Event current=Event.current;
        if (TypingNow && current.type==EventType.KeyDown && (current.keyCode==KeyCode.Return || current.keyCode==KeyCode.KeypadEnter))
        { SendNow(); GUI.FocusControl(null); current.Use(); }
    }
}
}

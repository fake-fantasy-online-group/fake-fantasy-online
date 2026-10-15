using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    private List<Line> mobileLines;
    private GUIStyle mobileChatLabelStyle;

    private void DrawMobileChat()
    {
        s = MobileClientPresentation.Scale;
        Rect panel = MobileClientPresentation.ChatRect;
        float control = 44f * s;
        // The collapsed 112-unit footprint reserves 68 units for three existing
        // 18-unit message rows. Channel/send/expand share one touch band below it.
        // Only explicit expansion draws the original message-window frame.
        Rect messages = new Rect(panel.x, panel.y, panel.width, Mathf.Max(0f, panel.height - control));
        if (MobileClientPresentation.ChatExpanded) DrawPanel(messages);
        if (mobileLines == null) mobileLines = new List<Line>();
        CollectMobileLines(mobileLines);
        DrawLines(mobileLines, messages, true);

        Rect bar = new Rect(panel.x, panel.yMax - control, panel.width - control, control);
        Rect channelRect = new Rect(bar.x, bar.y, 56f * s, control);
        Rect sendRect = new Rect(bar.xMax - control, bar.y, control, control);
        Rect inputRect = new Rect(channelRect.xMax + 4f * s, bar.y,
            Mathf.Max(0f, sendRect.x - channelRect.xMax - 8f * s), control);
        Rect toggle = new Rect(bar.xMax, bar.y, control, control);
        Rect barArtwork = new Rect(bar.x, bar.y + 6f * s, bar.width, 32f * s);
        if (inputBar != null) GUI.DrawTexture(barArtwork, inputBar);
        else ArtworkSkin.Input(barArtwork, s);

        Event current = Event.current;
        DrawMobileChatLabel(new Rect(channelRect.x + 2f * s, channelRect.y,
            36f * s, control), channel);
        DrawMobileChatIcon(new Rect(channelRect.xMax - 18f * s, bar.y + 14f * s,
            16f * s, 16f * s), channelRect, dropBtn);
        if (GUI.Button(channelRect, GUIContent.none, GUIStyle.none))
        {
            chIdx = (Array.IndexOf(Channels, channel) + 1) % Channels.Length;
            channel = Channels[chIdx];
            // Match selecting a normal channel in the original channel picker.
            whisperTarget = string.Empty;
        }

        EnsureEditStyle();
        bool inputFocused = GUI.GetNameOfFocusedControl() == "chatinput";
        // Capture before TextField can consume Return. Horn/duel typing is not
        // ownership of this input and must never send the pending chat text.
        bool enterPressed = inputFocused && current != null && current.type == EventType.KeyDown &&
            (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter);
        string before = inputText;
        inputText = GuardShareEdit(before,
            UiInput.Field(inputRect, "chatinput", before, 120, editSt, s), pendingRefs);
        if (_pendingFocus)
        {
            GUI.FocusControl("chatinput");
            _pendingFocus = false;
        }
        bool focused = GUI.GetNameOfFocusedControl() == "chatinput";
        TypingNow = focused || HornDialog.Typing || DuelDlg.Typing;
        if (string.IsNullOrEmpty(inputText) && !focused)
            DrawMobileChatLabel(inputRect, "点击输入");

        DrawMobileChatIcon(new Rect(sendRect.x + 8f * s, sendRect.y + 8f * s,
            28f * s, 28f * s), sendRect, sendBtn);
        if (!HasMobileChatIcon(sendBtn)) DrawMobileChatLabel(sendRect, "发送");
        if (GUI.Button(sendRect, GUIContent.none, GUIStyle.none))
        {
            SendNow();
            GUI.FocusControl(null);
        }
        if (enterPressed)
        {
            SendNow();
            GUI.FocusControl(null);
            current.Use();
        }

        DrawMobileChatIcon(new Rect(toggle.x + 10f * s, toggle.y + 1f * s,
            24f * s, 24f * s), toggle, sizeBtn);
        DrawMobileChatLabel(new Rect(toggle.x, toggle.y + 26f * s, toggle.width, 18f * s),
            MobileClientPresentation.ChatExpanded ? "收起" : "展开");
        if (GUI.Button(toggle, GUIContent.none, GUIStyle.none))
        {
            MobileClientPresentation.ChatExpanded = !MobileClientPresentation.ChatExpanded;
            if (MobileClientPresentation.ChatExpanded) MobileClientPresentation.MenuExpanded = false;
        }
    }

    private static bool HasMobileChatIcon(Texture2D[] frames)
    {
        if (frames == null) return false;
        for (int index = 0; index < frames.Length; index++)
            if (frames[index] != null) return true;
        return false;
    }

    private void DrawMobileChatIcon(Rect art, Rect hit, Texture2D[] frames)
    {
        if (!HasMobileChatIcon(frames)) return;
        bool hover = Event.current != null && hit.Contains(Event.current.mousePosition);
        ArtworkSkin.IconFace(art, frames, hover, hover && Input.GetMouseButton(0));
    }

    private void DrawMobileChatLabel(Rect rect, string text)
    {
        // Use the same bitmap alphabet as the messages and original HUD. The
        // named Chinese-font fallback also scales; never use Unity's tiny skin.
        BitmapFont.Measure(text, s);
        if (BitmapFont.Ready)
        {
            BitmapFont.CenterOutline(rect, text, new Color32(255, 230, 160, 255), s);
            return;
        }
        if (mobileChatLabelStyle == null)
        {
            mobileChatLabelStyle = new GUIStyle(GUIStyle.none)
            {
                font = NpcChatter.CnFont(), alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip, wordWrap = false,
                padding = new RectOffset(0, 0, 0, 0)
            };
            mobileChatLabelStyle.normal.textColor = new Color(1f, .90f, .63f, 1f);
        }
        mobileChatLabelStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(12f * s));
        GUI.Label(rect, text, mobileChatLabelStyle);
    }
}
}

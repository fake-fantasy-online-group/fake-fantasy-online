using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    private List<Line> mobileLines;
    private GUIStyle mobileChatLabelStyle;
    private bool mobileExpandPressed;
    private readonly object mobileChannelPanelOwner = new object();

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) mobileExpandPressed = false;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) mobileExpandPressed = false;
    }

    private void OnDisable()
    {
        mobileExpandPressed = false;
        MobileClientPresentation.RemovePanel(mobileChannelPanelOwner);
    }

    private Rect MobileChannelPanelRect()
    {
        Rect chat = MobileClientPresentation.ChatRect;
        float scale = MobileClientPresentation.Scale;
        float height = (Channels.Length * 44f + 8f) * scale;
        return new Rect(chat.x, chat.yMax - 44f * scale - height,
            104f * scale, height);
    }

    private void DrawMobileChannelPanel()
    {
        Rect panel = MobileChannelPanelRect();
        MobileClientPresentation.RegisterPanel(mobileChannelPanelOwner, panel);
        ArtworkSkin.Panel(panel, s);
        for (int index = 0; index < Channels.Length; index++)
        {
            Rect row = new Rect(panel.x + 4f * s, panel.y + (4f + index * 44f) * s,
                panel.width - 8f * s, 44f * s);
            ArtworkSkin.Row(row, s, channel == Channels[index]);
            DrawMobileChatLabel(row, Channels[index]);
            if (GUI.Button(row, GUIContent.none, GUIStyle.none))
            {
                chIdx = index;
                channel = Channels[index];
                whisperTarget = string.Empty;
                chanOpen = false;
                MobileClientPresentation.RemovePanel(mobileChannelPanelOwner);
            }
        }
    }

    private void DrawMobileChat()
    {
        s = MobileClientPresentation.Scale;
        // Details belong to the original tabbed history window, never a taller HUD.
        MobileClientPresentation.ChatExpanded = false;
        Rect panel = MobileClientPresentation.ChatRect;
        float control = 44f * s;
        // Reserve three enlarged message rows (24 to 30 units as size increases).
        // Channel/send share one touch band below the passive preview.
        // Opening details keeps this three-line preview and composer compact.
        Rect expand = new Rect(panel.xMax - control, panel.y, control, control);
        // Keep the original viewbtn face clear of message text and the composer.
        Rect messages = new Rect(panel.x, panel.y, Mathf.Max(0f, panel.width - control - 4f * s),
            Mathf.Max(0f, panel.height - control));
        Event current = Event.current;
        if (mobileLines == null) mobileLines = new List<Line>();
        CollectMobileLines(mobileLines);
        float layoutScale = s;
        try
        {
            s = layoutScale * MobileClientPresentation.TextSizeMultiplier;
            DrawLines(mobileLines, messages, true, false);
        }
        finally { s = layoutScale; }
        // Only the explicit upper-right icon opens the original tabbed history.
        // Compact message rows remain display-only, including long presses.
        Texture2D[] expandFrames = sideBtn != null && sideBtn.Length > 0 && HasMobileChatIcon(sideBtn[0])
            ? sideBtn[0] : sizeBtn;
        DrawMobileChatIcon(new Rect(expand.x + 6f * s, expand.y + 6f * s,
            32f * s, 32f * s), expand, expandFrames);
        if (!HasMobileChatIcon(expandFrames)) DrawMobileChatLabel(expand, "展开");
        bool touchCanceled = Input.touchCount > 1;
        if (mobileExpandPressed)
            for (int index = 0; index < Input.touchCount; index++)
                if (Input.GetTouch(index).phase == TouchPhase.Canceled) touchCanceled = true;
        bool blocked = GameHud.MobileShortcutPointerOwned || MobileClientPresentation.ModalOpen || chanOpen || touchCanceled;
        if (blocked) mobileExpandPressed = false;
        if (current != null && current.button == 0 && !blocked)
        {
            if (current.type == EventType.MouseDown)
            {
                mobileExpandPressed = expand.Contains(current.mousePosition);
                if (mobileExpandPressed) current.Use();
            }
            if (current.type == EventType.MouseDrag && !expand.Contains(current.mousePosition))
                mobileExpandPressed = false;
            if (current.type == EventType.MouseUp)
            {
                if (mobileExpandPressed && expand.Contains(current.mousePosition))
                {
                    Win05.Open("historybar");
                    MobileClientPresentation.MenuExpanded = false;
                    current.Use();
                }
                mobileExpandPressed = false;
            }
        }

        Rect bar = new Rect(panel.x, panel.yMax - control, panel.width, control);
        Rect channelRect = new Rect(bar.x, bar.y, 64f * s, control);
        Rect sendRect = new Rect(bar.xMax - control, bar.y, control, control);
        Rect inputRect = new Rect(channelRect.xMax + 4f * s, bar.y,
            Mathf.Max(0f, sendRect.x - channelRect.xMax - 8f * s), control);
        Rect barArtwork = new Rect(bar.x, bar.y + 6f * s, bar.width, 32f * s);
        if (inputBar != null) GUI.DrawTexture(barArtwork, inputBar);
        else ArtworkSkin.Input(barArtwork, s);

        if (chanOpen && current != null && current.type == EventType.MouseDown &&
            !MobileChannelPanelRect().Contains(current.mousePosition) && !channelRect.Contains(current.mousePosition))
            chanOpen = false;
        if (!chanOpen) MobileClientPresentation.RemovePanel(mobileChannelPanelOwner);
        DrawMobileChatLabel(new Rect(channelRect.x + 2f * s, channelRect.y,
            44f * s, control), channel);
        DrawMobileChatIcon(new Rect(channelRect.xMax - 18f * s, bar.y + 14f * s,
            16f * s, 16f * s), channelRect, dropBtn);
        if (GUI.Button(channelRect, GUIContent.none, GUIStyle.none))
        {
            chanOpen = !chanOpen;
            mobileExpandPressed = false;
        }

        EnsureEditStyle();
        editSt.fontSize = Mathf.Max(1, Mathf.RoundToInt(12f * s * MobileClientPresentation.TextSizeMultiplier));
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

        DrawMobileChatIcon(new Rect(sendRect.x + 6f * s, sendRect.y + 6f * s,
            32f * s, 32f * s), sendRect, sendBtn);
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

        if (chanOpen) DrawMobileChannelPanel();
        else MobileClientPresentation.RemovePanel(mobileChannelPanelOwner);
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
        float textScale = s * MobileClientPresentation.TextSizeMultiplier;
        BitmapFont.Measure(text, textScale);
        if (BitmapFont.Ready)
        {
            BitmapFont.CenterOutline(rect, text, new Color32(255, 230, 160, 255), textScale);
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
        mobileChatLabelStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(12f * s * MobileClientPresentation.TextSizeMultiplier));
        GUI.Label(rect, text, mobileChatLabelStyle);
    }
}
}

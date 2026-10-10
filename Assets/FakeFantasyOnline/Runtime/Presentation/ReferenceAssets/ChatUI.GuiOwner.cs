using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    private void OnGUI()
    {
        // 060000f6: this real Unity message does not depend on a map borrower.
        // The original depth/Load prefix precedes the live HUD gate, including between maps.
        GUI.depth = 50;
        if (!loaded) Load();
        GameHud hud = GameHud.I;
        if (hud == null || !hud.ShowChatForIndependentRuntime)
        {
            return;
        }

        // Ordered >0 includes +Infinity and rejects NaN, exactly as the native comparison.
        float ownerScale = hud.uiScale;
        s = ownerScale > 0f ? ownerScale : UIScale.Factor;
        LoadFilter();
        _ = Screen.height;
        float scale = s;
        int viewportHeight = Screen.height;
        int viewportWidth = Screen.width;
        float bottomHeight = GameHud.BottomHotbarHeight(scale, (float)viewportWidth);
        float barY = (float)viewportHeight + scale * -36f;
        Event current = Event.current;
        barY -= bottomHeight;
        // The native Unity inequality skips this texture only. The field is re-read for draw.
        if (inputBar != null)
        {
            GUI.DrawTexture(new Rect(0f, barY, 418f * s, 30f * s), inputBar);
        }

        Rect bar = new Rect(0f, barY, 418f * scale, 30f * scale);
        if (BitmapFont.Ready)
        {
            scale = s;
            BitmapFont.DrawOutline(
                8f * scale,
                bar.y + 12f * scale,
                channel,
                new Color32(180, 255, 180, 255),
                scale);
        }

        scale = s;
        DrawBtn(
            new Rect(62f * scale, bar.y + 10f * scale, 16f * scale, 16f * scale),
            dropBtn,
            current,
            ToggleChan);

        scale = s;
        if (current.type == EventType.MouseDown && current.button == 0 &&
            new Rect(4f * scale, bar.y + 10f * scale, 58f * scale, 16f * scale)
                .Contains(current.mousePosition))
        {
            ToggleChan();
            current.Use();
        }

        scale = s;
        DrawBtn(
            new Rect(380f * scale, bar.y + 10f * scale, 16f * scale, 16f * scale),
            sendBtn,
            current,
            SendNow);

        // SendNow's live callback has already run. Later controls read the current owner
        // scale/state; only the bar top remains the original per-pass layout observation.
        scale = s;
        DrawBtn(
            new Rect(396f * scale, bar.y + 10f * scale, 16f * scale, 16f * scale),
            faceBtn,
            current,
            ToggleFacePanel);

        DrawInput(bar, current);

        // One native branch owns both expanded content and its side strip. Do not choose
        // collapsed/expanded again after a row/menu/banner callback has run.
        bool expanded = panelsVisible;
        scale = s;
        if (expanded)
        {
            Rect panel = new Rect(24f * scale, bar.y - 154f * scale,
                390f * scale, 154f * scale);
            DrawPanel(panel);
            if (sysSeqSeen != sysSeq)
            {
                sysSeqSeen = sysSeq;
                sysShownTime = Time.unscaledTime;
            }
            ChatUI.PartitionLinesForIndependentRuntime();
            float systemHeight = 0f;
            if (_sysTmp.Count > 0 &&
                Time.unscaledTime - sysShownTime < 60f)
            {
                systemHeight = s * 66f;
                DrawLines(_sysTmp,
                    new Rect(panel.x, panel.y, panel.width, systemHeight), false);
                GUI.color = new Color(1f, .85f, .4f, .45f);
                scale = s;
                GUI.DrawTexture(new Rect(panel.x + scale * 6f, (panel.y + systemHeight) - scale,
                    panel.width + scale * -12f, 1f), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            DrawLines(_chatTmp,
                new Rect(panel.x, panel.y + systemHeight, panel.width, panel.height - systemHeight), true);
            DrawBanner(panel);
        }

        DrawSideButtons(bar.y, s, current, expanded);
        if (faceOpen)
        {
            scale = s;
            float width = EmotePicker.PanelW(scale);
            float height = EmotePicker.PanelH(scale);
            int emoteId = EmotePicker.Draw(
                new Rect(412f * scale - width, barY - height - 3f * scale, width, height),
                current,
                s);
            if (emoteId > 0)
            {
                inputText = inputText + "\\e" + emoteId.ToString("D2");
                faceOpen = false;
                GUI.FocusControl("chatinput");
            }
        }

        if (chanOpen)
        {
            DrawChannelPanel(bar.y, s, current);
        }
        if (filterOpen)
        {
            DrawFilterPanel(bar.y, s, current);
        }

    }

    private void DrawInput(Rect bar, Event current)
    {
        EnsureEditStyle();
        float scale = s;
        Rect rect = new Rect(
            82f * scale,
            bar.y + 9f * scale,
            296f * scale,
            17f * scale);

        // Official ChatUI.OnGUI (0x060000f6, arm64 0x235780 / x86_64 0x2433b0) reads
        // GUI.GetNameOfFocusedControl() and compares it with the literal "chatinput" (metadata
        // usage 14588) twice: once here, after the edit style and before UiInput.Field, and once
        // again after the pending-focus request has been consumed. The first result is the one the
        // Return/KeypadEnter arm below branches on, so that arm is a keyboard-owner decision and
        // not an emptiness test on the input text.
        bool inputFocused = string.Equals(
            GUI.GetNameOfFocusedControl(),
            "chatinput",
            StringComparison.Ordinal);

        // Preserve only an unconsumed key entering this field. Reading rawType afterward
        // would also revive keys masked by a modal or consumed by a different window.
        bool enterPressed = current != null && current.type == EventType.KeyDown &&
            (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter);

        // Native captures the old value before Field and retains it through the call. A
        // later re-read is not the old argument when a field/menu callback changes state.
        string oldInput = inputText;
        string edited = UiInput.Field(
            rect,
            "chatinput",
            oldInput,
            120,
            editSt,
            s);
        inputText = ChatUI.GuardShareEdit(oldInput, edited, pendingRefs);

        if (_pendingFocus)
        {
            GUI.FocusControl("chatinput");
            _pendingFocus = false;
        }

        // Official 0x060000f6: the second comparison is not the whole value. When the focused
        // control is not "chatinput" the native body falls through to HornDialog.Typing
        // (usage 1783, static slot +8) and then to DuelDlg.Typing (usage 1590, static slot +0x50):
        // a non-zero horn flag makes the value true, otherwise the value is the duel flag. Both
        // architectures print the same three-way expression, so the typing flag is "the chat input
        // owns the keyboard", not "this window's own field is focused".
        TypingNow = string.Equals(
            GUI.GetNameOfFocusedControl(),
            "chatinput",
            StringComparison.Ordinal) ||
            HornDialog.Typing ||
            DuelDlg.Typing;

        if (enterPressed)
        {
            if (inputFocused)
            {
                // Official order: the complete live-field SendNow runs before focus release.
                // Its exceptions propagate before either focus release or Event.Use.
                SendNow();
                GUI.FocusControl(null);
            }
            else
            {
                GUI.FocusControl("chatinput");
            }

            current.Use();
        }
    }

    private void DrawPanel(Rect panel)
    {
        Color previous = GUI.color;
        if (chatFade)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.35f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        // Official ChatUI.DrawPanel (0x060000f9, arm64 0x234914 592 bytes / x86_64 0x242460 560
        // bytes) reads GUI.color first, draws the fade quad from +0x6a, then returns when +0x28
        // (winup) is null and otherwise draws all three window slices with no further test. This
        // early return is the official gate for the panel art and is kept; the two sibling slices
        // stay unguarded exactly as the frozen slices leave them.
        // Native captures scale after the fade pass, before the winUp Unity-null test.
        float scale = s;
        if (winUp == null) return;
        float cap = 5f * scale;
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, cap), winUp);
        GUI.DrawTexture(
            new Rect(panel.x, panel.y + cap, panel.width, panel.height - (cap + cap)),
            winMid);
        GUI.DrawTexture(
            new Rect(panel.x, panel.yMax - cap, panel.width, cap),
            winDown);
    }

    private void DrawLines(List<ChatUI.Line> lines, Rect panel, bool withChannel)
    {
        if (!BitmapFont.Ready) return;
        // Height/width locals persist; each wrap, draw, hover and menu reads the live scale.
        float lineHeight = 18f * s;
        int capacity = Mathf.FloorToInt((panel.height + s * -10f) / lineHeight);
        float padding = (withChannel ? 48f : 8f) * s;
        float textWidth = (panel.width - padding) + s * -8f;
        var visible = new List<ReferenceChatVisualRow>();
        for (int lineIndex = lines.Count - 1; lineIndex >= 0 && visible.Count < capacity; lineIndex--)
        {
            ChatUI.Line line = lines[lineIndex];
            List<string> wrapped = EmoteText.Wrap(line.text, textWidth, s);
            for (int wrapIndex = wrapped.Count - 1; wrapIndex >= 0 && visible.Count < capacity; wrapIndex--)
                visible.Add(new ReferenceChatVisualRow(line, wrapped[wrapIndex], wrapIndex == 0));
        }
        visible.Reverse();
        for (int index = 0; index < visible.Count; index++)
        {
            ReferenceChatVisualRow row = visible[index];
            float y = (lineHeight + s * -12f) +
                (((panel.y + panel.height) + s * -7f) - lineHeight * (visible.Count - index));
            if (withChannel && row.First && !string.IsNullOrEmpty(row.Line.ch))
                BitmapFont.DrawOutline(panel.x + 4f * s, y, "[" + row.Line.ch + "]",
                    new Color32(255, 220, 120, 255), s);
            ChatUI.DrawShareRow(panel.x + padding, y, row.Text, row.Line.col, row.Line.shares, s, true);
            ChatUI.ShareHover(panel.x + padding, y, row.Text, row.Line.shares, s);
            ChatUI.LineMenuForIndependentRuntime(new Rect(panel.x, y + s * -3f, panel.width, lineHeight),
                row.Line.who, row.Line.text);
        }
    }

    private void DrawSideButtons(float barY, float scale, Event current, bool expanded)
    {
        if (!expanded)
        {
            DrawBtn(
                new Rect(0f, barY - 24f * scale, 24f * scale, 24f * scale),
                sizeBtn,
                current,
                () => panelsVisible = true);

            return;
        }

        for (int index = 0; index < SideBtns.Length; index++)
        {
            int capturedIndex = index;
            float offset = SideBtns[capturedIndex].Offset;
            scale = s;
            Rect rect = new Rect(
                0f,
                barY + offset * scale,
                24f * scale,
                24f * scale);
            DrawBtn(rect, sideBtn[index], current, () =>
            {
                // The native closure retains the index, not the tuple observed for layout.
                string name = SideBtns[capturedIndex].Name;
                if (string.Equals(name, "viewbtn", StringComparison.Ordinal))
                {
                    Win05.Open("historybar");
                }
                else if (string.Equals(name, "clearbtn", StringComparison.Ordinal))
                {
                    ChatUI.ClearLinesForIndependentRuntime();
                }
                else if (string.Equals(name, "lockbtn", StringComparison.Ordinal))
                {
                    chatFade = !chatFade;
                }
                else if (string.Equals(name, "sizebtn", StringComparison.Ordinal))
                {
                    panelsVisible = !panelsVisible;
                }
                else if (string.Equals(name, "filterbtn", StringComparison.Ordinal))
                {
                    filterOpen = !filterOpen;
                    chanOpen = false;
                }
            });
        }
    }

    private void DrawChannelPanel(float barY, float scale, Event current)
    {
        Rect panel = ChannelPanelRect(barY, scale);
        Color previous = GUI.color;
        GUI.color = new Color(0.06f, 0.05f, 0.04f, 0.94f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = new Color(0.4f, 0.7f, 0.4f, 0.7f);
        DrawBorder(panel);
        GUI.color = previous;

        for (int index = 0; index < Channels.Length; index++)
        {
            scale = s;
            Rect row = new Rect(
                panel.x + 3f * scale,
                (panel.y + scale * 4f) + scale * (index * 20),
                panel.width - 6f * scale,
                16f * scale);
            bool hovered = row.Contains(current.mousePosition);
            if (hovered)
            {
                GUI.color = new Color(0.4f, 0.7f, 0.4f, 0.3f);
                GUI.DrawTexture(row, Texture2D.whiteTexture);
                GUI.color = previous;
            }

            if (BitmapFont.Ready)
            {
                scale = s;
                BitmapFont.DrawOutline(
                    row.x + scale * 4f,
                    row.y + (scale + scale),
                    Channels[index],
                    chIdx == index
                        ? new Color32(255, 230, 120, 255)
                        : new Color32(190, 255, 190, 255),
                    s);
            }

            if (GUI.Button(row, GUIContent.none, GUIStyle.none))
            {
                chIdx = index;
                channel = Channels[index];
                whisperTarget = string.Empty;
                chanOpen = false;
            }
        }
    }

    private void EnsureEditStyle()
    {
        if (editSt == null)
        {
            // Publish only after the original clone/font/alignment initializers complete;
            // normal/focused color writes then operate on that published style.
            editSt = new GUIStyle(GUIStyle.none)
            {
                font = NpcChatter.CnFont(),
                alignment = TextAnchor.MiddleLeft
            };
            editSt.normal.textColor = Color.white;
            editSt.focused.textColor = Color.white;
        }

        // Reuse the same style even when scale changes, and write size on every GUI pass.
        editSt.fontSize = Mathf.RoundToInt(12f * s);
    }

    private void DrawBtn(Rect rect, Texture2D[] frames, Event current, Action onClick)
    {
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        bool hovered = rect.Contains(current.mousePosition);
        int frame = 0;
        if (hovered)
        {
            frame = Input.GetMouseButton(0) && frames.Length > 2
                ? 2
                : frames.Length > 1 ? 1 : 0;
        }

        Texture2D texture = frames[frame] ?? frames[0];
        if (texture != null)
        {
            GUI.DrawTexture(rect, texture);
        }

        // 060000f8 invokes the supplied action directly: a clicked null action throws.
        // No explicit Event.Use, null-conditional invocation or caller-side success arm.
        if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) onClick();
    }

    internal static Rect ChannelPanelRect(float barY, float scale)
    {
        float height = 6f * scale + (Channels.Length * 20) * scale;
        return new Rect(
            4f * scale,
            barY - height - scale,
            74f * scale,
            height);
    }

    private static void DrawBorder(Rect rect)
    {
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), Texture2D.whiteTexture);
    }

    private sealed class ReferenceChatVisualRow
    {
        public ReferenceChatVisualRow(
            ChatUI.Line line,
            string text,
            bool first)
        {
            Line = line;
            Text = text ?? string.Empty;
            First = first;
        }

        public ChatUI.Line Line { get; }
        public string Text { get; }
        public bool First { get; }
    }
}
}

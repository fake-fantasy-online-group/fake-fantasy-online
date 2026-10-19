using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    // Approved complete external messagebox. Do not enlarge it or paint an opaque
    // replacement behind its original translucent pixels. No bundled artwork/font.
    private static readonly Rect DeleteWindow = new Rect(227f, 236f, 346f, 128f);
    private static readonly Rect DeleteBody = new Rect(250f, 255f, 300f, 72f);
    private static readonly Rect DeleteConfirmFace = new Rect(278f, 334f, 70f, 22f);
    private static readonly Rect DeleteCancelFace = new Rect(460f, 334f, 70f, 22f);
    private const int DeleteButtonHint = 0x44454c42;
    private Texture2D deleteWindow;
    private readonly Texture2D[][] deleteFaces = { new Texture2D[4], new Texture2D[4] };
    private bool deleteArtworkLoaded, deleteFocusCancel;
    private int deleteConfirmControl, deleteCancelControl, deleteSlot = -1;
    private IReadOnlyList<ServerCharacterRecord> deleteRoster;
    private string deleteCharacterId, deleteCharacterName;
    private GUIStyle deleteTextStyle;

    private void OpenDeleteConfirmation()
    {
        if (selSlot < 0 || selSlot >= serverChars.Count) return;
        ServerCharacterRecord row = serverChars[selSlot];
        deleteRoster = serverChars;
        deleteSlot = selSlot;
        deleteCharacterId = row.CharacterId;
        deleteCharacterName = row.DisplayName;
        deleteFocusCancel = true;
        GUIUtility.keyboardControl = 0;
        pendingDelete = true;
    }

    private void CloseDeleteConfirmation()
    {
        pendingDelete = false;
        if (GUIUtility.hotControl != 0 &&
            (GUIUtility.hotControl == deleteConfirmControl || GUIUtility.hotControl == deleteCancelControl))
            GUIUtility.hotControl = 0;
        if (GUIUtility.keyboardControl != 0 &&
            (GUIUtility.keyboardControl == deleteConfirmControl || GUIUtility.keyboardControl == deleteCancelControl))
            GUIUtility.keyboardControl = 0;
        deleteRoster = null;
        deleteSlot = -1;
        deleteCharacterId = deleteCharacterName = null;
        deleteFocusCancel = false;
    }

    private bool DeleteTargetIsCurrent()
    {
        if (!ReferenceEquals(deleteRoster, serverChars) || selSlot != deleteSlot ||
            deleteSlot < 0 || deleteSlot >= serverChars.Count) return false;
        ServerCharacterRecord row = serverChars[deleteSlot];
        return string.Equals(row.CharacterId, deleteCharacterId, StringComparison.Ordinal) &&
            string.Equals(row.DisplayName, deleteCharacterName, StringComparison.Ordinal);
    }

    // A roster receipt can close the modal between press/release. Clear its capture
    // before any underlying page control is drawn, and consume the interrupted event.
    private void PrepareDeleteConfirmation()
    {
        if (pendingDelete && DeleteTargetIsCurrent()) return;
        if (pendingDelete || deleteRoster != null)
        {
            CloseDeleteConfirmation();
            ConsumeDeleteModalEvent();
        }
    }

    private void EnsureDeleteArtwork()
    {
        if (deleteArtworkLoaded) return;
        ResLoader loader = ResLoader.I;
        if (loader == null || loader.Pkg == null) return;
        deleteArtworkLoaded = true;
        Texture2D window = loader.LoadGsaFrameA("res/uires/common/msgboxbk.gsa", 0);
        if (IsDeleteFrame(window, 346f, 128f)) deleteWindow = window;
        string[] paths = { "res/uires/common/affirmbtn.gsa", "res/uires/common/cancelbtn.gsa" };
        for (int button = 0; button < 2; button++)
        {
            Texture2D normal = loader.LoadGsaFrameA(paths[button], 0);
            if (!IsDeleteFrame(normal, 70f, 22f)) continue;
            deleteFaces[button][0] = normal;
            for (int frame = 1; frame < 4; frame++)
            {
                Texture2D texture = loader.LoadGsaFrameA(paths[button], frame);
                if (IsDeleteFrame(texture, 70f, 22f)) deleteFaces[button][frame] = texture;
            }
        }
    }

    private static bool IsDeleteFrame(Texture2D texture, float width, float height) =>
        texture && GsaTextureScale.Width(texture) == width && GsaTextureScale.Height(texture) == height;

    // Hit areas may extend below the small artwork, never into its text or into each
    // other. The original button faces stay 70x22. Account for the current title scale.
    private static Rect DeleteHitRect(Rect face)
    {
        if (!MobileClientPresentation.Enabled) return face;
        float scale = Mathf.Max(.001f, Mathf.Abs(GUI.matrix.m11));
        float width = Mathf.Min(152f, Mathf.Max(100f, 100f / scale));
        float height = Mathf.Min(90f, Mathf.Max(44f, 44f / scale));
        return new Rect(face.x + (face.width - width) * .5f, face.y - 4f, width, height);
    }

    private bool DrawDeleteButton(int index, Rect face, string fallback)
    {
        Rect hit = DeleteHitRect(face);
        int id = GUIUtility.GetControlID(DeleteButtonHint + index, FocusType.Keyboard, hit);
        if (index == 0) deleteConfirmControl = id;
        else
        {
            deleteCancelControl = id;
            if (deleteFocusCancel) { GUIUtility.keyboardControl = id; deleteFocusCancel = false; }
        }
        Event current = Event.current;
        bool enabled = GUI.enabled;
        bool hover = enabled && hit.Contains(current.mousePosition);
        bool clicked = false;
        switch (current.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (enabled && current.button == 0 && hover)
                {
                    GUIUtility.hotControl = id;
                    GUIUtility.keyboardControl = id;
                    current.Use();
                }
                break;
            case EventType.MouseUp:
                if (current.button == 0 && GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0;
                    clicked = enabled && hover;
                    current.Use();
                }
                break;
            case EventType.KeyDown:
                if (enabled && GUIUtility.keyboardControl == id &&
                    (current.keyCode == KeyCode.Space || current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter))
                {
                    clicked = true;
                    current.Use();
                }
                break;
        }
        int state = !enabled ? 3 : GUIUtility.hotControl == id && hover ? 2 :
            hover || GUIUtility.keyboardControl == id ? 1 : 0;
        Texture2D texture = deleteFaces[index][state] ?? deleteFaces[index][0];
        if (texture) GUI.DrawTexture(face, texture);
        else ArtworkSkin.ButtonFace(face, fallback, enabled: enabled, selected: hover,
            pressed: GUIUtility.hotControl == id && hover);
        return clicked;
    }

    private void DrawDeleteMessage()
    {
        // Keep the identity and irreversible warning in the original 300x72 body;
        // no extra title/identity panel. A 24-character CJK name remains complete.
        string message = "确定删除【" + deleteCharacterName + "】？\n" +
            "删除后不可恢复！角色等级、装备和道具将一并删除。";
        BitmapFont.EnsureForIndependentRuntime();
        bool bitmap = BitmapFont.Ready;
        if (bitmap)
            foreach (char c in message)
                if (c != ' ' && c != '\n' && BitmapFont.CellOfForIndependentRuntime(c) < 0) { bitmap = false; break; }
        if (deleteTextStyle == null)
            deleteTextStyle = new GUIStyle(selectionText)
            { alignment = TextAnchor.MiddleLeft, richText = false, wordWrap = false, clipping = TextClipping.Clip,
                padding = new RectOffset(), margin = new RectOffset() };
        deleteTextStyle.fontSize = 12;
        deleteTextStyle.normal.textColor = Color.white;
        var lines = new List<string>();
        string line = string.Empty;
        TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(message);
        while (elements.MoveNext())
        {
            string part = elements.GetTextElement();
            if (part == "\n") { lines.Add(line); line = string.Empty; continue; }
            string candidate = line + part;
            float width = bitmap ? BitmapFont.Measure(candidate, 1f) : deleteTextStyle.CalcSize(new GUIContent(candidate)).x;
            if (line.Length != 0 && width > DeleteBody.width - 2f) { lines.Add(line); line = part; }
            else line = candidate;
        }
        if (line.Length != 0) lines.Add(line);
        float scale = Mathf.Min(1f, DeleteBody.height / Mathf.Max(12f, lines.Count * 16f - 4f));
        float y = (DeleteBody.height - (lines.Count * 16f - 4f) * scale) * .5f;
        GUI.BeginGroup(DeleteBody);
        try
        {
            foreach (string row in lines)
            {
                if (bitmap) BitmapFont.DrawOutline(1f, y, row, Color.white, scale);
                else
                {
                    deleteTextStyle.fontSize = Mathf.Max(1, Mathf.FloorToInt(12f * scale));
                    GUI.Label(new Rect(1f, y, DeleteBody.width - 2f, 12f * scale), row, deleteTextStyle);
                }
                y += 16f * scale;
            }
        }
        finally { GUI.EndGroup(); }
    }

    private bool DrawDeleteConfirmation()
    {
        if (!pendingDelete) return false;
        if (!DeleteTargetIsCurrent()) { CloseDeleteConfirmation(); ConsumeDeleteModalEvent(); return false; }
        EnsureDeleteArtwork();
        if (deleteWindow) GUI.DrawTexture(DeleteWindow, deleteWindow);
        else ArtworkSkin.Panel(DeleteWindow);
        DrawDeleteMessage();
        bool confirm = DrawDeleteButton(0, DeleteConfirmFace, "确认");
        bool cancel = DrawDeleteButton(1, DeleteCancelFace, "取消");
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) cancel = true;
        ConsumeDeleteModalEvent();
        if (cancel) { CloseDeleteConfirmation(); return false; }
        return confirm;
    }

    private static void ConsumeDeleteModalEvent()
    {
        Event current = Event.current;
        if (current.isMouse || current.type == EventType.ScrollWheel ||
            current.type == EventType.KeyDown || current.type == EventType.KeyUp) current.Use();
    }

    private void ReleaseDeletePresentation()
    {
        deleteWindow = null;
        deleteArtworkLoaded = false;
        deleteTextStyle = null;
        foreach (Texture2D[] frames in deleteFaces) Array.Clear(frames, 0, frames.Length);
    }
}
}

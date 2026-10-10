using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>
/// Shared, unlabelled reference artwork. Resource textures remain owned by ResLoader;
/// this cache is discarded when its owner changes and never destroys borrowed textures.
/// Rects and scale use the caller's GUI coordinate space (including any GUI.matrix).
/// </summary>
internal static class ArtworkSkin
{
    private static ResLoader owner;
    private static object packageOwner;
    private static bool loaded;
    private static readonly Dictionary<string, Texture2D[]> frameCache = new Dictionary<string, Texture2D[]>();
    private static readonly Texture2D[] patch = new Texture2D[9];
    private static Texture2D input, row;
    private static Texture2D[] button, close;
    private const int ButtonHint = 0x41525442;
    private static GUIStyle fallbackLabel;
    private static readonly GUIContent fallbackContent = new GUIContent();

    private static bool Ready()
    {
        ResLoader loader = ResLoader.I;
        object package = loader != null ? loader.Pkg : null;
        if (!ReferenceEquals(owner, loader) || !ReferenceEquals(packageOwner, package))
        {
            owner = loader;
            packageOwner = package;
            loaded = false;
            frameCache.Clear();
            input = row = null;
            button = close = null;
            System.Array.Clear(patch, 0, patch.Length);
        }
        return loader != null && loader.Pkg != null;
    }

    public static Texture2D[] Frames(string path, int count = 4)
    {
        if (!Ready()) return null;
        Texture2D[] frames;
        if (frameCache.TryGetValue(path, out frames) && frames.Length >= count) return frames;
        frames = new Texture2D[Mathf.Max(1, count)];
        for (int i = 0; i < frames.Length; i++) frames[i] = owner.LoadGsaFrameA(path, i);
        frameCache[path] = frames;
        return frames;
    }

    private static void Ensure()
    {
        if (!Ready() || loaded) return;
        loaded = true;
        button = Frames("res/uires/votedlg/buttonbk.gsa");
        close = Frames("res/uires/common/miniclose.gsa", 2);
        input = owner.LoadGsa("res/uires/sysdlg/editbk.gsa");
        row = owner.LoadGsa("res/uires/gameui/menubtn.gsa");
        string[] parts = { "upleft", "upmiddle", "upright", "leftmiddle", "middle", "rightmiddle", "downleft", "downmiddle", "downright" };
        for (int i = 0; i < parts.Length; i++)
            patch[i] = owner.LoadGsa("res/uires/patch/patch" + parts[i] + "2.gsa");
    }

    public static void Panel(Rect rect, float scale = 1f, bool highlighted = false, float opacity = 1f)
    {
        Ensure();
        Color previous = GUI.color;
        try
        {
            GUI.color = previous * (highlighted ? new Color(1.15f, 1.08f, .83f, opacity) : new Color(1f, 1f, 1f, opacity));
            float e = Mathf.Min(7f * scale, Mathf.Min(rect.width, rect.height) * .5f);
            // patchmiddle2 is translucent decoration, not an opaque panel background.
            Fill(new Rect(rect.x + e, rect.y + e, Mathf.Max(0f, rect.width - 2f * e),
                Mathf.Max(0f, rect.height - 2f * e)), new Color(.055f, .05f, .065f, .98f));
            for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
            {
                Rect cell = new Rect(x == 0 ? rect.x : x == 1 ? rect.x + e : rect.xMax - e,
                    y == 0 ? rect.y : y == 1 ? rect.y + e : rect.yMax - e,
                    x == 1 ? Mathf.Max(0f, rect.width - 2f * e) : e,
                    y == 1 ? Mathf.Max(0f, rect.height - 2f * e) : e);
                Texture2D texture = patch[y * 3 + x];
                if (texture != null) GUI.DrawTexture(cell, texture);
                else Fill(cell, x == 1 && y == 1 ? new Color(.07f, .07f, .08f, .96f) : new Color(.48f, .40f, .27f, 1f));
            }
        }
        finally { GUI.color = previous; }
    }

    public static void Input(Rect rect, float scale = 1f, bool focused = false)
    {
        Ensure();
        Color previous = GUI.color;
        try
        {
            if (focused) GUI.color = previous * new Color(1.18f, 1.12f, .90f, 1f);
            if (input != null) Slice(rect, input, 3f, scale);
            else Panel(rect, scale, focused);
        }
        finally { GUI.color = previous; }
    }

    public static void Row(Rect rect, float scale = 1f, bool highlighted = false)
    {
        Ensure();
        if (!highlighted) return;
        if (row != null) Slice(rect, row, 2f, scale);
        else Panel(rect, scale, true);
    }

    /// <summary>Draw only; selected/pressed let existing MouseDown owners retain their timing.</summary>
    public static void ButtonFace(Rect rect, string text, float scale = 1f, bool enabled = true,
        bool selected = false, float fontScale = 1f, bool pressed = false)
    {
        Ensure();
        bool active = enabled && GUI.enabled;
        int state = !active ? 3 : pressed ? 2 : selected ? 1 : 0;
        Texture2D texture = Frame(button, state);
        if (texture != null) Slice(rect, texture, 7f, scale);
        else Panel(rect, scale, selected && active);
        Label(rect, text, scale, fontScale,
            active ? new Color(1f, .94f, .78f, 1f) : new Color(.58f, .56f, .53f, 1f));
    }

    /// <summary>Release-to-activate control with IMGUI mouse capture and keyboard focus.</summary>
    public static bool Button(Rect rect, string text, float scale = 1f, bool enabled = true,
        bool selected = false, float fontScale = 1f)
    {
        int id = GUIUtility.GetControlID(ButtonHint, FocusType.Keyboard, rect);
        bool active = enabled && GUI.enabled;
        bool hover = active && rect.Contains(Event.current.mousePosition);
        bool clicked = Click(rect, id, active);
        ButtonFace(rect, text, scale, active, selected || hover || GUIUtility.keyboardControl == id,
            fontScale, active && GUIUtility.hotControl == id && hover);
        return clicked;
    }

    public static bool Tab(Rect rect, string text, float scale = 1f, bool enabled = true,
        bool selected = false, float fontScale = 1f)
    {
        int id = GUIUtility.GetControlID(ButtonHint, FocusType.Keyboard, rect);
        bool active = enabled && GUI.enabled;
        bool hover = active && rect.Contains(Event.current.mousePosition);
        bool clicked = Click(rect, id, active);
        Panel(rect, scale, selected || hover || active && GUIUtility.keyboardControl == id);
        Label(rect, text, scale, fontScale, active ? new Color(1f, .89f, .65f, 1f) : Color.gray);
        return clicked;
    }

    private static void Label(Rect rect, string text, float scale, float fontScale, Color color)
    {
        if (string.IsNullOrEmpty(text)) return;
        float font = Mathf.Min(scale * fontScale, Mathf.Max(0f, rect.height - 4f * scale) / 12f);
        float available = Mathf.Max(0f, rect.width - 10f * scale);
        // Measure also gives the bitmap atlas a chance to become ready after startup.
        float width = BitmapFont.Measure(text, font);
        if (BitmapFont.Ready)
        {
            if (width > 0f) font *= Mathf.Min(1f, available / width);
            BitmapFont.CenterOutline(rect, text, color, font);
            return;
        }

        // Login remains readable while the optional reference font is unavailable.
        // Reuse both the style and content; never allocate a per-event fallback texture.
        if (fallbackLabel == null)
            fallbackLabel = new GUIStyle(GUI.skin.label)
            {
                font = NpcChatter.CnFont(),
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                richText = false,
                wordWrap = false,
                padding = new RectOffset(0, 0, 0, 0)
            };
        fallbackContent.text = text;
        fallbackLabel.fontSize = Mathf.Max(1, Mathf.RoundToInt(12f * font));
        float measured = fallbackLabel.CalcSize(fallbackContent).x;
        if (measured > available && measured > 0f)
            fallbackLabel.fontSize = Mathf.Max(1, Mathf.FloorToInt(fallbackLabel.fontSize * available / measured));
        fallbackLabel.normal.textColor = color;
        GUI.Label(rect, fallbackContent, fallbackLabel);
    }

    private static bool Click(Rect rect, int id, bool enabled)
    {
        Event current = Event.current;
        switch (current.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (enabled && current.button == 0 && rect.Contains(current.mousePosition))
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
                    current.Use();
                    return enabled && rect.Contains(current.mousePosition);
                }
                break;
            case EventType.KeyDown:
                if (enabled && GUIUtility.keyboardControl == id &&
                    (current.keyCode == KeyCode.Space || current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter))
                {
                    current.Use();
                    return true;
                }
                break;
        }
        return false;
    }

    public static bool Close(Rect rect, float scale = 1f, bool enabled = true)
    {
        int id = GUIUtility.GetControlID(ButtonHint, FocusType.Keyboard, rect);
        bool active = enabled && GUI.enabled;
        bool hover = active && rect.Contains(Event.current.mousePosition);
        bool clicked = Click(rect, id, active);
        CloseFace(rect, hover || active && GUIUtility.keyboardControl == id, GUIUtility.hotControl == id && hover, active);
        return clicked;
    }

    public static void CloseFace(Rect rect, bool hover = false, bool pressed = false, bool enabled = true)
    {
        Ensure();
        if (Frame(close, 0) == null) ButtonFace(rect, "×", 1f, enabled, hover, 1f, pressed);
        else IconFace(rect, close, hover, pressed, enabled);
    }

    public static void IconFace(Rect rect, Texture2D[] frames, bool hover = false, bool pressed = false, bool enabled = true)
    {
        bool active = enabled && GUI.enabled;
        int state = !active && frames != null && frames.Length >= 4 ? 3 : pressed ? 2 : hover ? 1 : 0;
        Texture2D texture = Frame(frames, state);
        if (texture == null) return;
        Color previous = GUI.color;
        try
        {
            if (!active && (frames == null || frames.Length < 4)) GUI.color = previous * new Color(.55f, .55f, .55f, 1f);
            GUI.DrawTexture(rect, texture);
        }
        finally { GUI.color = previous; }
    }

    private static Texture2D Frame(Texture2D[] frames, int state)
    {
        if (frames == null || frames.Length == 0) return null;
        return frames[Mathf.Clamp(state, 0, frames.Length - 1)] ?? frames[0];
    }

    private static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = previous * color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    // Preserve corners rather than stretching the entire 79x39 button or 166x24 input.
    // Source borders are logical pixels; normalized UVs therefore also support HD assets.
    private static void Slice(Rect rect, Texture2D texture, float border, float scale)
    {
        float sourceX = Mathf.Min(.5f, border / TexScale.W(texture));
        float sourceY = Mathf.Min(.5f, border / TexScale.H(texture));
        float edgeX = Mathf.Min(border * scale, rect.width * .5f);
        float edgeY = Mathf.Min(border * scale, rect.height * .5f);
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 3; x++)
        {
            float u = x == 0 ? 0f : x == 1 ? sourceX : 1f - sourceX;
            float top = y == 0 ? 0f : y == 1 ? sourceY : 1f - sourceY;
            float uw = x == 1 ? 1f - 2f * sourceX : sourceX;
            float vh = y == 1 ? 1f - 2f * sourceY : sourceY;
            Rect cell = new Rect(x == 0 ? rect.x : x == 1 ? rect.x + edgeX : rect.xMax - edgeX,
                y == 0 ? rect.y : y == 1 ? rect.y + edgeY : rect.yMax - edgeY,
                x == 1 ? Mathf.Max(0f, rect.width - edgeX * 2f) : edgeX,
                y == 1 ? Mathf.Max(0f, rect.height - edgeY * 2f) : edgeY);
            if (cell.width > 0f && cell.height > 0f)
                GUI.DrawTextureWithTexCoords(cell, texture, new Rect(u, 1f - top - vh, uw, vh));
        }
    }
}
}

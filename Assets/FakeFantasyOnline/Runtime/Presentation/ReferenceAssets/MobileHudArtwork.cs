using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Mobile-only faces; input continues to belong to the existing HUD controls.</summary>
internal static class MobileHudArtwork
{
    internal const string RoundControlPath = "res/uires/petCircle/choosepetbtn.gsa";
    internal const string AttackIconPath = "res/uires/badgen/hurttype0.gsa";
    internal const string SkillWellPath = "res/uires/composedlg/sculphole.gsa";
    private static GUIStyle labelStyle;
    private static readonly GUIContent labelContent = new GUIContent();

    internal static Texture2D ControlFrame(Texture2D[] frames, int index, bool hovered, bool pressed)
    {
        int first = index * 3;
        if (index < 0 || frames == null || first >= frames.Length) return null;
        int state = pressed ? 2 : hovered ? 1 : 0;
        return first + state < frames.Length && frames[first + state] != null
            ? frames[first + state] : frames[first];
    }

    internal static Rect Inset(Rect rect, float padding)
    {
        float x = Mathf.Min(padding, rect.width * .5f);
        float y = Mathf.Min(padding, rect.height * .5f);
        return new Rect(rect.x + x, rect.y + y, Mathf.Max(0f, rect.width - x * 2f),
            Mathf.Max(0f, rect.height - y * 2f));
    }

    internal static Rect Fit(Rect rect, float width, float height)
    {
        if (width <= 0f || height <= 0f) return new Rect(rect.center.x, rect.center.y, 0f, 0f);
        float scale = Mathf.Min(rect.width / width, rect.height / height);
        float w = width * scale, h = height * scale;
        return new Rect(rect.center.x - w * .5f, rect.center.y - h * .5f, w, h);
    }

    internal static void Image(Rect rect, Texture2D texture, bool pressed = false, bool enabled = true,
        float opacity = 1f)
    {
        if (texture == null) return;
        Color previous = GUI.color;
        try
        {
            GUI.color = previous * (!enabled || !GUI.enabled ? new Color(.55f, .55f, .55f, opacity)
                : pressed ? new Color(.82f, .82f, .82f, opacity) : new Color(1f, 1f, 1f, opacity));
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
        }
        finally { GUI.color = previous; }
    }

    private static Texture2D ResourceFrame(string path)
    {
        // ArtworkSkin owns loader/package invalidation. These are existing package assets,
        // borrowed without changing the desktop's resource slots or allocating new artwork.
        Texture2D[] frames = ArtworkSkin.Frames(path, 1);
        return frames != null && frames.Length > 0 ? frames[0] : null;
    }

    internal static void Joystick(Rect touchRect, Vector2 axis, float scale)
    {
        Texture2D face = ResourceFrame(RoundControlPath);
        bool moving = axis.x * axis.x + axis.y * axis.y > .01f;
        Image(Inset(touchRect, 10f * scale), face, opacity: moving ? .38f : .28f);
        Rect knob = new Rect(touchRect.center.x - 26f * scale + axis.x * 40f * scale,
            touchRect.center.y - 26f * scale - axis.y * 40f * scale, 52f * scale, 52f * scale);
        Image(knob, face, opacity: .92f);
    }

    internal static void AttackFace(Rect touchRect, float scale, bool pressed)
    {
        Image(Inset(touchRect, 2f * scale), ResourceFrame(RoundControlPath), pressed, opacity: .92f);
        // The original 40x40 sword has transparent margins; a 64px canvas yields a
        // visible 38px blade at the logical HUD scale without enlarging the touch area.
        Image(Inset(touchRect, 16f * scale), ResourceFrame(AttackIconPath), pressed);
    }

    internal static void SkillFace(Rect touchRect, float scale, bool occupied, bool pressed)
    {
        Image(Inset(touchRect, 2f * scale), ResourceFrame(SkillWellPath), pressed,
            opacity: occupied ? .92f : .48f);
    }

    internal static void ControlFace(Rect rect, Texture2D[] bottomFrames, Texture2D[] legacyFrames,
        int index, float scale, bool hovered, bool pressed)
    {
        // The complete 50x50 bottom-art frames are already transparent icon faces.
        // Legacy ctrlbar frames have a different shape and alone need the original UV crop.
        Texture2D texture = ControlFrame(bottomFrames, index, hovered, pressed);
        Rect well = Inset(rect, 2f * scale);
        if (texture != null) { Image(well, texture); return; }
        texture = ControlFrame(legacyFrames, index, hovered, pressed);
        if (texture == null) return;
        Color previous = GUI.color;
        try
        {
            if (!GUI.enabled) GUI.color = previous * new Color(.55f, .55f, .55f, 1f);
            Rect art = Fit(well, texture.width, texture.height * .61818182f);
            GUI.DrawTextureWithTexCoords(art, texture, new Rect(0f, .27272728f, 1f, .61818182f));
        }
        finally { GUI.color = previous; }
    }

    internal static void Label(Rect rect, string text, float scale)
    {
        if (string.IsNullOrEmpty(text)) return;
        float available = Mathf.Max(0f, rect.width - 10f * scale);
        float fontScale = Mathf.Min(scale, Mathf.Max(0f, rect.height - 6f * scale) / 12f);
        float width = BitmapFont.Measure(text, fontScale);
        Color color = GUI.enabled ? new Color(1f, .94f, .78f, 1f) : new Color(.58f, .56f, .53f, 1f);
        if (BitmapFont.Ready)
        {
            if (width > available && width > 0f) fontScale *= available / width;
            BitmapFont.CenterOutline(rect, text, color, fontScale);
            return;
        }
        if (labelStyle == null)
            labelStyle = new GUIStyle(GUIStyle.none)
            {
                font = NpcChatter.CnFont(), alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip, richText = false, wordWrap = false,
                padding = new RectOffset(0, 0, 0, 0)
            };
        labelContent.text = text;
        labelStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(12f * fontScale));
        float measured = labelStyle.CalcSize(labelContent).x;
        if (measured > available && measured > 0f)
            labelStyle.fontSize = Mathf.Max(1, Mathf.FloorToInt(labelStyle.fontSize * available / measured));
        labelStyle.normal.textColor = color;
        GUI.Label(rect, labelContent, labelStyle);
    }

    internal static bool TextButton(Rect rect, string text, float scale, bool selected = false)
    {
        bool hovered = GUI.enabled && rect.Contains(Event.current.mousePosition);
        ArtworkSkin.ButtonFace(rect, text, scale, selected: selected || hovered,
            pressed: hovered && Input.GetMouseButton(0));
        // Retain the original IMGUI release-to-activate path without a second skin frame.
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }
}
}

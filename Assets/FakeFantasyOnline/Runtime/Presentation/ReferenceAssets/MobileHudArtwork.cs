using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Mobile-only faces; input continues to belong to the existing HUD controls.</summary>
internal static class MobileHudArtwork
{
    internal const string RoundControlPath = "res/uires/petCircle/choosepetbtn.gsa";
    internal const string AttackIconPath = "res/uires/badgen/hurttype0.gsa";
    internal const string SkillWellPath = "res/uires/composedlg/sculphole.gsa";
    internal const string MenuTogglePath = "res/uires/sketchmap/togglebtn.gsa";
    private static readonly Dictionary<Texture2D, Texture2D> roundTextures = new Dictionary<Texture2D, Texture2D>();
    private static GUIStyle labelStyle;
    private static readonly GUIContent labelContent = new GUIContent();

    internal static Texture2D ControlFrame(Texture2D[] frames, int index, bool hovered, bool pressed)
    {
        int first = index * 3;
        if (index < 0 || frames == null || first >= frames.Length) return null;
        // Original ctrlbar GSA/PNG frames: 0 pressed, 1 gold hover, 2 unlit idle.
        int state = pressed ? 0 : hovered ? 1 : 2;
        return first + state < frames.Length && frames[first + state] != null
            ? frames[first + state] : first + 2 < frames.Length ? frames[first + 2] : null;
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

    // Mask only mobile copies. Borrowed package pixels and desktop textures are never changed.
    internal static Texture2D RoundTexture(Texture2D source)
    {
        if (source == null) return null;
        if (roundTextures.TryGetValue(source, out Texture2D cached)) return cached;
        // At most 30 slot icons + 37 cooldown frames are needed concurrently. Bound the
        // cache across page/rebinding changes and release owned copies on HUD disable.
        if (roundTextures.Count >= 128) ClearRoundTextures();
        Color32[] original;
        try { original = ReadPixels(source); }
        catch (System.Exception error)
        {
            // Isolate a failed icon from the rest of OnGUI. A cached null keeps the
            // existing round well and input alive without retrying/logging each frame.
            roundTextures.Add(source, null);
            Debug.LogWarning("Mobile HUD could not read icon '" + source.name +
                "' for its circular mask; showing the empty round well until cache reset. " + error.Message);
            return null;
        }
        int size = Mathf.Min(source.width, source.height);
        int left = (source.width - size) / 2, bottom = (source.height - size) / 2;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x + .5f - size * .5f) * 2f / size;
            float dy = (y + .5f - size * .5f) * 2f / size;
            // One source-pixel feather avoids jagged corners when the HUD is scaled.
            float coverage = Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) *
                size * .5f);
            int i = y * size + x;
            pixels[i] = original[(y + bottom) * source.width + x + left];
            pixels[i].a = (byte)Mathf.RoundToInt(pixels[i].a * coverage);
        }
        Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = source.name + " (mobile round)", filterMode = source.filterMode,
            wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
        };
        result.SetPixels32(pixels);
        result.Apply(false, false);
        roundTextures.Add(source, result);
        return result;
    }

    private static Color32[] ReadPixels(Texture2D source)
    {
        if (source.isReadable) return source.GetPixels32();
        // Imported/GPU-only icons need an explicit readback. Never fall back to the
        // unmasked square, and always restore the caller's active render target.
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = null;
        Texture2D readable = null;
        try
        {
            target = RenderTexture.GetTemporary(source.width, source.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
            readable.Apply(false, false);
            return readable.GetPixels32();
        }
        finally
        {
            RenderTexture.active = previous;
            if (target != null) RenderTexture.ReleaseTemporary(target);
            if (readable != null) Object.Destroy(readable);
        }
    }

    internal static void ClearRoundTextures()
    {
        foreach (Texture2D texture in roundTextures.Values) Object.Destroy(texture);
        roundTextures.Clear();
    }

    internal static bool MenuButton(Rect rect, float scale, bool expanded)
    {
        bool hovered = GUI.enabled && Event.current != null && rect.Contains(Event.current.mousePosition);
        bool pressed = hovered && Input.GetMouseButton(0);
        // The native map toggle supplies both arrow directions: frames 0..3 up,
        // 4..7 down, each normal/hover/pressed/disabled. Use no text or default skin.
        Texture2D[] frames = ArtworkSkin.Frames(MenuTogglePath, 8);
        int frame = (expanded ? 0 : 4) + (!GUI.enabled ? 3 : pressed ? 2 : hovered ? 1 : 0);
        Image(Inset(rect, 6f * scale), frames != null && frame < frames.Length ? frames[frame] : null);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
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
        // This is a top menu: the desktop top branch draws the entire ctrlbar frame.
        // The bottom-hotbar UV crop removes the decorative foot and must not be reused here.
        Texture2D texture = ControlFrame(legacyFrames, index, hovered, pressed)
            ?? ControlFrame(bottomFrames, index, hovered, pressed);
        if (texture == null) return;
        Rect well = Inset(rect, 2f * scale);
        Image(Fit(well, texture.width, texture.height), texture);
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

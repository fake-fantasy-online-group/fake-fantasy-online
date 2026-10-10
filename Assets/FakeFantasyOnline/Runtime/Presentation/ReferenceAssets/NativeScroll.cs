using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Direct source recovery of the complete official 1.5.8 NativeScroll owner.
/// </summary>
public static class NativeScroll
{
    private const string THUMB_PATH = "res/uires/common/slidebtn.gsa";

    private static Texture2D _thumb;
    private static bool _tried;
    private static bool _drag;
    private static float _grab;
    private static object _cur;
    private static Rect _curTrack;

    private static bool SameTrack(Rect t)
    {
        return Mathf.Abs(_curTrack.x - t.x) < 1f &&
               Mathf.Abs(_curTrack.y - t.y) < 1f;
    }

    private static Texture2D Thumb()
    {
        if (!_tried)
        {
            _tried = true;
            _thumb = ResLoader.I?.LoadGsa(THUMB_PATH);
        }

        return _thumb;
    }

    public static void Wheel(
        Rect view,
        float viewH,
        float contentH,
        ref Vector2 scroll,
        float step)
    {
        Event e = Event.current;
        if (e.type != EventType.ScrollWheel ||
            !view.Contains(e.mousePosition))
        {
            return;
        }

        float max = contentH - viewH;
        if (max <= 0f)
        {
            return;
        }

        if (step <= 0f)
        {
            step = UIScale.Factor * 20f;
        }

        scroll.y = Mathf.Clamp(
            scroll.y + (e.delta.y >= 0f ? step : -step),
            0f,
            max);
        e.Use();
    }

    public static void Bar(
        object owner,
        Rect view,
        float contentH,
        ref Vector2 scroll)
    {
        float s = UIScale.Factor;
        Rect track = TrackRect(view, s);
        BarAt(owner, track, view.height, contentH, ref scroll);
    }

    // These are the official Bar/BarAt/BarRows expressions, factored so the
    // focused audit checks the geometry actually consumed by the draw path.
    internal static Rect TrackRect(Rect view, float scale)
    {
        return new Rect(
            view.x + view.width - 15f * scale - scale,
            view.y + scale,
            15f * scale,
            view.height - (scale + scale));
    }

    internal static Rect ThumbRect(
        Rect track,
        float maximum,
        float scroll,
        float thumbHeight)
    {
        float normalized = maximum > 0f ? Mathf.Clamp01(scroll / maximum) : 0f;
        return new Rect(
            track.x,
            track.y + (track.height - thumbHeight) * normalized,
            track.width,
            thumbHeight);
    }

    public static void BarRows(
        object owner,
        Rect track,
        int visibleRows,
        int totalRows,
        ref int scroll)
    {
        int maximum = totalRows - visibleRows;
        if (maximum < 1)
        {
            return;
        }

        float thumbH = UIScale.Factor * 20f;
        float travel = track.height - thumbH;
        Rect thumbRect = ThumbRect(track, maximum, scroll, thumbH);
        Texture2D texture = Thumb();
        if (texture != null)
        {
            GUI.DrawTexture(thumbRect, texture);
        }
        else
        {
            Color old = GUI.color;
            GUI.color = new Color(0.6f, 0.55f, 0.4f, 0.9f);
            GUI.DrawTexture(thumbRect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        Event e = Event.current;
        Vector2 mouse = e.mousePosition;
        if (thumbRect.Contains(mouse) && e.type == EventType.MouseDown)
        {
            _drag = true;
            _cur = owner;
            _curTrack = track;
            _grab = mouse.y - thumbRect.y;
            e.Use();
        }
        else if (track.Contains(mouse) && e.type == EventType.MouseDown && travel > 0f)
        {
            scroll = Mathf.RoundToInt(
                Mathf.Clamp01((mouse.y - track.y - thumbH * 0.5f) / travel) *
                maximum);
            e.Use();
        }

        if (_drag && ReferenceEquals(_cur, owner) && SameTrack(track) &&
            e.type == EventType.MouseDrag && travel > 0f)
        {
            scroll = Mathf.RoundToInt(
                Mathf.Clamp01((mouse.y - _grab - track.y) / travel) * maximum);
            e.Use();
        }

        if (e.type == EventType.MouseUp)
        {
            _drag = false;
            _cur = null;
        }
    }

    public static void BarAt(
        object owner,
        Rect track,
        float viewH,
        float contentH,
        ref Vector2 scroll)
    {
        if (contentH <= viewH + 1f)
        {
            return;
        }

        float thumbH = UIScale.Factor * 20f;
        float maximum = contentH - viewH;
        float travel = track.height - thumbH;
        Rect thumbRect = ThumbRect(track, maximum, scroll.y, thumbH);
        Texture2D texture = Thumb();
        if (texture != null)
        {
            GUI.DrawTexture(thumbRect, texture);
        }
        else
        {
            Color old = GUI.color;
            GUI.color = new Color(0.6f, 0.55f, 0.4f, 0.9f);
            GUI.DrawTexture(thumbRect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        Event e = Event.current;
        Vector2 mouse = e.mousePosition;
        if (thumbRect.Contains(mouse) && e.type == EventType.MouseDown)
        {
            _drag = true;
            _cur = owner;
            _curTrack = track;
            _grab = mouse.y - thumbRect.y;
            e.Use();
        }
        else if (track.Contains(mouse) && e.type == EventType.MouseDown && travel > 0f)
        {
            scroll.y = maximum * Mathf.Clamp01(
                (mouse.y - track.y - thumbH * 0.5f) / travel);
            e.Use();
        }

        if (_drag && ReferenceEquals(_cur, owner) && SameTrack(track) &&
            e.type == EventType.MouseDrag && travel > 0f)
        {
            scroll.y = maximum * Mathf.Clamp01(
                (mouse.y - _grab - track.y) / travel);
            e.Use();
        }

        if (e.type == EventType.MouseUp)
        {
            _drag = false;
            _cur = null;
        }
    }
}

}

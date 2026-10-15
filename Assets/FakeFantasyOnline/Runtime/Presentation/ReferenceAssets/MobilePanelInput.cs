using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Geometry and input adapters only. Native owners keep their artwork and drawing code.
internal static class MobilePanelLayout
{
    internal static Rect Fit(Rect design, Rect safe, ref float scale, ref float x, ref float y)
    {
        scale = Mathf.Min(scale, Mathf.Min(safe.width / design.width, safe.height / design.height));
        x = Mathf.Clamp(x, safe.x - design.x * scale, safe.xMax - design.xMax * scale);
        y = Mathf.Clamp(y, safe.y - design.y * scale, safe.yMax - design.yMax * scale);
        return new Rect(x + design.x * scale, y + design.y * scale, design.width * scale, design.height * scale);
    }

    internal static Rect Actions(Vector2 anchor, int rows, out float scale)
    {
        Rect safe = MobileClientPresentation.SafeAreaGui;
        scale = Mathf.Min(Mathf.Max(1f, MobileClientPresentation.Scale),
            Mathf.Min(safe.width / 160f, safe.height / (rows * 32f)));
        float width = 160f * scale, height = rows * 32f * scale;
        return new Rect(Mathf.Clamp(anchor.x, safe.x, safe.xMax - width),
            Mathf.Clamp(anchor.y, safe.y, safe.yMax - height), width, height);
    }

    internal static int HotbarSlot(Rect box, float scale, ref int page, out bool cancel)
    {
        int selected = -1;
        for (int button = 0; button < 6; button++)
        {
            int slot = page * 6 + button;
            if (slot >= 0 && slot < 24 && Action(box, button, (page + 1) + "页" + (button + 1), scale)) selected = slot;
        }
        if (Action(box, 6, "上一页", scale)) page = (page + 3) % 4;
        if (Action(box, 7, "下一页", scale)) page = (page + 1) % 4;
        cancel = Action(box, 8, "取消", scale);
        return selected;
    }

    internal static bool Action(Rect box, int row, string label, float scale, bool enabled = true)
    {
        Rect rect = new Rect(box.x, box.y + row * 32f * scale, box.width, 32f * scale);
        bool old = GUI.enabled;
        try
        {
            GUI.enabled = old && enabled;
            ArtworkSkin.ButtonFace(rect, label, scale, GUI.enabled, rect.Contains(Event.current.mousePosition), pressed: false);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }
        finally { GUI.enabled = old; }
    }
}

// A tap selects a native tooltip; a hold requests actions. Neither sends a command.
// Raw touches are sampled once per frame and their synthetic mouse release is consumed.
internal sealed class MobilePanelTouch
{
    private readonly MobileItemGesture gesture = new MobileItemGesture();
    private int frame = -1;
    private bool suppressRelease;
    internal bool Canceled { get; private set; }
    internal bool Started { get; private set; }
    internal Vector2 Anchor { get; private set; }
    internal Vector2 StartPoint { get; private set; }
    internal void Reset()
    {
        if (gesture.Pointer != -1) suppressRelease = true;
        gesture.Reset();
    }
    internal bool Read(Event ev, float scale, Func<Vector2, int> hit, out int target, out bool held)
    {
        target = -1; held = false; Canceled = Started = false;
        if (Input.touchCount > 1)
        { Reset(); Canceled = true; }
        else if (Input.touchCount > 0)
        {
            if (frame != Time.frameCount)
            {
                frame = Time.frameCount;
                Touch touch = Input.GetTouch(0);
                bool cancel = touch.phase == TouchPhase.Canceled;
                if (gesture.Pointer >= 0 && gesture.Pointer != touch.fingerId)
                { Reset(); Canceled = true; }
                if (cancel) Canceled = true;
                Sample(touch.fingerId, new Vector2(touch.position.x, Screen.height - touch.position.y),
                    touch.phase == TouchPhase.Began, touch.phase == TouchPhase.Ended || cancel, cancel,
                    scale, hit, out target, out held);
            }
        }
        else
        {
            if (gesture.CancelMissingOwner(false)) { suppressRelease = true; Canceled = true; }
            if (!UnityEngine.Application.isMobilePlatform && ev.button == 0 &&
                (ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag || ev.type == EventType.MouseUp || ev.type == EventType.Repaint))
                Sample(-2, ev.mousePosition, ev.type == EventType.MouseDown, ev.type == EventType.MouseUp,
                    false, scale, hit, out target, out held);
        }
        if (Canceled && (ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag)) ev.Use();
        if (suppressRelease && ev.type == EventType.MouseUp)
        { suppressRelease = false; ev.Use(); }
        else if (gesture.Pointer != -1 && (ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag)) ev.Use();
        return target >= 0;
    }
    private void Sample(int pointer, Vector2 point, bool begin, bool end, bool cancel,
        float scale, Func<Vector2, int> hit, out int target, out bool held)
    {
        target = -1; held = false;
        if (begin)
        {
            suppressRelease = false;
            Started = true;
            StartPoint = point;
            int slot = hit(point);
            if (slot >= 0)
            {
                gesture.Begin(pointer, slot, point.x, point.y, Time.unscaledTime);
                suppressRelease = true;
                Anchor = point;
            }
        }
        if (cancel) { gesture.End(pointer, point.x, point.y, 0f, true); return; }
        int selected = gesture.Slot;
        float threshold = Mathf.Max(12f, 12f * scale);
        held = gesture.Move(pointer, point.x, point.y, Time.unscaledTime, threshold);
        bool tap = end && gesture.End(pointer, point.x, point.y, threshold, false);
        if (held || tap) target = selected;
    }
}
}

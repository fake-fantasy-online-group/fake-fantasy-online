using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Mobile-only content drag scrolling for native IMGUI lists. Raw touches are
// sampled once per frame per list, in the caller's GUI space; the desktop path
// returns the incoming value unchanged and never samples input.
internal static class MobileDragScroll
{
    private sealed class Entry
    {
        public readonly MobileDragScrollTrack Track = new MobileDragScrollTrack();
        public int Frame = -1;
        public float Result, Remainder;
    }

    private static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
    private static Rect pairedView;
    private static int pairedFrame = -1;
    private static EventType pairedEvent;
    internal static int LastDragFrame { get; private set; } = -10;

    internal static float Threshold => 10f * Mathf.Max(1f, MobileClientPresentation.Scale);

    // NativeScroll.Wheel has no owner; its view pairs with the immediately following bar.
    internal static void PairView(Rect view)
    {
        if (!MobileClientPresentation.Enabled || Event.current == null) return;
        pairedView = view; pairedFrame = Time.frameCount; pairedEvent = Event.current.type;
    }

    internal static bool TakePairedView(Rect track, out Rect view)
    {
        view = pairedView;
        bool ok = Event.current != null && pairedFrame == Time.frameCount && pairedEvent == Event.current.type &&
            pairedView.yMax > track.y && pairedView.y < track.yMax && pairedView.x < track.x &&
            Mathf.Abs(pairedView.xMax - track.x) <= 48f * Mathf.Max(1f, MobileClientPresentation.Scale);
        pairedFrame = -1;
        return ok;
    }

    // Infers the content region left of a bar inside its owner window.
    internal static Rect RegionLeftOf(object owner, Rect track)
    {
        float left = track.x - Mathf.Max(track.height, 160f);
        if (WinZ.TryGetRect(owner, out Rect window))
        {
            Vector2 min = GUIUtility.ScreenToGUIPoint(window.position);
            left = Mathf.Max(left, min.x);
            left = Mathf.Min(left, track.x - 24f);
        }
        return new Rect(left, track.y, Mathf.Max(0f, track.xMax - left), track.height);
    }

    // Pixel offset lists.
    internal static float Drag(object owner, Rect region, float current, float max)
    {
        if (!MobileClientPresentation.Enabled || owner == null || Event.current == null) return current;
        Entry entry = Get(owner, region);
        if (entry.Frame == Time.frameCount) return entry.Track.Dragging || entry.Track.Coasting ? entry.Result : current;
        entry.Frame = Time.frameCount;
        entry.Result = Sample(entry, region, current, max);
        return entry.Result;
    }

    // Row-indexed lists. Sub-row motion is retained between frames.
    internal static int DragRows(object owner, Rect region, int current, int maximum, float rowHeight)
    {
        if (!MobileClientPresentation.Enabled || owner == null || Event.current == null || rowHeight <= 0f) return current;
        Entry entry = Get(owner, region);
        if (entry.Frame != Time.frameCount)
        {
            entry.Frame = Time.frameCount;
            if (Mathf.RoundToInt(entry.Remainder / rowHeight) != current) entry.Remainder = current * rowHeight;
            entry.Remainder = Sample(entry, region, entry.Remainder, maximum * rowHeight);
        }
        return Mathf.Clamp(Mathf.RoundToInt(entry.Remainder / rowHeight), 0, Mathf.Max(0, maximum));
    }

    private static Entry Get(object owner, Rect region)
    {
        string key = owner.GetHashCode() + ":" + Mathf.RoundToInt(region.x) + ":" + Mathf.RoundToInt(region.y);
        if (!entries.TryGetValue(key, out Entry entry)) { entry = new Entry(); entries[key] = entry; }
        return entry;
    }

    private static float Sample(Entry entry, Rect region, float current, float max)
    {
        MobileDragScrollTrack track = entry.Track;
        int finger = -1; bool down = false, began = false; Vector2 point = default;
        if (Input.touchCount > 1) { track.Cancel(); return current; }
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            finger = touch.fingerId;
            down = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
            began = touch.phase == TouchPhase.Began;
            point = GUIUtility.ScreenToGUIPoint(new Vector2(touch.position.x, Screen.height - touch.position.y));
            if (touch.phase == TouchPhase.Canceled) { track.Cancel(); return current; }
        }
        else if (!UnityEngine.Application.isMobilePlatform && Input.GetMouseButton(0))
        {
            finger = -2; down = true; began = Input.GetMouseButtonDown(0);
            point = GUIUtility.ScreenToGUIPoint(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
        }
        float next = track.Update(finger, down, began, region.Contains(point), point.x, point.y,
            Time.unscaledTime, current, max, Threshold, out bool cancelPress);
        if (cancelPress) GUIUtility.hotControl = 0;
        if (track.Dragging) LastDragFrame = Time.frameCount;
        return next;
    }
}
}

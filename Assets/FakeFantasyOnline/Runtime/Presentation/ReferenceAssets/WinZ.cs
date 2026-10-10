using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static class WinZ
{
    private sealed class Entry
    {
        public Rect rect;
        public int frame;
        public float at;
    }

    private const float ForgetIdleSec = 10f;
    private const float PruneEverySec = 5f;
    private static readonly List<object> order = new List<object>();
    private static readonly Dictionary<object, Entry> reg = new Dictionary<object, Entry>();
    private static float _lastPruneAt;

    private static void Prune()
    {
        float now = Time.unscaledTime;
        if (now - _lastPruneAt < PruneEverySec) return;
        _lastPruneAt = now;
        for (int i = order.Count - 1; i >= 0; i--)
        {
            object owner = order[i];
            if (owner == null)
            {
                order.RemoveAt(i);
                continue;
            }
            if (!(owner is UnityEngine.Object unity && unity == null) &&
                reg.TryGetValue(owner, out Entry entry) && now - entry.at <= ForgetIdleSec) continue;
            order.RemoveAt(i);
            reg.Remove(owner);
        }
    }

    // Additive lifetime hook for addon-owned windows; removes hit zones immediately.
    public static void Forget(object owner)
    {
        if (owner == null) return;
        order.Remove(owner);
        reg.Remove(owner);
    }

    public static void Raise(object owner)
    {
        int index = order.IndexOf(owner);
        if (index >= 0)
        {
            if (index == order.Count - 1) return;
            order.RemoveAt(index);
        }
        order.Add(owner);
    }

    private static bool CoveredAt(object owner, Vector2 point, int frame)
    {
        int index = order.IndexOf(owner);
        if (index < 0) return false;
        for (int i = index + 1; i < order.Count; i++)
            if (reg.TryGetValue(order[i], out Entry entry) &&
                unchecked(frame - entry.frame) <= 2 && entry.rect.Contains(point)) return true;
        return false;
    }

    public static bool AnyWindowAt(Vector2 point)
    {
        int frame = Time.frameCount;
        foreach (var pair in reg)
            if (unchecked(frame - pair.Value.frame) <= 2 && pair.Value.rect.Contains(point)) return true;
        return false;
    }

    public static (int, bool) Frame(object owner, Rect rect, int baseDepth)
    {
        int frame = Time.frameCount;
        Prune();
        if (!reg.TryGetValue(owner, out Entry entry))
        {
            entry = new Entry();
            reg[owner] = entry;
            order.Add(owner);
        }
        else if (unchecked(frame - entry.frame) > 2) Raise(owner);

        Vector2 point = Event.current != null ? Event.current.mousePosition : new Vector2(-9999f, -9999f);
        bool covered = !InventoryDlg.HasHeld && CoveredAt(owner, point, frame);
        if (Event.current != null && Event.current.type == EventType.MouseDown && rect.Contains(point) && !covered)
            Raise(owner);

        entry.rect = rect;
        entry.frame = frame;
        entry.at = Time.unscaledTime;
        int index = order.IndexOf(owner);
        return (unchecked(baseDepth + order.Count - 1 - index), covered);
    }
}

}

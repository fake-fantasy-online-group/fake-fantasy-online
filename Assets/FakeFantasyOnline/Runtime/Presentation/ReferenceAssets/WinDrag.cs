using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static class WinDrag
{
    private static bool dragging;
    private static Vector2 off;
    private static object cur;
    private static int lastW;
    private static int lastH;
    private static int gen;

    public static int Gen
    {
        get
        {
            if (Screen.width != lastW || Screen.height != lastH)
            {
                lastW = Screen.width;
                lastH = Screen.height;
                gen = unchecked(gen + 1);
            }
            return gen;
        }
    }

    public static bool Stale(float x, ref int windowGeneration)
    {
        int currentGeneration = Gen;
        if (float.IsNaN(x) || windowGeneration != currentGeneration)
        {
            windowGeneration = currentGeneration;
            return true;
        }
        return false;
    }

    public static void Handle(object owner, Rect hit, ref float x, ref float y)
    {
        Event e = Event.current;
        if (!dragging && e.type == EventType.MouseDown && e.button == 0 && hit.Contains(e.mousePosition))
        {
            dragging = true;
            cur = owner;
            off = e.mousePosition - new Vector2(x, y);
            e.Use();
        }
        else if (dragging && object.ReferenceEquals(cur, owner))
        {
            if (e.type == EventType.MouseDrag)
            {
                x = e.mousePosition.x - off.x;
                y = e.mousePosition.y - off.y;
                e.Use();
            }
            else if (e.type == EventType.MouseUp)
            {
                dragging = false;
                cur = null;
            }
        }
    }
}

}

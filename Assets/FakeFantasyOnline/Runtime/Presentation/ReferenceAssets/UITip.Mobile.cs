using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class UITip
{
    private static Rect mobileMainBox, mobileCompareBox;
    private static string mobileTipText, mobileTipComparison;
    private static int mobileMainRow, mobileCompareRow, mobileTipFrame = -10, mobileTipDrag;
    private static float mobileTipLastY, mobileTipDragRemainder;

    internal static bool MobileContainsPointer(Vector2 point) => MobileClientPresentation.Enabled &&
        Time.frameCount - mobileTipFrame <= 1 &&
        (mobileMainBox.Contains(point) || mobileCompareBox.Contains(point));

    // The original layout and DrawBox supply every row, colour, frame and comparison.
    // Only placement/clipping and drag scrolling are adapted for a touch safe area.
    private static void RenderMobileOriginal(string text, string comparison, float scale, Vector2 pointer)
    {
        if (!BitmapFont.Ready) return;
        Ensure();
        Rect safe = MobileClientPresentation.SafeAreaGui;
        if (mobileTipText != text || mobileTipComparison != comparison)
        {
            mobileTipText = text; mobileTipComparison = comparison;
            mobileMainRow = mobileCompareRow = mobileTipDrag = 0; mobileTipDragRemainder = 0;
        }
        var main = Layout(text, scale);
        bool compare = !string.IsNullOrEmpty(comparison);
        var other = compare ? Layout(comparison, scale) : default;
        float combinedWidth = main.Item2 + (compare ? other.Item2 + 2 * scale : 0);
        if (combinedWidth > safe.width)
        {
            scale *= safe.width / combinedWidth;
            main = Layout(text, scale);
            if (compare) other = Layout(comparison, scale);
        }
        int capacity = Mathf.Max(1, (int)((safe.height - 8 * scale) / (16 * scale)));
        int mainCount = Mathf.Min(capacity, main.Item1.Count);
        int otherCount = compare ? Mathf.Min(capacity, other.Item1.Count) : 0;
        float height = 8 * scale + mainCount * 16 * scale;
        float x = pointer.x + 16 * scale, y = pointer.y + 16 * scale;
        if (x + main.Item2 > safe.xMax) x = pointer.x - main.Item2 - 4 * scale;
        if (y + height > safe.yMax) y = pointer.y - height - 4 * scale;
        x = Mathf.Clamp(x, safe.x, safe.xMax - main.Item2);
        y = Mathf.Clamp(y, safe.y, safe.yMax - height);
        mobileMainBox = new Rect(x, y, main.Item2, height);
        mobileCompareBox = new Rect(-9999, -9999, 0, 0);
        if (compare)
        {
            float otherHeight = 8 * scale + otherCount * 16 * scale;
            float cx = x - other.Item2 - 2 * scale;
            if (cx < safe.x) cx = x + main.Item2 + 2 * scale;
            if (cx + other.Item2 > safe.xMax)
            {
                // Move the pair together instead of clamping one card over the other.
                x = Mathf.Clamp(x, safe.x + other.Item2 + 2 * scale, safe.xMax - main.Item2);
                mobileMainBox.x = x;
                cx = x - other.Item2 - 2 * scale;
            }
            mobileCompareBox = new Rect(cx, Mathf.Clamp(y, safe.y, safe.yMax - otherHeight), other.Item2, otherHeight);
        }
        mobileTipFrame = Time.frameCount;
        Event ev = Event.current;
        if (ev.type == EventType.MouseDown)
        {
            mobileTipDrag = mobileCompareBox.Contains(ev.mousePosition) ? 2 : mobileMainBox.Contains(ev.mousePosition) ? 1 : 0;
            mobileTipLastY = ev.mousePosition.y; mobileTipDragRemainder = 0;
            if (mobileTipDrag > 0) ev.Use();
        }
        if (ev.type == EventType.MouseDrag && mobileTipDrag > 0)
        {
            mobileTipDragRemainder += mobileTipLastY - ev.mousePosition.y;
            mobileTipLastY = ev.mousePosition.y;
            int rows = (int)(mobileTipDragRemainder / (16 * scale));
            mobileTipDragRemainder -= rows * 16 * scale;
            if (mobileTipDrag == 1) mobileMainRow += rows;
            else mobileCompareRow += rows;
            ev.Use();
        }
        if (ev.type == EventType.MouseUp && mobileTipDrag > 0) { mobileTipDrag = 0; ev.Use(); }
        if (Input.touchCount > 1 || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Canceled)) mobileTipDrag = 0;
        mobileMainRow = Mathf.Max(0, Mathf.Min(main.Item1.Count - mainCount, mobileMainRow));
        DrawBox(mobileMainBox, main.Item1.GetRange(mobileMainRow, mainCount), scale);
        if (compare)
        {
            mobileCompareRow = Mathf.Max(0, Mathf.Min(other.Item1.Count - otherCount, mobileCompareRow));
            DrawBox(mobileCompareBox, other.Item1.GetRange(mobileCompareRow, otherCount), scale);
        }
    }
}
}

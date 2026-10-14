using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal readonly struct MobilePlayerMenuLayout
    {
        internal readonly Rect Box;
        internal readonly float RowHeight, Padding;
        internal readonly int RowsPerPage, PageCount;
        internal MobilePlayerMenuLayout(Rect safe, Vector2 anchor, int count, float scale)
        {
            scale = Mathf.Max(.1f, scale);
            RowHeight = 44f * scale;
            Padding = 8f * scale;
            float availableHeight = Mathf.Max(RowHeight * 3f + Padding * 2f, safe.height - Padding * 2f);
            RowsPerPage = Mathf.Max(1, Mathf.FloorToInt((availableHeight - RowHeight * 2f - Padding * 2f) / RowHeight));
            int rows = Mathf.Min(RowsPerPage, Mathf.Max(1, count));
            PageCount = Mathf.Max(1, (count + RowsPerPage - 1) / RowsPerPage);
            float width = Mathf.Min(260f * scale, safe.width - Padding * 2f);
            float height = (rows + 2) * RowHeight + Padding * 2f;
            Box = new Rect(Mathf.Clamp(anchor.x, safe.xMin + Padding, safe.xMax - Padding - width),
                Mathf.Clamp(anchor.y, safe.yMin + Padding, safe.yMax - Padding - height), width, height);
        }
        internal Rect Header => new Rect(Box.x + Padding, Box.y + Padding, Box.width - 2f * Padding, RowHeight);
        internal Rect Row(int index) => new Rect(Box.x + Padding, Box.y + Padding + (index + 1) * RowHeight,
            Box.width - 2f * Padding, RowHeight);
        internal Rect Previous => new Rect(Box.x + Padding, Box.yMax - Padding - RowHeight,
            (Box.width - 2f * Padding) * .5f, RowHeight);
        internal Rect Next => new Rect(Previous.xMax, Previous.y, Previous.width, RowHeight);
    }
}

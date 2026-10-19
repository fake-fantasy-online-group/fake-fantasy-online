using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    // Only three streams render through this owner: system, ordinary and mobile.
    // Layout stores text rows, never textures, timestamps, hover state or share
    // projections. Shares keep their live list so changes still render immediately.
    private ReferenceChatVisualRows[] visualRowsCache;

    private List<ReferenceChatVisualRow> VisibleRows(List<Line> lines,
        float textWidth, int capacity, float scale)
    {
        if (visualRowsCache == null) visualRowsCache = new ReferenceChatVisualRows[3];
        ReferenceChatVisualRows cache = null;
        int slot = 0;
        for (int index = 0; index < visualRowsCache.Length; index++)
        {
            if (visualRowsCache[index] == null) { slot = index; break; }
            if (ReferenceEquals(visualRowsCache[index].Source, lines))
            {
                cache = visualRowsCache[index];
                break;
            }
        }
        if (cache == null)
            visualRowsCache[slot] = cache = new ReferenceChatVisualRows { Source = lines };

        LineCacheKey key = CurrentLineCacheKey;
        object font = BitmapFont.LayoutIdentity;
        if (cache.Valid && cache.Key.Equals(key) && cache.Count == lines.Count &&
            cache.Width == textWidth && cache.Capacity == capacity && cache.Scale == scale &&
            ReferenceEquals(cache.Font, font)) return cache.Rows;

        List<ReferenceChatVisualRow> rows = cache.Rows;
        rows.Clear();
        for (int lineIndex = lines.Count - 1; lineIndex >= 0 && rows.Count < capacity; lineIndex--)
        {
            Line line = lines[lineIndex];
            List<string> wrapped = EmoteText.Wrap(line.text, textWidth, scale);
            for (int wrapIndex = wrapped.Count - 1; wrapIndex >= 0 && rows.Count < capacity; wrapIndex--)
                rows.Add(new ReferenceChatVisualRow(line, wrapped[wrapIndex], wrapIndex == 0));
        }
        rows.Reverse();
        cache.Key = key;
        cache.Count = lines.Count;
        cache.Width = textWidth;
        cache.Capacity = capacity;
        cache.Scale = scale;
        cache.Font = font;
        cache.Valid = true;
        return rows;
    }

    private sealed class ReferenceChatVisualRows
    {
        internal List<Line> Source;
        internal readonly List<ReferenceChatVisualRow> Rows = new List<ReferenceChatVisualRow>();
        internal LineCacheKey Key;
        internal int Count, Capacity;
        internal float Width, Scale;
        internal object Font;
        internal bool Valid;
    }

    private readonly struct ReferenceChatVisualRow
    {
        internal ReferenceChatVisualRow(Line line, string text, bool first)
        {
            Line = line;
            Text = text ?? string.Empty;
            First = first;
            Channel = first && !string.IsNullOrEmpty(line.ch) ? "[" + line.ch + "]" : string.Empty;
        }

        internal Line Line { get; }
        internal string Text { get; }
        internal bool First { get; }
        internal string Channel { get; }
    }
}
}

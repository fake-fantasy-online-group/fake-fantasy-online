using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Shared by the live owner and the snapshot adapter. Cache the measured rows between GUI passes.
internal sealed class TaskPromptLayout
{
    private readonly List<string> source = new List<string>();
    internal readonly List<string> Rows = new List<string>();
    internal readonly List<float> Widths = new List<float>();
    private float lastWidth = -1f, lastScale = -1f;

    internal void Update(IReadOnlyList<string> lines, float maxWidth, float scale, Func<string, float> measure)
    {
        bool same = lastWidth == maxWidth && lastScale == scale && source.Count == lines.Count;
        for (int i = 0; same && i < lines.Count; i++) same = source[i] == lines[i];
        if (same) return;
        lastWidth = maxWidth; lastScale = scale;
        source.Clear(); Rows.Clear(); Widths.Clear();
        for (int i = 0; i < lines.Count; i++)
        {
            string text = lines[i] ?? string.Empty;
            source.Add(lines[i]);
            string row = string.Empty;
            float width = 0f;
            for (int pos = 0; pos < text.Length;)
            {
                char c = text[pos];
                if (c == '\r') { pos++; continue; }
                if (c == '\n') { Add(row, width); row = string.Empty; width = 0f; pos++; continue; }
                int count = char.IsHighSurrogate(c) && pos + 1 < text.Length &&
                    char.IsLowSurrogate(text[pos + 1]) ? 2 : 1;
                string glyph = text.Substring(pos, count);
                float advance = measure(glyph);
                if (row.Length > 0 && width + advance > maxWidth)
                { Add(row, width); row = string.Empty; width = 0f; }
                row += glyph; width += advance; pos += count;
            }
            if (row.Length > 0) Add(row, width);
        }
    }

    private void Add(string row, float width) { Rows.Add(row); Widths.Add(width); }
}
}

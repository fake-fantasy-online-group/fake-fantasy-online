using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal static class ChatShareTokens
    {
        private const char L = '【';
        private const char R = '】';

        // Official 0x060000d9: one nonoverlapping occurrence per reference, in reference order.
        public static List<(int start, int end, int index)> Locate(string text, List<(byte, byte, int, string)> refs)
        {
            var result = new List<(int, int, int)>();
            if (string.IsNullOrEmpty(text) || refs == null || refs.Count == 0) return result;
            for (int i = 0; i < refs.Count; i++)
            {
                string token = L + refs[i].Item4 + R;
                int offset = 0;
                while (offset + token.Length <= text.Length)
                {
                    int start = text.IndexOf(token, offset, StringComparison.Ordinal);
                    if (start < 0) break;
                    bool overlaps = false;
                    foreach (var used in result)
                        if (start < used.Item2 && used.Item1 < start + token.Length) { overlaps = true; break; }
                    if (overlaps) { offset = start + 1; continue; }
                    result.Add((start, start + token.Length, i));
                    break;
                }
            }
            return result;
        }

        // Official 0x060000da.
        public static string Guard(string oldText, string newText, List<(byte, byte, int, string)> refs)
        {
            oldText = oldText ?? "";
            newText = newText ?? "";
            if (refs == null || refs.Count == 0 || oldText == newText) return newText;
            var tokens = Locate(oldText, refs);
            if (tokens.Count == 0) return newText;
            int start = 0;
            while (start < oldText.Length && start < newText.Length && oldText[start] == newText[start]) start++;
            int oldEnd = oldText.Length;
            int newEnd = newText.Length;
            while (oldEnd > start && newEnd > start && oldText[oldEnd - 1] == newText[newEnd - 1]) { oldEnd--; newEnd--; }
            string inserted = newText.Substring(start, newEnd - start);
            if (start == oldEnd)
            {
                foreach (var token in tokens)
                    if (token.start < start && start < token.end) return oldText;
                return newText;
            }
            var removed = new List<int>();
            int expandedStart = start;
            int expandedEnd = oldEnd;
            foreach (var token in tokens)
            {
                if (token.start < oldEnd && start < token.end)
                {
                    expandedStart = Math.Min(expandedStart, token.start);
                    expandedEnd = Math.Max(expandedEnd, token.end);
                    removed.Add(token.index);
                }
            }
            if (removed.Count == 0) return newText;
            removed.Sort();
            for (int i = removed.Count - 1; i >= 0; i--) refs.RemoveAt(removed[i]);
            return oldText.Substring(0, expandedStart) + inserted + oldText.Substring(expandedEnd);
        }
    }
}

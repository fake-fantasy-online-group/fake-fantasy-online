using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class NpcActions
{
    /// <summary>
    /// Official NpcActions.BlockRange (0x06000903).
    /// Locates the model block inside one _npc_.txt body: the first of "\"M{model}\"",
    /// "\"A{model}\"" or "\"N{model}\"" wins, and the block ends at the next "id = \""
    /// marker or at the end of the text. Returns (-1, -1) when the text is empty or the
    /// model is absent.
    /// </summary>
    private static (int, int) BlockRange(string t, int model)
    {
        if (!string.IsNullOrEmpty(t))
        {
            int start = t.IndexOf(string.Format("\"M{0}\"", model));
            if (start < 0)
            {
                start = t.IndexOf(string.Format("\"A{0}\"", model));
            }

            if (start < 0)
            {
                start = t.IndexOf(string.Format("\"N{0}\"", model));
            }

            if (start >= 0)
            {
                int end = t.IndexOf("id = \"", start + 1);
                if (end < 0)
                {
                    end = t.Length;
                }

                return (start, end);
            }
        }

        return (-1, -1);
    }

    /// <summary>
    /// Official NpcActions.EnumEntries (0x06000904).
    /// Walks the "{id, \"name\", ms, ...}" rows of one model block. The arm64 MoveNext
    /// (0x280530, 640 bytes) clamps nothing on the upper side: it stops on the first
    /// '{' that has no matching '}', and every malformed row is skipped by advancing the
    /// scan position past the closing brace.
    /// </summary>
    private static IEnumerable<(string nm, int aid, int ms)> EnumEntries(string block)
    {
        int p = 0;
        while (true)
        {
            int open = block.IndexOf('{', p);
            if (open < 0)
            {
                yield break;
            }

            int close = block.IndexOf('}', open);
            if (close < 0)
            {
                yield break;
            }

            string row = block.Substring(open + 1, close - open - 1);
            p = close + 1;

            int firstQuote = row.IndexOf('"');
            if (firstQuote < 0)
            {
                continue;
            }

            int secondQuote = row.IndexOf('"', firstQuote + 1);
            if (secondQuote < 0)
            {
                continue;
            }

            string nm = row.Substring(firstQuote + 1, secondQuote - firstQuote - 1).Trim();
            int idComma = row.IndexOf(',');
            if (idComma < 0)
            {
                continue;
            }

            string idText = row.Substring(0, idComma)
                .Trim()
                .TrimStart('{')
                .Trim();
            int aid;
            if (!int.TryParse(idText, out aid))
            {
                continue;
            }

            if (nm.Length == 0)
            {
                continue;
            }

            int ms = 0;
            int intervalComma = row.IndexOf(',', secondQuote);
            if (intervalComma >= 0)
            {
                string intervalText = row.Substring(intervalComma + 1);
                int tailComma = intervalText.IndexOf(',');
                if (tailComma >= 0)
                {
                    intervalText = intervalText.Substring(0, tailComma);
                }

                int parsed;
                if (int.TryParse(intervalText.Trim(), out parsed))
                {
                    ms = parsed;
                }
            }

            yield return (nm, aid, ms);
        }
    }

    /// <summary>
    /// Official NpcActions.ParseBlockIn (0x06000901): action ids of one model read out
    /// of an explicit text body. Returns null when the model has no block there.
    /// </summary>
    private static Dictionary<string, int> ParseBlockIn(string t, int model)
    {
        (int start, int end) = BlockRange(t, model);
        if (start < 0)
        {
            return null;
        }

        Dictionary<string, int> map =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (nm, aid, ms) in EnumEntries(t.Substring(start, end - start)))
        {
            map[nm] = aid;
        }

        return map;
    }

    /// <summary>
    /// Official NpcActions.ParseIntervalsIn (0x06000902): same walk, but it keeps the
    /// third row field and only keeps strictly positive intervals.
    /// </summary>
    private static Dictionary<string, int> ParseIntervalsIn(string t, int model)
    {
        (int start, int end) = BlockRange(t, model);
        if (start < 0)
        {
            return null;
        }

        Dictionary<string, int> map =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (nm, aid, ms) in EnumEntries(t.Substring(start, end - start)))
        {
            if (ms > 0)
            {
                map[nm] = ms;
            }
        }

        return map;
    }

    /// <summary>
    /// Official NpcActions.FixCrossSetActionIds (0x06000900), tail-called by ParseBlock.
    /// When the model block only exists in the 24-res body, the ids parsed there are the
    /// authoritative ones: an action whose primary id resolves to a *different* action
    /// name in the 24-res set is rewritten, its stale interval is dropped, and intervals
    /// missing from the primary parse are filled in from the 24-res set.
    /// </summary>
    private static void FixCrossSetActionIds(int model, Dictionary<string, int> map, Dictionary<string, int> iv)
    {
        if (map.Count == 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(_text24))
        {
            return;
        }

        if (TextFor(model) == _text)
        {
            return;
        }

        Dictionary<string, int> other = ParseBlockIn(_text24, model);
        if (other == null || other.Count == 0)
        {
            return;
        }

        Dictionary<int, string> nameOfId = new Dictionary<int, string>();
        foreach (KeyValuePair<string, int> entry in other)
        {
            nameOfId[entry.Value] = entry.Key;
        }

        foreach (string key in new List<string>(map.Keys))
        {
            int otherId;
            if (!other.TryGetValue(key, out otherId) || otherId == map[key])
            {
                continue;
            }

            string alias;
            if (!nameOfId.TryGetValue(map[key], out alias))
            {
                continue;
            }

            if (string.Equals(alias, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            map[key] = otherId;
            iv.Remove(key);
        }

        Dictionary<string, int> otherIntervals = ParseIntervalsIn(_text24, model);
        if (otherIntervals == null)
        {
            return;
        }

        foreach (KeyValuePair<string, int> entry in otherIntervals)
        {
            if (!iv.ContainsKey(entry.Key))
            {
                iv[entry.Key] = entry.Value;
            }
        }
    }
}

}

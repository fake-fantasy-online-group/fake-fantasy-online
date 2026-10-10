using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class Win05
{
    // Original ComposeTip 0600102d.
    private static string ComposeTip(List<(string, char, bool)> rows)
    {
        int W(string text)
        {
            int width = 0;
            foreach (char c in text) width += c > 127 ? 2 : 1;
            return width;
        }
        int width = 0;
        foreach (var row in rows)
            if (!row.Item3) width = Mathf.Max(width, W(row.Item1));
        width = Mathf.Min(width, 32);
        var result = new StringBuilder();
        for (int i = 0; i < rows.Count; i++)
        {
            if (i != 0) result.Append('\n');
            var row = rows[i];
            if (row.Item2 != 0) result.Append(row.Item2);
            if (row.Item3)
            {
                int padding = (width - W(row.Item1)) / 2;
                if (padding > 0) result.Append(new string(' ', padding));
            }
            result.Append(row.Item1);
        }
        return result.ToString();
    }

    // Original BakeDesc 0600102e.
    private static string BakeDesc(string description, int level)
    {
        if (string.IsNullOrEmpty(description)) return description;
        MatchCollection increments = Regex.Matches(description, @"[，,]每次升级[^。；，%]*?(\d+)%");
        for (int i = increments.Count - 1; i >= 0; i--)
        {
            Match increment = increments[i];
            int step = int.Parse(increment.Groups[1].Value);
            Match basis = Regex.Match(description.Substring(0, increment.Index), @"(\d+)%(?!.*\d+%)");
            if (!basis.Success) continue;
            int value = int.Parse(basis.Groups[1].Value) + step * (level - 1);
            int afterBasis = basis.Index + basis.Length;
            description = description.Substring(0, basis.Index) + value.ToString() + "%" +
                description.Substring(afterBasis, increment.Index - afterBasis) +
                description.Substring(increment.Index + increment.Length);
        }
        return description;
    }
}

}

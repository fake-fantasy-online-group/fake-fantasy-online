using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class Win05
{
    // Preserve each authored row's alignment until UITip has wrapped the text and
    // measured its actual bitmap glyphs. Padding to 32 character columns here was
    // wider than UITip's 150px content limit and shifted short headings right.
    private static string ComposeTip(List<(string, char, bool)> rows)
    {
        var result = new StringBuilder();
        for (int i = 0; i < rows.Count; i++)
        {
            if (i != 0) result.Append('\n');
            var row = rows[i];
            // UITip already supports center-before-color protocol markers and
            // centers every wrapped continuation against the final box width.
            if (row.Item3) result.Append('\u0007');
            if (row.Item2 != 0) result.Append(row.Item2);
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

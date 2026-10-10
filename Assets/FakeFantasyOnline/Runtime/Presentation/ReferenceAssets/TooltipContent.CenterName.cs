using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class TooltipContent
{
    // Official 0x06000f58. Measures the widest line of the already wrapped body and pads
    // the name so that it sits in the middle of that column budget. A leading quality tag
    // (0x03 followed by the quality digit, see QualityName) is lifted off first and kept
    // in front of the padding so the tag itself is never measured.
    public static string CenterName(string name, string body)
    {
        string tag = "";
        if (name.Length > 1 && name[0] == '\u0003')
        {
            tag = name.Substring(0, 2);
            name = name.Substring(2);
        }

        int width = 0;
        foreach (string line in body.Split('\n'))
            width = Mathf.Max(width, W(line));
        int pad = Mathf.Min(width, 32) - W(name);
        if (pad > 1) name = new string(' ', pad / 2) + name;
        return tag + name;

        // Official 0x06000f59, the local width function: a full-width character is two
        // columns wide, everything up to 0x7f is one column.
        static int W(string s)
        {
            int width = 0;
            for (int i = 0; i < s.Length; i++)
                width += s[i] > '\u007f' ? 2 : 1;
            return width;
        }
    }
}
}

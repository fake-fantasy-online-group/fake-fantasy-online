namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class TooltipContent
{
    public static string Fmt(string name, string desc)
    {
        if (string.IsNullOrEmpty(name)) return desc;
        string result = "\u0007" + name;
        if (!string.IsNullOrEmpty(desc)) result += "\n" + desc;
        return result;
    }
}
}

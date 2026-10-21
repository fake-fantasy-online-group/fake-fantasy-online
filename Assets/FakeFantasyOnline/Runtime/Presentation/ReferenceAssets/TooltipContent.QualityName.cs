namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class TooltipContent
{
    public static string QualityName(string name, int q)
    {
        // Older two-affix names already carry light-blue text markup. Do not nest it
        // beneath the new explicit quality marker; Layout consumes one prefix only.
        if (q == 6 && !string.IsNullOrEmpty(name) && name[0] == '\u0005')
            name = name.Substring(1);
        return q <= 0 ? name : "\u0003" + unchecked((char)(q + '0')).ToString() + name;
    }
}
}

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class TooltipContent
{
    public static string QualityName(string name, int q) => q <= 0 ? name :
        "\u0003" + unchecked((char)(q + '0')).ToString() + name;
}
}

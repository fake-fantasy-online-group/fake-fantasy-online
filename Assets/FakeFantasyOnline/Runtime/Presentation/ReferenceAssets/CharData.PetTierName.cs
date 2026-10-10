namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class CharData
{
    public static string PetTierName(int t)
    {
        switch (t)
        {
            case 1: return "凡";
            case 2: return "黄";
            case 3: return "玄";
            case 4: return "地";
            case 5: return "天";
            case 6: return "神";
            default: return "";
        }
    }
}
}

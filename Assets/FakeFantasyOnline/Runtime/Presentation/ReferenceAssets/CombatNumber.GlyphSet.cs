namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatNumber
{
    private static int SetIndexForType(int type)
    {
        switch (type)
        {
            case 0: case 1: return 0;
            case 2: case 3: return 1;
            default: return 2;
        }
    }

    internal static int GlyphSetForIndependentRuntime(int type) => SetIndexForType(type);
}

}

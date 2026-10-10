namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    // Official 1.5.8 token 06000a1d (arm64 0x2b0ed8 / x86_64 0x2c1ca0). Scans i = 0..7 for
    // dirByOct[i] == curDir (+0x258) and flipByOct[i] == flip (+0x260) and returns i, falling back
    // to 2 when no pair matches (arm64 mov w20,#2 at 0x2b0fb8). Both arrays are read from the
    // PlayerController static slot (+0xb8 then +0x80/+0x88) with the official null and bounds
    // guards, which the indexers reproduce. The only official caller is ChatUI.SendNow
    // (060000f7), whose /pos arm feeds the result to OctToName; that is why the reported
    // direction is the octant the animation state currently shows and not the stored faceOct
    // (+0x164), which the ordinary walk branch of UpdateAnim (060009e7) leaves stale.
    public int FacingOct()
    {
        for (int i = 0; i < 8; i++)
        {
            if (dirByOct[i] == curDir && flipByOct[i] == flip)
            {
                return i;
            }
        }

        return 2;
    }

    // Official 1.5.8 token 06000a1e (arm64 0x2b0fd8 / x86_64 0x2c1d80). Out-of-range octants
    // (cmp #7 / b.hi, i.e. unsigned > 7 = oct < 0 || oct > 7) return the metadata string literal
    // "?" (usage ordinal 11265); in range the static OctName slot (+0x90) is indexed with the
    // official null and bounds guards.
    public static string OctToName(int oct)
    {
        if (oct < 0 || oct > 7)
        {
            return "?";
        }

        return OctName[oct];
    }
}

}

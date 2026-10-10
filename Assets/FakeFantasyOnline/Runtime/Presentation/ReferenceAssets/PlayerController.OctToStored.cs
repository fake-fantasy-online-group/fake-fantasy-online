namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    // Official 1.5.8 token 06000a1f.
    public static void OctToStored(int oct, out int storedDir, out bool flipX)
    {
        int index = oct & 7;
        storedDir = dirByOct[index];
        flipX = flipByOct[index];
    }
}

}

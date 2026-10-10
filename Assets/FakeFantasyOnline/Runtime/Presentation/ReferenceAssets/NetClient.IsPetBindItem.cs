namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official 1.5.8 0x06000724.
    public static bool IsPetBindItem(int id)
    {
        return unchecked((uint)(id - 0xbeb)) < 12 ||
            unchecked((uint)(id - 0x6237)) < 4 ||
            unchecked((uint)(id - 0x8b60)) < 4 ||
            id == 0x2a06 || id == 0x2f5a || id == 0xc04 || id == 0x62b6 ||
            unchecked((uint)(id - 0x2a07)) < 11;
    }
}
}

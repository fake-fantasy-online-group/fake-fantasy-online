namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official resource-family magic owner.
/// </summary>
public static class HdMagic
{
    public const uint COPYRIGHT = 440815953U;

    public static int TierOf(byte tier)
    {
        switch (tier)
        {
            case 50:
                return 2;
            case 71:
            case 73:
                return 1;
            case 72:
                return 4;
            default:
                return 0;
        }
    }

    private static byte TierByte(uint id)
    {
        return (byte)(id >> 24);
    }

    private static bool FamilyIs(uint id, char c0, char c1, char c2)
    {
        return (id & 255U) == c0 &&
               ((id >> 8) & 255U) == c1 &&
               ((id >> 16) & 255U) == c2;
    }

    public static int DimgScale(uint id)
    {
        return FamilyIs(id, 'D', 'I', 'M') ? TierOf(TierByte(id)) : 0;
    }

    public static int IaniScale(uint id)
    {
        return FamilyIs(id, 'I', 'A', 'N') ? TierOf(TierByte(id)) : 0;
    }

    public static bool IsDimg(uint id)
    {
        return FamilyIs(id, 'D', 'I', 'M') && TierOf(TierByte(id)) > 0;
    }

    public static bool IsIani(uint id)
    {
        return FamilyIs(id, 'I', 'A', 'N') && TierOf(TierByte(id)) > 0;
    }

    public static int IaniScaleOf(byte[] d)
    {
        if (d == null || d.Length < 8 ||
            d[0] != 81 || d[1] != 81 || d[2] != 70 || d[3] != 26 ||
            d[4] != 73 || d[5] != 65 || d[6] != 78)
        {
            return 0;
        }

        return TierOf(d[7]);
    }

    public static int DimgScaleOf(byte[] d)
    {
        if (d == null || d.Length < 8 ||
            d[0] != 81 || d[1] != 81 || d[2] != 70 || d[3] != 26 ||
            d[4] != 68 || d[5] != 73 || d[6] != 77)
        {
            return 0;
        }

        return TierOf(d[7]);
    }

    public static bool IsIaniBytes(byte[] d)
    {
        return d != null && d.Length >= 8 &&
               d[0] == 81 && d[1] == 81 && d[2] == 70 && d[3] == 26 &&
               d[4] == 73 && d[5] == 65 && d[6] == 78 &&
               TierOf(d[7]) > 0;
    }
}

}

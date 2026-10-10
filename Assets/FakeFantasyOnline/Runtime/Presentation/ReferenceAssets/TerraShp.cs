using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Original 1.5.8 TerraShp, tokens 06000c5d–06000c63.
public static class TerraShp
{
    private const int HEAD = 48;
    private const int STRIDE = 1044;
    private const int NTERRAIN = 544;
    private const int OFF_COUNT = 12;
    private const int OFF_VARS = 20;
    private static byte[] dat;
    private static int curSet = -1;

    public static bool Ready => dat != null;

    public static void Load(PkgArchive pkg, int setNum = 0)
    {
        if (pkg == null) return;
        if (dat != null && curSet == setNum) return;
        string path = setNum == 0 ? "tile/terrashp.dat" : string.Format("tile/terrashp{0:D2}.dat", setNum);
        byte[] bytes = pkg.Read(path);
        if (bytes == null)
        {
            if (ResLoader.I == null || ResLoader.I.Pkg24 == null) return;
            bytes = ResLoader.I.Pkg24.Read(path);
            if (bytes == null) return;
        }
        if (bytes.Length >= HEAD + NTERRAIN * STRIDE && bytes[0] == 'R' && bytes[4] == 'T')
        {
            dat = bytes;
            curSet = setNum;
        }
    }

    private static int I32(int offset) => BitConverter.ToInt32(dat, offset);
    private static int Count(int t) => I32(HEAD + t * STRIDE + OFF_COUNT);
    private static int ImgId(int t, int v, int f) => I32(HEAD + t * STRIDE + OFF_VARS + v * 16 + f * 4);

    public static int GetTile(int code, int frame = 0)
    {
        if (code < 0 || dat == null) return -1;
        int terrain;
        if ((code & 0x0ff00000) == 0)
            terrain = (code >> 8) & 0xff;
        else
        {
            if (((code >> 16) & 0xff0) > 0x20f) return -1;
            terrain = ((code >> 16) & 0xfff) + 16;
        }
        int count = Count(terrain);
        if (count <= 0) return -1;
        int variant = code & 0xff;
        if (variant >= count) variant = 0;
        if ((uint)frame > 3) frame = 0;
        return ImgId(terrain, variant, frame);
    }
}

}

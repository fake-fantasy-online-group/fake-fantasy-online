namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed class ReferenceGsmCoverImage
{
    public int Width, Height, Pitch, Attribute;
    public byte[] Data;
}

public sealed class ReferenceGsmCover
{
    public int PixelWidth, PixelHeight, BlockWidth, BlockHeight, Columns, Rows;
    public ReferenceGsmCoverImage[] Images;
}

public partial class GsmMap
{
    // Cover region of 06000bce. Scalars and array attach before image completion.
    private bool ParseCover(byte[] data, ref int cursor)
    {
        if (data.Length < unchecked(cursor + 24)) return false;
        coverPw = I32(data, cursor);
        coverPh = I32(data, cursor + 4);
        coverBw = I32(data, cursor + 8);
        coverBh = I32(data, cursor + 12);
        coverCx = I32(data, cursor + 16);
        coverCy = I32(data, cursor + 20);
        if ((uint)(coverCx - 1) >= 256 || (uint)(coverCy - 1) >= 256) return false;
        cursor = unchecked(cursor + 24);
        coverImgs = new NakedImg[coverCx * coverCy];
        for (int row = 0; row < coverCy; row++)
            for (int col = 0; col < coverCx; col++)
            {
                if (data.Length < unchecked(cursor + 16)) return false;
                NakedImg img = new NakedImg
                {
                    w = I32(data, cursor), h = I32(data, cursor + 4),
                    pitch = I32(data, cursor + 8), attrib = I32(data, cursor + 12)
                };
                cursor = unchecked(cursor + 16);
                if (img.w > 0 && img.h > 0)
                {
                    if (img.w > 2048 || img.h > 2048) return false;
                    int count = unchecked(img.pitch * img.h);
                    if (data.Length < unchecked(cursor + count)) return false;
                    img.data = new byte[count];
                    System.Array.Copy(data, cursor, img.data, 0, count);
                    cursor = unchecked(cursor + count);
                }
                coverImgs[row * coverCx + col] = img;
            }
        return true;
    }
}

}

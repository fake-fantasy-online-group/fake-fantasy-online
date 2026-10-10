namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GsmMap
{
    // 06000bd1. The output is cleared before the size guard.
    private static bool ReadObj(byte[] data, ref int cursor, uint version, out Obj value)
    {
        value = default;
        int size = (version > 0x10001 ? 36 : 0) + (version >= 0x10006 ? 20 : 0) +
            (version == 0x20006 ? 8 : 0);
        if (unchecked(cursor + size) > data.Length) return false;
        if (version <= 0x10001) return true;
        int id = I32(data, cursor + 8);
        int pivotY = I32(data, cursor + 16);
        int pivotZ = I32(data, cursor + 20);
        cursor += 36;
        int x = 0, y = 0, width = 0, height = 0;
        if (version >= 0x10006)
        {
            x = I32(data, cursor);
            y = I32(data, cursor + 4);
            width = I32(data, cursor + 8);
            height = I32(data, cursor + 12);
            cursor += 20;
            if (version == 0x20006) cursor += 8;
        }
        value = new Obj { id = id, sx = x, sy = y, xl = width, yl = height,
            py = pivotY, pz = pivotZ };
        return true;
    }
}

}

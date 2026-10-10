using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed class ReferenceGsmFieldColor
{
    public int SourceX { get; }
    public int SourceY { get; }
    public int Width { get; }
    public int Height { get; }
    public int TileWidth { get; }
    public int TileHeight { get; }
    public IReadOnlyList<uint> Colors { get; }

    internal ReferenceGsmFieldColor(int sourceX, int sourceY, int width, int height,
        int tileWidth, int tileHeight, uint[] colors)
    {
        SourceX = sourceX;
        SourceY = sourceY;
        Width = width;
        Height = height;
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        Colors = System.Array.AsReadOnly(colors);
    }
}

public partial class GsmMap
{
    // 06000bcf. A normal early return leaves objsOk eligible to become true.
    private void ParseFieldColor(byte[] data, int cursor)
    {
        if (data == null) throw new System.NullReferenceException();
        if (data.Length < unchecked(cursor + 0x34)) return;
        int width = I32(data, cursor + 0x24);
        int height = I32(data, cursor + 0x28);
        if ((uint)(width - 1) >= 8192 || (uint)(height - 1) >= 8192) return;
        int colorOffset = I32(data, cursor + 0x34);
        if (colorOffset < 0) return;
        int payload = unchecked(colorOffset + 8);
        int count = unchecked((width + 1) * (height + 1));
        if (payload > data.Length || unchecked(payload + count * 4) > data.Length) return;
        int sourceX = I32(data, cursor + 0x0c);
        int sourceY = I32(data, cursor + 0x10);
        int tileWidth = I32(data, cursor + 0x2c);
        int tileHeight = I32(data, cursor + 0x30);
        fcColors = new uint[count];
        for (int i = 0; i < count; i++)
            fcColors[i] = U32(data, unchecked(payload + i * 4));
        fcSx = sourceX;
        fcSy = sourceY;
        fcW = width;
        fcH = height;
        fcTileW = tileWidth;
        fcTileH = tileHeight;
        hasFieldColor = true;
    }
}

}

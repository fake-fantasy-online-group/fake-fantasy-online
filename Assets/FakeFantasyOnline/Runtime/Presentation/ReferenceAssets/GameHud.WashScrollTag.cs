using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    public static void DrawWashScrollTag(Rect slot, int itemId, float scale)
    {
        string tag = (itemId & ~3) == 0x182d4 ? "重" : (itemId & ~3) == 0x182d0 ? "洗" : null;
        if (tag == null || !BitmapFont.Ready) return;
        int offset = unchecked((uint)(itemId - 0x182d0)) <= 3u ? itemId - 0x182d0 : itemId - 0x182d4;
        BitmapFont.DrawOutline(slot.x + scale * 3f, slot.y + scale * 3f, tag, UITip.QualityColor(offset + 1), scale);
    }
}
}

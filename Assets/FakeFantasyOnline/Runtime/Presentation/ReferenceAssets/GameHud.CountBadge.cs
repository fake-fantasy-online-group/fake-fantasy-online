using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    private static Texture2D _sl1;
    private static Texture2D _sl2;
    private static bool _slTried;

    internal static void DrawCountBadge(Rect icon, string text, Color color, float scale)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (!_slTried && ResLoader.I != null)
        {
            _slTried = true;
            _sl1 = ResLoader.I.LoadGsa("res/effect/shuliang1.gsa");
            _sl2 = ResLoader.I.LoadGsa("res/effect/shuliang2.gsa");
        }
        Texture2D background = text.Length > 2 ? _sl2 : _sl1;
        float width = background != null ? GsaDecoder.W(background) : text.Length > 2 ? 19f : 14f;
        float height = background != null ? GsaDecoder.H(background) : 14f;
        width *= scale;
        height *= scale;
        Rect bounds = new Rect((icon.x + icon.width) - width, (icon.y + icon.height) - height, width, height);
        if (background != null) GUI.DrawTexture(bounds, background);
        if (BitmapFont.Ready) BitmapFont.CenterOutline(bounds, text, color, scale);
    }
}

}

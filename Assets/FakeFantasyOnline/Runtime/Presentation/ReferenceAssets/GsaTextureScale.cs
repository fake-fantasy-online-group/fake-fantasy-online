using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Compatibility names for existing consumers; all scale ownership is the recovered TexScale.
internal static class GsaTextureScale
{
    public static void Register(Texture2D texture, int scale) => TexScale.Register(texture, scale);
    public static int Of(Texture2D texture) => TexScale.Of(texture);
    public static float Width(Texture2D texture) => TexScale.W(texture);
    public static float Height(Texture2D texture) => TexScale.H(texture);
    public static Sprite MakeSprite(Texture2D texture, float basePixelsPerUnit,
        float pivotX = 0.5f, float pivotY = 0.5f) =>
        TexScale.MakeSprite(texture, basePixelsPerUnit, pivotX, pivotY);
}

}

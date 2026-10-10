using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class PopUI
{
    public static void Panel(Rect r)
    {
        GUI.color = new Color(0.08f, 0.07f, 0.11f, 0.97f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = new Color(0.4f, 0.34f, 0.2f, 1f);
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.y + r.height - 2f, r.width, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
}

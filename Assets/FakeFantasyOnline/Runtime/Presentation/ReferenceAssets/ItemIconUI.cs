using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static class ItemIconUI
{
    public static void Draw(Rect area, Texture2D texture, float scale)
    {
        if (texture == null) return;
        float width = TexScale.W(texture) * scale;
        float height = TexScale.H(texture) * scale;
        if (width <= area.width && height <= area.height)
        {
            GUI.DrawTexture(new Rect(area.x + (area.width - width) * 0.5f,
                area.y + (area.height - height) * 0.5f, width, height), texture);
        }
        else GUI.DrawTexture(area, texture, ScaleMode.ScaleToFit);
    }
}

}

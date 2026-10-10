using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class EquipDlg
{
    // Official 0x06000309. Textures are owned and cached by ResLoader.
    public Texture2D[] Frames(ResLoader loader, string name, int count)
    {
        Texture2D[] frames = new Texture2D[count];
        for (int index = 0; index < frames.Length; index++)
        {
            Texture2D texture = loader.LoadUI(
                "ui/equipdlg05/" + name + "_f" + index.ToString() + ".png");
            frames[index] = texture;
            if (texture != null) texture.filterMode = FilterMode.Point;
        }
        return frames;
    }
}
}

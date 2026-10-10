using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    // Official 0x06000524. The resource loader owns returned textures.
    public Texture2D[] GsaFrames(ResLoader loader, string path, int count)
    {
        Texture2D[] frames = new Texture2D[count];
        if (count < 1) return frames;
        for (int index = 0; index < count; index++)
            frames[index] = loader.LoadGsaFrame(path, index);
        return frames;
    }

    // Official 0x06000525. Metadata-backed path literals are ui/waredlg05/, _f and .png.
    public Texture2D[] Frames(ResLoader loader, string name, int count)
    {
        Texture2D[] frames = new Texture2D[count];
        for (int index = 0; index < count; index++)
        {
            frames[index] = loader.LoadUI(
                "ui/waredlg05/" + name + "_f" + index.ToString() + ".png");
        }
        return frames;
    }

}
}

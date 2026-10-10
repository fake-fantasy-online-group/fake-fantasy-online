using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    // Official instance layout for the resource surface. Offsets are the frozen 1.5.8
    // field-table offsets; C# declaration order is not observable.
    private bool loaded;            // +0x21
    private Texture2D bk;           // +0x40
    private Texture2D icoGold;      // +0x48
    private Texture2D icoSilver;    // +0x50
    private Texture2D icoCopper;    // +0x58
    private Texture2D slotTex;      // +0x60
    private Texture2D[][] tabBtn;   // +0x68
    private Texture2D[] closeBtn;   // +0x70
    private Texture2D[] helpBtn;    // +0x78

    // Official 1.6.0 0x0600052d. Mark this instance loaded before capturing ResLoader.I.
    // A Unity-null loader returns without loading; the caller owns the loaded guard.
    private void Load()
    {
        loaded = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) return;
        bk = loader.LoadUI("ui/waredlg05/waredlgbk50.png");
        if (bk != null) bk.filterMode = FilterMode.Point;
        slotTex = loader.LoadUI("ui/waredlg05/slot.png");
        icoGold = loader.LoadUI("ui/waredlg05/gold.png");
        icoSilver = loader.LoadUI("ui/waredlg05/silver.png");
        icoCopper = loader.LoadUI("ui/waredlg05/copper.png");
        tabBtn = new Texture2D[4][];
        for (int tab = 0; tab < 4; tab++)
        {
            tabBtn[tab] = new Texture2D[2];
            for (int frame = 0; frame < 2; frame++)
            {
                tabBtn[tab][frame] = loader.LoadUI(
                    "ui/waredlg05/" + TabAssets[tab] + "_f" + frame.ToString() + ".png");
            }
        }
        closeBtn = Frames(loader, "miniclose", 4);
        helpBtn = Frames(loader, "helpicon", 4);
        splitBk = loader.LoadGsa("res/uires/gameui/numinput.gsa");
        splitSlide = loader.LoadGsa("res/uires/gameui/smallslide.gsa");
        splitOk = GsaFrames(loader, "res/uires/gameui/inputbtn.gsa", 4);
        spinL = GsaFrames(loader, "res/uires/gameui/spinleft.gsa", 4);
        spinR = GsaFrames(loader, "res/uires/gameui/spinright.gsa", 4);
        splitMinBtn = GsaFrames(loader, "res/uires/common/minbtn.gsa", 4);
        splitMaxBtn = GsaFrames(loader, "res/uires/common/maxbtn.gsa", 4);
    }
}
}

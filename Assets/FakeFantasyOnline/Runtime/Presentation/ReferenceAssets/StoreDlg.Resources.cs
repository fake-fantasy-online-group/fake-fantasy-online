using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class StoreDlg
{
    private const int Cols = 10, Rows = 10, Slots = Cols * Rows;
    private const float NW = 423, NH = 501, GridTop = 66, Pitch = 40, Cell = 36, CoinY = 479, BtnY = 474;
    private static readonly Color GOLDc = new Color(1, .92f, .66f, 1);
    private Texture2D bg, icoClose, icoHelp, icoSave, icoCash, slotTex, icoGold, icoSilver, icoCopper;
    private bool bgBig;
    private int[] slotIdx;

    // Official 1.5.8 06000fe2: use the enlarged warehouse asset, never stretch the old small panel.
    private void Load()
    {
        tried = true;
        ResLoader r = ResLoader.I;
        if (r == null) { tried = false; return; }
        bg = r.LoadUI("ui/storedlg05/storedlgbk100.png");
        bgBig = bg != null;
        if (!bgBig) Debug.LogWarning("[StoreDlg] 缺 ui/storedlg05/storedlgbk100.png(加大版仓库底图) → 只画暗底板(绝不拉伸原版小图: 那会让底图上的槽/牌全错位)。这通常意味着这次更新没把 StreamingAssets/gameassets 发出去, 见 Tools/publish_update.py 的 REQUIRED_UI。");
        icoGold = r.LoadUI("ui/waredlg05/gold.png");
        icoSilver = r.LoadUI("ui/waredlg05/silver.png");
        icoCopper = r.LoadUI("ui/waredlg05/copper.png");
        slotTex = r.LoadUI("ui/waredlg05/slot.png");
        icoClose = r.LoadGsa("res/uires/common/miniclose.gsa");
        icoHelp = r.LoadGsa("res/uires/gameui/helpicon.gsa");
        icoSave = r.LoadGsa("res/uires/common/savebtn.gsa");
        icoCash = r.LoadGsa("res/uires/common/cashbtn.gsa");
        for (int i = 0; i < 4; i++) tabCover[i] = r.LoadGsa("res/uires/storedlg/storetab" + (i + 1) + ".gsa");
    }
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private void Lbl(float x, float y, string t, Color c)
    { if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + x * s, winY + y * s, t, c, s); }

    // Official 1.5.8 06000fe8, captured x advances in logical pixels, independently of UIScale.
    private void Coin(Texture2D ico, long value, ref float x, Color color)
    {
        if (ico != null) GUI.DrawTexture(L(x, CoinY, 12, 12), ico);
        Lbl(x + 16, CoinY, value.ToString(), color);
        x += (BitmapFont.Ready ? BitmapFont.Measure(value.ToString(), 1) : value.ToString().Length * 6f) + 16 + 6;
    }
}
}

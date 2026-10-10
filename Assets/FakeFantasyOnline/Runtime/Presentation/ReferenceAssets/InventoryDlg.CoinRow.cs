using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    // Official InventoryDlg+<>c__DisplayClass132_0, the drawing cursor shared by the OnGUI
    // local functions: X, Y (the window origin in screen space) and coinX (the coin row cursor).
    private struct DrawCursor
    {
        internal float X;
        internal float Y;
        internal float coinX;
    }

    // Official 0x06000534, instance UnityEngine.Rect InventoryDlg.<OnGUI>g__L|132_0(
    //     System.Single, System.Single, System.Single, System.Single, <>c__DisplayClass132_0&)
    // The origin comes from the cursor, not from winX/winY.
    private Rect L(float x, float y, float w, float h, ref DrawCursor dc) =>
        new Rect(dc.X + x * s, dc.Y + y * s, w * s, h * s);

    // Official 0x06000535, static System.Boolean InventoryDlg.<OnGUI>g__IsWeap|132_4(System.Int32)
    private static bool IsWeap(int w) => w == 4 || w == 13;

    // Official 0x06000530, instance System.Void InventoryDlg.DrawCoin(
    //     UnityEngine.Rect, UnityEngine.Texture2D, System.Int64)
    // The icon is 12*scale square drawn at r.x + 2*scale; the amount text starts at r.x + 16*scale.
    private void DrawCoin(Rect r, Texture2D ico, long v)
    {
        if (ico != null) GUI.DrawTexture(new Rect(r.x + (s + s), r.y, s * 12f, s * 12f), ico);
        if (BitmapFont.Ready) BitmapFont.Draw(r.x + s * 16f, r.y, v.ToString(), Color.white, s);
    }

    // Official 0x06000536, instance System.Void InventoryDlg.<OnGUI>g__CoinFlow|132_1(
    //     UnityEngine.Texture2D, System.Int64, <>c__DisplayClass132_0&)
    // Draws one coin entry at (dc.X + dc.coinX * s, dc.Y + 479 * s) sized (36, 12) in design units,
    // then advances dc.coinX by the measured text width plus 22 (the 16 and 6 insets).
    private void CoinFlow(Texture2D coin, long amount, ref DrawCursor dc)
    {
        Rect r = new Rect(dc.X + dc.coinX * s, dc.Y + 479f * s, 36f * s, 12f * s);
        DrawCoin(r, coin, amount);
        float originalCoinX = dc.coinX;
        bool fontReady = BitmapFont.Ready;
        string text = amount.ToString();
        float width = fontReady
            ? BitmapFont.Measure(text, 1f)
            : text.Length * 6f;
        dc.coinX = originalCoinX + (width + 16f + 6f);
    }
}
}

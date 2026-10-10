using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    /// <summary>
    /// WBC-039 lane C: official <c>StallDlg</c> layout helper and money renderer.
    /// Covers 06000db9 <c>L(float,float,float,float)</c> and 06000dbb
    /// <c>Coins(float,float,long,Color)</c>, read on both frozen architectures
    /// (Build/ReferenceReverseGhidra-1.5.8/queries/wbc038-arm64-StallDlg and
    /// wbc038-x86_64-StallDlg).
    /// </summary>
    public partial class StallDlg
    {
        // 06000db9: the arm64 body is winX (+0x78) + x * s (+0x80) and the x86_64 body is the same
        // expression built from *(this + 0x78) and *(this + 0x80). The remaining three components are
        // established by the two official inlinings of this helper, not by the standalone body:
        //   - 06000dbb draws the coin icon at (winX + x * s, winY + y * s, s * 12.0, s * 12.0)
        //     for the call L(x, y, 12f, 12f);
        //   - 06000dbc sizes the frame with WinZ.Frame(winX, winY, s * 569.0, s * 426.0), i.e.
        //     L(0f, 0f, NW, NH).
        // So every component is scaled by s and the offset pair is winX/winY.
        private Rect L(float x, float y, float w, float h)
        {
            return new Rect(winX + x * s, winY + y * s, w * s, h * s);
        }

        /// <summary>
        /// 06000dbb: draws one money amount as three icon/amount pairs (gold, silver, copper).
        /// </summary>
        /// <remarks>
        /// Splitting follows the sibling <see cref="MoneyWords"/> (06000dba): gold = money / 1000000,
        /// silver = money % 1000000 / 1000, copper = money % 1000. The x86_64 body keeps all three in
        /// 64-bit (it really emits <c>money % 1000000</c>); arm64 narrows only the silver term to
        /// 32-bit - <c>(long)(((int)money + (int)gold * -1000000) / 1000)</c> - which is a legal
        /// range-based narrowing because money % 1000000 always lies inside int range, so both
        /// architectures agree for every money value the protocol can carry.
        /// Each pair is guarded independently: the icon is drawn when that texture is non-null and the
        /// amount is printed only when <see cref="BitmapFont.Ready"/> is true, so a missing texture
        /// never suppresses the number. There is no early-out and no clamp: a non-null check failing
        /// simply skips that icon.
        /// </remarks>
        private void Coins(float x, float y, long money, Color c)
        {
            long gold = money / 1000000;
            long silver = money % 1000000 / 1000;
            long copper = money % 1000;

            if (goldIc != null)
            {
                GUI.DrawTexture(L(x, y, 12f, 12f), goldIc);
                if (BitmapFont.Ready)
                    BitmapFont.DrawOutline(winX + (x + 14f) * s, winY + y * s, gold.ToString(), c, s);
            }

            if (silverIc != null)
            {
                GUI.DrawTexture(L(x + 52f, y, 12f, 12f), silverIc);
                if (BitmapFont.Ready)
                    BitmapFont.DrawOutline(winX + (x + 66f) * s, winY + y * s, silver.ToString(), c, s);
            }

            if (copperIc != null)
            {
                GUI.DrawTexture(L(x + 104f, y, 12f, 12f), copperIc);
                if (BitmapFont.Ready)
                    BitmapFont.DrawOutline(winX + (x + 118f) * s, winY + y * s, copper.ToString(), c, s);
            }
        }
    }
}

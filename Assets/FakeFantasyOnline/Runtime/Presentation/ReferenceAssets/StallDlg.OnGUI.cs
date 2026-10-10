using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// WBC-040 lane C: official <c>StallDlg.OnGUI</c> (06000dbc), read on both frozen architectures
/// (<c>Build/ReferenceReverseGhidra-1.5.8/queries/wbc038-arm64-StallDlg/06000dbc.c</c> and
/// <c>wbc038-x86_64-StallDlg/06000dbc.c</c>).
/// </summary>
/// <remarks>
/// PARTIAL (WBC-040, still valid): the row layout here is the native draw order; nothing below was
/// re-read in WBC-041.
/// WBC-041: the row button click now answers with the recovered <c>StallDlg.Deal(int)</c>
/// (06000dbd, arm64 <c>bl</c> at 0x337fec, x86_64 equivalent) before <c>Event.Use()</c>, which is
/// the native order - the click is consumed after the call, never before it. <c>Deal</c> itself and
/// its two local functions live in <c>StallDlg.Deal.cs</c>.
/// The window still has no official opener, so this remains owner-only: no 0x803C entry, no
/// second stall dialog and no invented caller.
/// </remarks>
public partial class StallDlg
{
    // The two Vector2 literals the arm64 body reads from __const. Both were resolved through the
    // access instructions plus the Mach-O section map of the frozen 1.5.8 arm64 image
    // (__TEXT,__const at vm 0x1126ec0, file offset == vm address):
    //   DAT_011276a0 -> 0x11276a0 = 00 c0 07 44 00 00 a8 41 = (543f, 21f)  close button
    //   DAT_011276a8 -> 0x11276a8 = 00 80 86 43 00 00 5c 42 = (269f, 55f)  scroll bar track
    // The independent x86_64 image carries the same geometry through its own constant pool.
    private const float CloseX = 543f;
    private const float CloseY = 21f;
    private const float BarX = 269f;
    private const float BarY = 55f;

    // Row geometry: the native body computes fVar17 = 43f * s once and reuses it for the row
    // pitch (rowH * i), the row rectangle height (rowH - 3f * s), the scroll content height
    // (rowH * rows.Count + 2f) and the NativeScroll.Wheel step.
    private const float RowH = 43f;
    private const float ViewX = 39f;
    private const float ViewY = 58f;
    private const float ViewW = 216f;
    private const float ViewH = 330f;

    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();

        // 06000dbc: s, winX and winY are refreshed on every frame before WinZ.Frame, with no
        // WinDrag.Stale gate; the native body recentres unconditionally.
        s = UIScale.Factor;
        winX = (Screen.width - NW * s) * 0.5f;
        winY = (Screen.height - NH * s) * 0.5f;

        (int depth, bool covered) = WinZ.Frame(this, L(0f, 0f, NW, NH), 10);
        Vector2 saved = Event.current.mousePosition;
        if (covered) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            Vector2 mp = Event.current.mousePosition;
            GUI.depth = depth;

            if (bg != null)
            {
                GUI.DrawTexture(L(0f, 0f, NW, NH), bg);
            }
            else
            {
                GUI.color = new Color(0.07f, 0.06f, 0.09f, 0.97f);
                GUI.DrawTexture(L(0f, 0f, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            if (BitmapFont.Ready)
            {
                // 17579 "收购清单" when type == 1, otherwise 17103 "出售商品".
                BitmapFont.DrawOutline(winX + 108f * s, winY + 25f * s,
                    type == 1 ? "收购清单" : "出售商品", GOLD, s);

                BitmapFont.DrawOutline(winX + 350f * s, winY + 26f * s,
                    BitmapFont.Elide(shopName, 189f * s, s), GOLD, s);

                // The full shop name is only tipped while it is actually elided.
                if (189f * s < BitmapFont.Measure(shopName, s) &&
                    new Rect(winX + 350f * s, winY + 22f * s, 189f * s, 16f * s).Contains(mp))
                {
                    UITip.Show(shopName, s);
                }
            }

            Rect closeRect = new Rect(winX + CloseX * s, winY + CloseY * s, 20f * s, 19f * s);
            if (closeIc != null) GUI.DrawTexture(closeRect, closeIc);
            if (closeRect.Contains(mp) &&
                Event.current.type == EventType.MouseDown &&
                Event.current.button == 0)
            {
                visible = false;
                Event.current.Use();
                PlayerController.SuppressWorldClick();
                return;
            }

            Rect view = new Rect(winX + ViewX * s, winY + ViewY * s, ViewW * s, ViewH * s);
            float rowH = RowH * s;
            float contentH = rowH * rows.Count + 2f;

            scroll = GUI.BeginScrollView(
                view,
                scroll,
                new Rect(0f, 0f, ViewW * s, contentH),
                GUIStyle.none,
                GUIStyle.none);

            int hover = -1;
            for (int i = 0; ; i++)
            {
                if (rows.Count <= i)
                {
                    GUI.EndScrollView();

                    if (hover >= 0 && view.Contains(Event.current.mousePosition))
                    {
                        Row tip = rows[hover];
                        // 16656 "{0} ×{1}"; the formatted name is only used when count > 1.
                        string head = tip.count > 1
                            ? string.Format("{0} ×{1}", tip.name, tip.count)
                            : tip.name;
                        UITip.Show(TooltipContent.Fmt(head, tip.desc), s);
                    }

                    // The trailing float argument is s6 = 43f * s at 0x33819c
                    // (`ldp s6, s2, [sp, #0x48]` / `fmul s0, s6, s0`), i.e. the row pitch, not a
                    // hidden default: Ghidra's prototype drops it from the argument list.
                    NativeScroll.Wheel(view, ViewH * s, contentH, ref scroll, rowH);
                    NativeScroll.BarAt(this,
                        new Rect(winX + BarX * s, winY + BarY * s, 15f * s, 332f * s),
                        ViewH * s, contentH, ref scroll);

                    if (rows.Count == 0 && BitmapFont.Ready)
                        BitmapFont.CenterOutline(view, "空", GRAY, s);

                    if (BitmapFont.Ready)
                    {
                        // 17566 "摊主: " concatenated with ownerName.
                        BitmapFont.DrawOutline(winX + 315f * s, winY + 62f * s,
                            "摊主: " + ownerName, GRAY, s);

                        // 10839 "收购商店: 把对应物品卖给摊主" when type == 1, otherwise
                        // 10827 "出售商店: 购买摊主的物品".
                        BitmapFont.DrawOutline(winX + 315f * s, winY + 80f * s,
                            type == 1 ? "收购商店: 把对应物品卖给摊主" : "出售商店: 购买摊主的物品",
                            GRAY, s);

                        BitmapFont.DrawOutline(winX + 300f * s, winY + 383f * s, "持有",
                            new Color(1f, 0.95f, 0.7f, 1f), s);
                    }

                    Coins(341f, 383f, InventoryDlg.Money, new Color(1f, 0.95f, 0.7f, 1f));

                    WinDrag.Handle(this, L(0f, 0f, NW, 24f), ref winX, ref winY);

                    // Every MouseDown inside the window is consumed so it cannot reach the world,
                    // unless a message box owns the click.
                    if (Event.current.type == EventType.MouseDown &&
                        L(0f, 0f, NW, NH).Contains(mp) &&
                        !SysMsgBox.Visible)
                    {
                        Event.current.Use();
                    }
                    break;
                }

                Row row = rows[i];
                float y = rowH * i;
                float rowDrawH = rowH - 3f * s;

                // Rows are drawn in the scroll view's content space, so the rectangle starts at
                // x = 0 and the window offset only comes from BeginScrollView.
                Rect rowRect = new Rect(0f, y, ViewW * s, rowDrawH);
                if (itemBg != null)
                {
                    GUI.DrawTexture(rowRect, itemBg);
                }
                else
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.05f);
                    GUI.DrawTexture(rowRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                if (row.icon != null)
                    ItemIconUI.Draw(new Rect(4f * s, y + 5f * s, 32f * s, 32f * s), row.icon, s);

                string label = row.count > 1
                    ? string.Format("{0} ×{1}", row.name, row.count)
                    : row.name;

                if (BitmapFont.Ready)
                {
                    // Elide width: (216 - 52 - 40 - 4) * s, i.e. the row width minus the button
                    // column and the two paddings.
                    BitmapFont.DrawOutline(40f * s, y + 5f * s,
                        BitmapFont.Elide(label, 120f * s, s), Color.white, s);

                    // 17571 "收价 " when type == 1, otherwise 17153 "单价 ".
                    string price = type == 1 ? "收价 " : "单价 ";
                    BitmapFont.DrawOutline(40f * s, y + 23f * s, price, YEL, s);
                    GroUI.MoneyInline(BitmapFont.Measure(price, s) + 40f * s, y + 23f * s,
                        row.price, s);
                }

                // The hovered row is the one under the pointer; the last read wins because the
                // assignment only happens when the pointer is inside that row.
                if (rowRect.Contains(Event.current.mousePosition)) hover = i;

                // Button column: x = (216 - 52) * s, 48 x 22, inset 9 from the row top.
                Rect button = new Rect(164f * s, y + 9f * s, 48f * s, 22f * s);
                bool over = button.Contains(Event.current.mousePosition);
                GUI.color = over
                    ? new Color(0.55f, 0.42f, 0.18f, 0.95f)
                    : new Color(0.32f, 0.26f, 0.16f, 0.9f);
                GUI.DrawTexture(button, Texture2D.whiteTexture);
                GUI.color = Color.white;

                if (BitmapFont.Ready)
                {
                    // 17102 "出售" when type == 1, otherwise 18088 "购买".
                    BitmapFont.CenterOutline(button, type == 1 ? "出售" : "购买", GOLD, s);
                }

                if (over &&
                    Event.current.type == EventType.MouseDown &&
                    Event.current.button == 0)
                {
                    // Native calls StallDlg.Deal(i) here (06000dbd) and only then consumes the
                    // event; both steps are restored in that order (WBC-041).
                    Deal(i);
                    Event.current.Use();
                }
            }
        }
        finally
        {
            if (covered) Event.current.mousePosition = saved;
        }
    }
}

}

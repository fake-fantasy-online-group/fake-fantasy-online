using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 family stash window composition.
/// </summary>
/// <remarks>
/// Official 06000359, instance System.Void FamilyStashDlg.OnGUI() (arm64 rva 0x4076d4, 8368 bytes;
/// x86_64 rva 0x41c540, 9344 bytes).
///
/// The window is a 470x360 plate with two independent scroll columns: the left one lists every bag
/// row that may be deposited, the right one lists the stash contents. A single <c>tip</c> slot is
/// shared by both columns and is flushed once, after the permission row, through UITip.Show.
///
/// The close button is the only early exit: clicking it clears <c>visible</c> and skips the whole
/// body for that frame, so the "x" glyph and both columns disappear together.
///
/// The captured confirm closure is the official nested type
/// FamilyStashDlg+&lt;&gt;c__DisplayClass28_0 (0600035d / 0600035e); it carries the bag tab and the
/// bag index of the clicked row, which is why the left column keeps a (tab, item) tuple instead of
/// the bare item.
/// </remarks>
public partial class FamilyStashDlg
{
    // Official 06000359. Restored from both frozen bodies; every rect, colour, literal and call
    // order below is read off the arm64 and x86_64 decompilations.
    private void OnGUI()
    {
        if (!visible)
        {
            return;
        }

        // The frame is raised with the scale carried over from the previous frame: UIScale.Factor
        // is only sampled once the try block opens.
        (int depth, bool covered) = WinZ.Frame(this, new Rect(winX, winY, s * NW, s * NH), 10);
        Vector2 savedMouse = Event.current.mousePosition;
        if (covered)
        {
            Event.current.mousePosition = new Vector2(-9999f, -9999f);
        }
        try
        {
            s = UIScale.Factor;
            if (WinDrag.Stale(winX, ref _wdGen))
            {
                winX = (Screen.width - NW * s) * 0.5f;
                winY = (Screen.height - NH * s) * 0.5f;
            }
            Vector2 mouse = Event.current.mousePosition;
            GUI.depth = depth;
            GUI.color = new Color(0.1f, 0.09f, 0.13f, 0.98f);
            GUI.DrawTexture(L(0f, 0f, NW, NH), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (BitmapFont.Ready)
            {
                BitmapFont.Draw(winX + s * 12f, winY + s * 10f, "家族仓库", GOLD, s);
            }
            WinDrag.Handle(this, L(0f, 0f, 446f, 26f), ref winX, ref winY);

            if (Click(L(448f, 6f, 16f, 16f), mouse))
            {
                visible = false;
            }
            else
            {
                if (BitmapFont.Ready)
                {
                    BitmapFont.Draw(winX + s * 450f, winY + s * 8f, "×", WHITE, s);
                }
                if (BitmapFont.Ready)
                {
                    BitmapFont.Draw(winX + s * 12f, winY + s * 32f, "我的背包（点一下存入）", WHITE, s);
                }

                // ---- left column: the depositable bag rows -------------------------------
                // Official 1.6.0 06000360 captures and draws this plate before building the bag.
                Rect leftPanel = L(8f, 46f, 218f, 276f);
                GUI.color = new Color(0f, 0f, 0f, 0.35f);
                GUI.DrawTexture(leftPanel, Texture2D.whiteTexture);
                GUI.color = Color.white;

                // Rebuilt every frame. Only rows without a pet, without a bind and without a
                // lock are offered, and each surviving row keeps the tab it came from.
                List<(byte tab, InventoryDlg.Item it)> bag = new List<(byte tab, InventoryDlg.Item it)>();
                for (byte tab = 0; tab < InventoryDlg.Items.Length; tab++)
                {
                    foreach (InventoryDlg.Item it in InventoryDlg.Items[tab])
                    {
                        if (!it.pet.HasValue && !it.bound && !it.locked)
                        {
                            bag.Add((tab, it));
                        }
                    }
                }

                string tip = null;

                // The content rect keeps one row of height even for an empty list.
                int bagRows = bag.Count > 1 ? bag.Count : 1;
                scL = GUI.BeginScrollView(leftPanel, scL,
                    new Rect(0f, 0f, 200f * s, (bagRows * 30) * s));
                for (int i = 0; i < bag.Count; i++)
                {
                    (byte tab, InventoryDlg.Item it) = bag[i];
                    Rect row = new Rect(2f * s, (i * 30) * s, 196f * s, 28f * s);
                    if (row.Contains(Event.current.mousePosition))
                    {
                        GUI.color = new Color(1f, 1f, 1f, 0.12f);
                        GUI.DrawTexture(row, Texture2D.whiteTexture);
                        GUI.color = Color.white;
                        tip = TooltipContent.Fmt(TooltipContent.QualityName(it.name, it.quality), it.desc);
                    }
                    if (it.icon != null)
                    {
                        ItemIconUI.Draw(new Rect(row.x + 2f, row.y + 1f, s * 26f, s * 26f), it.icon, s);
                    }
                    if (BitmapFont.Ready)
                    {
                        string txt = it.name + (it.count > 1 ? " ×" + it.count : string.Empty);
                        BitmapFont.Draw(row.x + s * 32f, row.y + s * 8f, txt, UITip.QualityColor(it.quality), s);
                    }
                    if (Click(row, Event.current.mousePosition))
                    {
                        byte dTab = tab;
                        int dIdx = it.bagIdx;
                        string ask = string.Concat(new string[]
                        {
                            "把【",
                            TooltipContent.QualityName(it.name, it.quality),
                            "】",
                            it.count > 1 ? "×" + it.count + " " : string.Empty,
                            "存入家族仓库吗？\n存入后有取用权限的族人都可以取走。"
                        });
                        SysMsgBox.Show(ask, () => NetClient.I?.FamilyStashForIndependentRuntime(1, dTab, dIdx, 0), null);
                    }
                }
                GUI.EndScrollView();

                // ---- capacity line between the two columns -------------------------------
                string head = string.Format("家族仓库 {0}/{1}", Stash.Count, Cap)
                    + (CanTake ? "（点一下取出）" : "（你的职位不能取出）");
                if (BitmapFont.Ready)
                {
                    BitmapFont.Draw(winX + s * 236f, winY + s * 32f, head,
                        CanTake ? WHITE : GRAY, s);
                }

                // ---- right column: the stash contents ------------------------------------
                Rect rightPanel = L(232f, 46f, 230f, 276f);
                GUI.color = new Color(0f, 0f, 0f, 0.35f);
                GUI.DrawTexture(rightPanel, Texture2D.whiteTexture);
                GUI.color = Color.white;
                int stashRows = Stash.Count > 1 ? Stash.Count : 1;
                scR = GUI.BeginScrollView(rightPanel, scR,
                    new Rect(0f, 0f, 212f * s, (stashRows * 30) * s));
                for (int i = 0; i < Stash.Count; i++)
                {
                    Entry e = Stash[i];
                    Rect row = new Rect(2f * s, (i * 30) * s, 208f * s, 28f * s);
                    if (row.Contains(Event.current.mousePosition))
                    {
                        GUI.color = new Color(1f, 1f, 1f, 0.12f);
                        GUI.DrawTexture(row, Texture2D.whiteTexture);
                        GUI.color = Color.white;
                        tip = TooltipContent.Fmt(TooltipContent.QualityName(e.name, e.quality), e.desc)
                            + "\n【" + e.by + "】存入";
                    }
                    if (e.icon != null)
                    {
                        ItemIconUI.Draw(new Rect(row.x + 2f, row.y + 1f, s * 26f, s * 26f), e.icon, s);
                    }
                    if (BitmapFont.Ready)
                    {
                        string txt = e.name + (e.count > 1 ? " ×" + e.count : string.Empty);
                        BitmapFont.Draw(row.x + s * 32f, row.y + s * 3f, txt, UITip.QualityColor(e.quality), s);
                        BitmapFont.Draw(row.x + s * 32f, row.y + s * 15f, e.by + " 存入", GRAY, s);
                    }
                    if (Click(row, Event.current.mousePosition) && CanTake)
                    {
                        NetClient.I?.FamilyStashForIndependentRuntime(2, 0, 0, i);
                    }
                }
                GUI.EndScrollView();

                // ---- bottom row: the take permission -------------------------------------
                if (IsLeader)
                {
                    if (BitmapFont.Ready)
                    {
                        BitmapFont.Draw(winX + s * 12f, winY + s * 330f, "谁可以取出：", WHITE, s);
                    }
                    DrawPosBtn(80f, "仅族长", 1, mouse);
                    DrawPosBtn(150f, "族长+长老", 2, mouse);
                    DrawPosBtn(240f, "全体族人", 10, mouse);
                }
                else if (BitmapFont.Ready)
                {
                    BitmapFont.Draw(winX + s * 12f, winY + s * 330f,
                        "取用权限由族长设置。存入不受限制，全族都可以放东西进来。", GRAY, s);
                }

                if (tip != null)
                {
                    UITip.Show(tip, s);
                }
            }
        }
        finally
        {
            if (covered)
            {
                Event.current.mousePosition = savedMouse;
            }
        }
    }
}

}

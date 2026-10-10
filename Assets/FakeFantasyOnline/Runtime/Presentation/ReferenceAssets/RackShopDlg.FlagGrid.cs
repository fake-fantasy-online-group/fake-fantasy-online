using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg
    {
        // Official 0x06000ab9.
        private void DrawFlagGrid(Vector2 mouse, byte mask, string title)
        {
            if (shopBk != null) GUI.DrawTexture(L(28f, 65f, 812f, 524f), shopBk);
            tipText = null;

            var rows = new List<Good>();
            foreach (Good good in goods)
                if (good.group == 255 && (good.flags & mask) != 0) rows.Add(good);

            Rect heading = L(150f, 98f, 396f, 34f);
            GUI.color = new Color(0.13333334f, 0.13333334f, 0.06666667f, 1f);
            GUI.DrawTexture(heading, Texture2D.whiteTexture);
            GUI.color = Color.white;
            FrameBox(heading);
            LblC(heading, title, GOLD);

            for (int i = 0; i < 5 && i < rows.Count; i++)
            {
                Good good = rows[i];
                float y = 105f + i * 72f;
                Rect row = L(38f, y, 106f, 64f);
                Rect icon = L(73f, y + 4f, 30f, 30f);
                Rect add = L(112f, y + 7f, 24f, 24f);
                if (row.Contains(mouse)) tipText = TooltipContent.Fmt(good.name, good.desc);
                if (good.icon != null) ItemIconUI.Draw(icon, good.icon, s);
                BlockMask(icon, good.blocked);
                DrawFlagAdd(add, mouse);
                if (IsGold(good.flags))
                    GroUI.MoneyInlineCentered(L(42f, y + 44f, 99f, 13f).x, L(42f, y + 44f, 99f, 13f).y,
                        L(42f, y + 44f, 99f, 13f).width, good.price, s);
                else
                    LblC(L(42f, y + 44f, 99f, 13f), good.price + "彩玉", YEL);
                if (Click(add, mouse) || Click(row, mouse)) AddToCart(good.id);
            }

            int pages = Mathf.Max(1, (rows.Count + PerPage - 1) / PerPage);
            page = Mathf.Clamp(page, 0, pages - 1);
            for (int i = 0; i < PerPage; i++)
            {
                int index = page * PerPage + i;
                if (index >= rows.Count) break;
                Good good = rows[index];
                float x = i % 2 == 0 ? 160f : 360f;
                float y = 140f + i / 2 * 51f;
                Rect row = L(x, y, 146f, 50f);
                Rect iconBg = L(x + 4f, y + 8f, 34f, 34f);
                Rect icon = L(x + 5f, y + 9f, 32f, 32f);
                Rect add = L(x + 145f, y + 12f, 24f, 24f);
                bool inCart = cart.ContainsKey(good.id);
                bool hover = row.Contains(mouse) || add.Contains(mouse);

                FrameBox(row);
                GUI.color = new Color(0f, 0f, 0f, 0.35f);
                GUI.DrawTexture(iconBg, Texture2D.whiteTexture);
                GUI.color = Color.white;
                if (inCart)
                {
                    GUI.color = new Color(1f, 0.85f, 0.3f, 0.2f);
                    GUI.DrawTexture(row, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                else if (hover)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.1f);
                    GUI.DrawTexture(row, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                if (hover) tipText = TooltipContent.Fmt(good.name, good.desc);

                if (good.icon != null) ItemIconUI.Draw(icon, good.icon, s);
                BlockMask(icon, good.blocked);
                int nameLength = good.remain < 0 ? 8 : 6;
                LblC(L(x + 42f, y + 5f, good.remain < 0 ? 104f : 74f, 12f),
                    Trunc(good.name, nameLength), inCart ? GOLD : Color.white);
                if (good.remain >= 0)
                {
                    string remain = good.remain > 0 ? "剩" + good.remain : "售罄";
                    Color remainColor = good.remain > 0
                        ? new Color(0.55f, 1f, 0.55f, 1f)
                        : new Color(1f, 0.45f, 0.45f, 1f);
                    LblC(L(x + 114f, y + 4f, 30f, 13f), remain, remainColor);
                }
                if (IsGold(good.flags))
                {
                    Lbl(x + 42f, y + 32f, "价格", GRAY);
                    GroUI.MoneyInline(winX + (x + 74f) * s, winY + (y + 32f) * s, good.price, s);
                }
                else
                    LblC(L(x + 42f, y + 32f, 100f, 12f), good.price + "彩玉", YEL);

                DrawFlagAdd(add, mouse);
                if (Click(add, mouse) || Click(row, mouse)) AddToCart(good.id);
            }

            if (rows.Count == 0)
                LblC(L(145f, 230f, 402f, 14f), goods.Count == 0 ? "载入中…" : "暂无" + title + "物品(后台货架管理添加)", GRAY);

            Rect previous = L(159f, 462f, 44f, 22f);
            Rect next = L(470f, 462f, 44f, 22f);
            DrawFlagPage(previous, page > 0, pageUp, mouse);
            DrawFlagPage(next, page < pages - 1, pageDn, mouse);
            if (Click(previous, mouse) && page > 0) page--;
            if (Click(next, mouse) && page < pages - 1) page++;
            LblC(L(231f, 464f, 230f, 18f), (page + 1) + " / " + pages, GRAY);

            DrawCart(mouse, true, mask == 2 ? (byte)1 : (byte)2);
            if (!string.IsNullOrEmpty(tipText)) UITip.Show(tipText, s);
        }

        private void DrawFlagAdd(Rect rect, Vector2 mouse)
        {
            Texture2D texture = carBtn == null ? null : (carBtn[rect.Contains(mouse) ? 1 : 0] ?? carBtn[0]);
            if (texture != null) GUI.DrawTexture(rect, texture);
            else
            {
                GUI.color = new Color(0.5f, 0.4f, 0.16f, 0.9f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                LblC(rect, "+", GOLD);
            }
        }

        private static void DrawFlagPage(Rect rect, bool enabled, Texture2D[] frames, Vector2 mouse)
        {
            if (frames == null || frames[0] == null) return;
            int index = enabled ? (rect.Contains(mouse) && frames.Length > 1 ? 1 : 0) : Mathf.Min(3, frames.Length - 1);
            GUI.DrawTexture(rect, frames[index]);
        }
    }
}

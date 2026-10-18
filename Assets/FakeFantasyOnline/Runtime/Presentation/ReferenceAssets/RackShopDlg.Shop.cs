using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg
    {
        private string tipText;
        private GUIStyle _searchSt;

        // Official 0x06000a96. The mask is visual; it does not veto purchase input.
        private static void BlockMask(Rect rect, byte blocked)
        {
            if (blocked == 0) return;
            Texture2D focus = InventoryDlg.FocusBk;
            GUI.color = InventoryDlg.ItemFocusBkColor;
            GUI.DrawTexture(rect, focus != null ? focus : Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Official 0x06000acc, also used by the fitting page.
        private bool FitPartTab(float x, float y, float width, string name, bool selected, Vector2 mouse)
        {
            Rect rect = L(x, y, width, 18f);
            TextButtonFace(rect, name, mouse, selected);
            return Click(rect, mouse);
        }

        // Official 0x06000ab6. The five featured rows and paged grid have distinct click paths.
        private void DrawShop(Vector2 mouse)
        {
            if (shopBk != null) GUI.DrawTexture(L(28f, 65f, 812f, 524f), shopBk);
            tipText = null;
            EnsureView();
            string[] names;
            if (cats != null && cats.Count > 0)
            {
                var ordered = new List<(byte, string)>(cats);
                ordered.Sort((a, b) => a.Item1 - b.Item1);
                names = new string[ordered.Count + 1];
                names[0] = "全部";
                for (int i = 0; i < ordered.Count; i++) names[i + 1] = ordered[i].Item2;
            }
            else names = new[] { "全部", "圣变装", "法宝", "部位灵", "宠物材料", "功能道具", "坐骑" };
            if (groupFilter >= names.Length) groupFilter = 0;

            float categoryX = 156f;
            int categoryRows = 1;
            foreach (string name in names)
            {
                float width = Mathf.Min(228f, name.Length * 12f + 8f);
                if (categoryX > 156f && categoryX + width > 384f) { categoryX = 156f; categoryRows++; }
                categoryX += width + 3f;
            }
            float categoryHeight = (categoryRows - 1) * 23f + 34f;
            int kind = ShopPartKind();
            string[] parts = kind == 0 ? AvatarPartOrder : kind == 1 ? SoulPartOrder : null;
            float partHeight = 0f;
            if (parts != null)
            {
                float partX = 191f;
                int partRows = 1;
                foreach (string part in parts)
                {
                    float width = part.Length * 12f + 8f;
                    if (partX > 156f && partX + width > 540f) { partX = 156f; partRows++; }
                    partX += width + 3f;
                }
                partHeight = partRows * 20f;
            }
            else if (shopPartTab >= 0) shopPartTab = -1;
            Rect categoryPanel = L(150f, 98f, 396f, categoryHeight + partHeight);
            FrameBox(categoryPanel);

            categoryX = 156f;
            float categoryY = 105f;
            for (int i = 0; i < names.Length; i++)
            {
                float width = Mathf.Min(228f, names[i].Length * 12f + 8f);
                if (categoryX > 156f && categoryX + width > 384f) { categoryX = 156f; categoryY += 23f; }
                Rect rect = L(categoryX, categoryY, width, 20f);
                bool selected = groupFilter == i;
                TextButtonFace(rect, names[i], mouse, selected);
                if (rect.Contains(mouse)) tipText = names[i];
                if (Click(rect, mouse)) { groupFilter = i; shopPartTab = -1; page = 0; }
                categoryX += width + 3f;
            }
            float partY = categoryY + 23f;
            if (parts != null)
            {
                if (FitPartTab(156f, partY, 32f, "全部", shopPartTab < 0, mouse)) { shopPartTab = -1; page = 0; }
                float partX = 191f;
                for (int i = 0; i < parts.Length; i++)
                {
                    float width = parts[i].Length * 12f + 8f;
                    if (partX + width > 540f) { partX = 156f; partY += 20f; }
                    if (FitPartTab(partX, partY, width, parts[i], shopPartTab == i, mouse)) { shopPartTab = i; page = 0; }
                    partX += width + 3f;
                }
            }
            InputStyle(ref _searchSt, 12f, TextAnchor.MiddleLeft);
            Rect searchRect = L(390f, 105f, 150f, 20f);
            ArtworkSkin.Input(searchRect, s, GUI.GetNameOfFocusedControl() == "rack_search");
            GUI.SetNextControlName("rack_search");
            string nextSearch = UiInput.Field(searchRect, "rack_search", search ?? "", 24, _searchSt, s);
            if (nextSearch != search) search = nextSearch;
            if (string.IsNullOrEmpty(search) && GUI.GetNameOfFocusedControl() != "rack_search")
                LblC(searchRect, "搜索…", GRAY);

            float gridTop = categoryHeight + partHeight + 98f + 8f;
            for (int i = 0; i < 5; i++)
            {
                if (i >= view.Count) continue;
                Good good = view[i];
                float y = 105f + i * 72f;
                Rect row = L(38f, y, 106f, 64f);
                Rect icon = L(73f, y + 4f, 30f, 30f);
                Rect add = L(112f, y + 7f, 24f, 24f);
                if (row.Contains(mouse)) tipText = TooltipContent.Fmt(good.name, good.desc);
                DrawGoodIcon(icon, good);
                BlockMask(icon, good.blocked);
                Texture2D texture = carBtn == null ? null : (carBtn[add.Contains(mouse) ? 1 : 0] ?? carBtn[0]);
                if (texture != null) GUI.DrawTexture(add, texture);
                else
                {
                    GUI.color = new Color(0.5f, 0.4f, 0.16f, 0.9f);
                    GUI.DrawTexture(add, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    LblC(add, "+", GOLD);
                }
                Rect price = L(42f, y + 44f, 99f, 13f);
                if (IsGold(good.flags)) GroUI.MoneyInlineCentered(price.x, price.y, price.width, good.price, s);
                else LblC(price, good.price + "彩玉", YEL);
                if (Click(add, mouse) || Click(row, mouse)) AddToCart(good.id);
            }

            int pages = Mathf.Max(1, (view.Count + PerPage - 1) / PerPage);
            page = Mathf.Clamp(page, 0, pages - 1);
            for (int i = 0; i < PerPage; i++)
            {
                int index = page * PerPage + i;
                if (index >= view.Count) break;
                Good good = view[index];
                float x = i % Cols == 0 ? 160f : 360f;
                float y = gridTop + i / Cols * 51f;
                Rect row = L(x, y, 146f, 50f);
                Rect add = L(x + 145f, y + 12f, 24f, 24f);
                bool inCart = cart.ContainsKey(good.id);
                if (itemBk != null) GUI.DrawTexture(row, itemBk);
                if (inCart || row.Contains(mouse))
                {
                    GUI.color = inCart ? new Color(1f, 0.85f, 0.3f, 0.22f) : new Color(1f, 1f, 1f, 0.12f);
                    GUI.DrawTexture(row, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                if (row.Contains(mouse) || add.Contains(mouse)) tipText = TooltipContent.Fmt(good.name, good.desc);
                Rect icon = L(x + 3f, y + 8f, 32f, 32f);
                DrawGoodIcon(icon, good);
                BlockMask(icon, good.blocked);
                LblC(L(x + 40f, y + 3f, 104f, 12f), Trunc(good.name, 8), inCart ? GOLD : Color.white);
                bool gold = IsGold(good.flags);
                LblC(L(x + 40f, y + 33f, 100f, 12f), gold ? good.price + "金币" : good.price.ToString(),
                    gold ? new Color(1f, 0.62f, 0.2f, 1f) : YEL);
                Texture2D texture = carBtn == null ? null : (carBtn[add.Contains(mouse) ? 1 : 0] ?? carBtn[0]);
                if (texture != null) GUI.DrawTexture(add, texture);
                else
                {
                    GUI.color = new Color(0.5f, 0.4f, 0.16f, 0.9f);
                    GUI.DrawTexture(add, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    LblC(add, gold ? "买" : "+", GOLD);
                }
                bool clicked = Click(add, mouse);
                if (!gold)
                {
                    if (clicked || Click(row, mouse)) AddToCart(good.id);
                }
                else if (clicked)
                {
                    int id = good.id;
                    SysMsgBox.Show(string.Format("确定用 {0} 金币购买【{1}】？\n(金币商城为即时扣款, 不进购物篮)",
                        (long)good.price, good.name), () => NetClient.I?.BuyRackForIndependentRuntime(id, 0, 1));
                }
            }
            if (view.Count == 0)
                LblC(L(145f, gridTop + 90f, 402f, 14f), goods.Count == 0 ? "载入中…" : "该分组暂无商品", GRAY);
            float pagingY = gridTop + 306f + 16f;
            Rect prev = L(159f, pagingY, 44f, 22f);
            Rect next = L(470f, pagingY, 44f, 22f);
            if (pageUp != null && pageUp[0] != null)
                GUI.DrawTexture(prev, pageUp[page > 0 ? (prev.Contains(mouse) && pageUp.Length > 1 ? 1 : 0) : Mathf.Min(3, pageUp.Length - 1)]);
            if (pageDn != null && pageDn[0] != null)
                GUI.DrawTexture(next, pageDn[page < pages - 1 ? (next.Contains(mouse) && pageDn.Length > 1 ? 1 : 0) : Mathf.Min(3, pageDn.Length - 1)]);
            if (Click(prev, mouse) && page > 0) page--;
            if (Click(next, mouse) && page < pages - 1) page++;
            LblC(L(231f, pagingY + 2f, 230f, 18f), (page + 1) + " / " + pages, GRAY);
            DrawCart(mouse, false, 0);
            if (!string.IsNullOrEmpty(tipText)) UITip.Show(tipText, s);
        }
    }
}

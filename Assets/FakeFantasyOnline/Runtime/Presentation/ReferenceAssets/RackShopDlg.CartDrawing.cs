using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg
    {
        // Official 0x06000ab7, including the native text-edit and delayed removal order.
        private void DrawCart(Vector2 mouse, bool gold = false, byte src = 0)
        {
            Rect panel = L(590f, 98f, 210f, 333f);
            GUI.color = new Color(0f, 0f, 0f, 0.12f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;
            int displayedWeight = 0;
            int displayedPrice = 0;
            foreach (int id in cartOrder)
            {
                int quantity = cart[id];
                Good good = FindGood(id);
                displayedPrice = unchecked(displayedPrice + good.price * quantity);
                displayedWeight = unchecked(displayedWeight + good.weight * quantity);
            }
            Event e = Event.current;
            if (panel.Contains(mouse) && e.type == EventType.ScrollWheel)
            {
                cartScroll += e.delta.y < 0f ? -1 : 1;
                e.Use();
            }
            cartScroll = Mathf.Clamp(cartScroll, 0, Mathf.Max(0, cartOrder.Count - CartRows));
            int removeIndex = -1;
            for (int i = 0; i < CartRows; i++)
            {
                int index = i + cartScroll;
                if (index >= cartOrder.Count) break;
                int id = cartOrder[index];
                int quantity = cart[id];
                Good good = FindGood(id);
                float y = 100f + i * 36f;
                if (L(592f, y, 200f, 34f).Contains(mouse)) tipText = TooltipContent.Fmt(good.name, good.desc);
                if (good.icon != null) ItemIconUI.Draw(L(596f, y + 2f, 30f, 30f), good.icon, s);
                Lbl(630f, y + 4f, Trunc(good.name, 7), GOLD);
                int maximum = good.remain > 0 ? Mathf.Min(good.remain, CartQtyMax) : CartQtyMax;
                Rect background = L(642f, y + 16f, 30f, 14f);
                Rect minus = L(628f, y + 16f, 13f, 14f);
                Rect plus = L(673f, y + 16f, 13f, 14f);
                GUI.color = new Color(0f, 0f, 0f, 0.35f);
                GUI.DrawTexture(background, Texture2D.whiteTexture);
                GUI.color = Color.white;
                LblC(minus, "-", minus.Contains(mouse) ? Color.white : YEL);
                LblC(plus, "+", plus.Contains(mouse) ? Color.white : YEL);
                string control = "cartq" + id;
                GUI.SetNextControlName(control);
                string text = qtyEdit.TryGetValue(id, out string edited) ? edited : quantity.ToString();
                if (text.Length == 0 && GUI.GetNameOfFocusedControl() != control)
                {
                    text = cart[id].ToString();
                    qtyEdit[id] = text;
                }
                string next = GUI.TextField(new Rect(background.x + 2f * s, background.y,
                    background.width - 4f * s, background.height), text, 3, QtyStyle());
                if (next != text)
                {
                    text = new string(Array.FindAll((next ?? "").ToCharArray(), char.IsDigit));
                    if (text.Length > 0 && int.TryParse(text, out int entered) && entered > maximum)
                        text = maximum.ToString();
                    qtyEdit[id] = text;
                    cart[id] = text.Length == 0 ? 1 : Mathf.Clamp(int.Parse(text), 1, maximum);
                }
                if (Click(minus, mouse))
                {
                    cart[id] = Mathf.Max(1, cart[id] - 1);
                    qtyEdit[id] = cart[id].ToString();
                }
                if (Click(plus, mouse))
                {
                    cart[id] = Mathf.Min(maximum, cart[id] + 1);
                    qtyEdit[id] = cart[id].ToString();
                }
                if (gold) GroUI.MoneyInline(winX + 690f * s, winY + (y + 18f) * s, good.price, s);
                else Lbl(690f, y + 18f, good.price + "彩玉/件", YEL);
                Rect remove = L(770f, y + 6f, 20f, 20f);
                if (remove.Contains(mouse))
                {
                    GUI.color = new Color(0.6f, 0.2f, 0.2f, 0.9f);
                    GUI.DrawTexture(remove, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                LblC(remove, "×", Color.white);
                if (Click(remove, mouse)) removeIndex = index;
            }
            if (removeIndex >= 0)
            {
                qtyEdit.Remove(cartOrder[removeIndex]);
                cart.Remove(cartOrder[removeIndex]);
                cartOrder.RemoveAt(removeIndex);
            }
            NativeScroll.BarRows(this, L(795f, 116f, 15f, 297f), CartRows, cartOrder.Count, ref cartScroll);
            if (cartOrder.Count == 0)
                LblC(new Rect(panel.x, panel.y + panel.height * 0.5f - 7f * s, panel.width, 14f * s),
                    "购物篮(空) — 点商品车钮加入", GRAY);
            if (prunedCount > 0 && Time.realtimeSinceStartup < prunedUntil)
                LblC(L(590f, 88f, 210f, 12f), "货架已刷新, " + prunedCount + " 件已下架商品移出购物篮",
                    new Color(1f, 0.6f, 0.4f, 1f));
            if (gold)
            {
                GoldField(L(551f, 447f, 134f, 22f), "所需", displayedPrice);
                GoldField(L(551f, 474f, 134f, 22f), "持有", InventoryDlg.Money);
                LblC(L(753f, 454f, 60f, 13f), displayedWeight.ToString(), GOLD);
            }
            else
            {
                LblC(L(626f, 454f, 46f, 13f), displayedPrice.ToString(), GOLD);
                LblC(L(753f, 454f, 60f, 13f), displayedWeight.ToString(), GOLD);
                LblC(L(626f, 482f, 60f, 13f), InventoryDlg.CaiYu.ToString(), GOLD);
            }
            LblC(L(753f, 482f, 60f, 13f), InventoryDlg.Weight + "/" + InventoryDlg.MaxWeight, GOLD);
            if (StateBtn(L(592f, 519f, 79f, 39f), buyBtn, "购买", mouse))
            {
                int count = 0;
                long cost = 0;
                foreach (var row in cart)
                {
                    count = unchecked(count + row.Value);
                    foreach (Good good in goods)
                        if (good.id == row.Key)
                        {
                            cost = unchecked(cost + (long)row.Value * good.price);
                            break;
                        }
                }
                long available = gold ? InventoryDlg.Money : InventoryDlg.CaiYu;
                string currency = gold ? "金币" : "彩玉";
                if (count > 0 && available < cost)
                {
                    SysMsgBox.Show(string.Format("{0}不足, 无法结算。\n购物篮合计 {1} {2}, 你只有 {3} {4}。\n\n★购物篮会原样保留, 请先删掉一些商品再结算。",
                        currency, cost, currency, available, currency), null);
                    return;
                }
                if (count > 0)
                    SysMsgBox.Show(string.Format("确定购买购物篮里的 {0} 件商品？\n共需 {1} {2}。", count, cost, currency),
                        () => CheckoutCart(src));
            }
            if (StateBtn(L(706f, 530f, 90f, 22f), clearBtn, "清空", mouse))
            {
                cart.Clear();
                cartOrder.Clear();
                qtyEdit.Clear();
            }
        }

        // Official 0x06000ab8.
        private void GoldField(Rect rect, string label, long money)
        {
            GUI.color = new Color(20f / 255f, 18f / 255f, 13f / 255f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.58f, 0.48f, 0.24f, 1f);
            EdgeRect(rect, Mathf.Max(1f, s));
            GUI.color = Color.white;
            if (BitmapFont.Ready)
                BitmapFont.DrawOutline(rect.x + 5f * s, rect.y + (rect.height - 12f * s) * 0.5f,
                    label, new Color(0.85f, 0.75f, 0.45f, 1f), s);
            float width = GroUI.MoneyWidth(money, s);
            GroUI.MoneyInline(rect.x + rect.width - width - 5f * s,
                rect.y + (rect.height - 11f * s) * 0.5f, money, s);
        }
    }
}

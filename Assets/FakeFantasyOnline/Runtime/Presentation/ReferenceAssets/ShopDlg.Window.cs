using System;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ShopDlg
{
    // Native repair selection also works when the shop opens without a map borrower.
    private Action<byte> beginRepairSelection = RepairDlg.BeginPick;
    internal void BindRepairSelectionForIndependentRuntime(Action<byte> begin) => beginRepairSelection = begin;
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);

    public bool MouseOver()
    {
        if (!visible || float.IsNaN(winX)) return false;
        float scale = UIScale.Factor;
        Vector2 mp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        return new Rect(winX, winY, NW * scale, NH * scale).Contains(mp);
    }

    private void OnGUI()
    {
        if (!visible) return;
        if (!triedAssets) LoadAssets();
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - NW * s) * 0.5f;
            winY = (Screen.height - NH * s) * 0.5f;
        }
        var stacking = WinZ.Frame(this, L(0, 0, NW, NH), 10);
        Vector2 savedMouse = Event.current.mousePosition;
        if (stacking.Item2) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            Vector2 mp = Event.current.mousePosition;
            bool block = picking || SysMsgBox.Visible;
            GUI.depth = stacking.Item1;
            if (picking) PickerInput(mp);
            if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg);
            else
            {
                GUI.color = new Color(0.07f, 0.06f, 0.09f, 0.97f);
                GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (stallMode && stallType == 1 && buyLabelIc != null) GUI.DrawTexture(L(7, 18, 46, 26), buyLabelIc);
            if (BitmapFont.Ready)
            {
                string left, right;
                if (stallMode)
                {
                    left = stallType == 0 ? "出售商品" : "我的物品栏";
                    right = string.IsNullOrEmpty(stallName) ? "购物车" : stallName;
                }
                else
                {
                    left = sellMode ? "我的物品栏" : "购买商品";
                    right = sellMode ? "出售清单" : "购物车";
                }
                Color title = new Color(1f, 0.9f, 0.6f, 1f);
                BitmapFont.DrawOutline(winX + 108f * s, winY + 25f * s, left, title, s);
                BitmapFont.DrawOutline(winX + 350f * s, winY + 26f * s, right, title, s);
            }
            Rect close = L(543, 21, 20, 19);
            if (closeIc != null) GUI.DrawTexture(close, closeIc);
            if (!block && close.Contains(mp) && Clicked())
            {
                visible = false;
                Event.current.Use();
                PlayerController.SuppressWorldClick();
                return;
            }
            tipText = null;
            tipPet = null;
            float rowH = 43f * s;
            Rect leftView = L(39, 58, 216, 330);
            int sourceCount = sellMode ? SellSrcCount() : buyItems.Count;
            float sourceHeight = rowH * sourceCount + 2f;
            scrollL = GUI.BeginScrollView(leftView, scrollL, new Rect(0, 0, leftView.width, sourceHeight), GUIStyle.none, GUIStyle.none);
            DrawSource(leftView.width, rowH, block);
            GUI.EndScrollView();
            NativeScroll.Wheel(leftView, leftView.height, sourceHeight, ref scrollL, rowH);
            NativeScroll.BarAt(this, L(269, 55, 15, 332), leftView.height, sourceHeight, ref scrollL);
            Rect rightView = L(305, 52, 231, 293);
            scrollR = GUI.BeginScrollView(rightView, scrollR, new Rect(0, 0, rightView.width, rowH * cart.Count + 2f), GUIStyle.none, GUIStyle.none);
            DrawCart(rightView.width, rowH, block);
            GUI.EndScrollView();
            NativeScroll.Wheel(rightView, rightView.height, rowH * cart.Count + 2f, ref scrollR, rowH);
            NativeScroll.BarAt(this, L(547, 47, 15, 296), rightView.height, rowH * cart.Count + 2f, ref scrollR);
            if (!block)
            {
                if (!string.IsNullOrEmpty(tipText)) UITip.Show(tipText, s);
                if (tipPet.HasValue) PetInfoTooltip.Show(tipPet.Value, Event.current.mousePosition, s, "(摊位出售)");
            }
            Coins(341, 358, CartTotal(), Color.white);
            Coins(341, 383, InventoryDlg.Money, Color.white);
            if (!block)
            {
                if (GsaBtn(L(296, 400, 90, 22), tradeBtn, "交易", mp)) Confirm();
                if (GsaBtn(L(386, 400, 70, 22), clearBtn, "清空", mp)) cart.Clear();
                if (!stallMode)
                {
                    if (GsaBtn(L(24, 398, 90, 22), repairAll, "全部修理", mp))
                        if (NetClient.I != null) NetClient.I.RepairForIndependentRuntime(2, -1, 1, 0);
                    if (GsaBtn(L(118, 398, 90, 22), repairSpec, "特殊修理", mp)) beginRepairSelection(1);
                    if (GsaBtn(L(212, 398, 70, 22), repairOne, "修理", mp)) beginRepairSelection(0);
                }
            }
            if (!picking) WinDrag.Handle(this, L(0, 0, NW, 44), ref winX, ref winY);
            if (picking) PickerRender(mp);
        }
        finally
        {
            if (stacking.Item2) Event.current.mousePosition = savedMouse;
        }
    }
}
}

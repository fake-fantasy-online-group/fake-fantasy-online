using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class StoreDlg
{
    // Official 1.5.8 06000feb–06000fee callbacks: no optimistic money changes.
    private static void DepositMoney(long amount)
    { if (amount > 0 && NetClient.I != null) NetClient.I.WarehouseMoneyForIndependentRuntime(0, amount); }
    private static long DepositLimit() => InventoryDlg.Money;
    private static void WithdrawMoney(long amount)
    { if (amount > 0 && NetClient.I != null) NetClient.I.WarehouseMoneyForIndependentRuntime(1, amount); }
    private static long WithdrawLimit() => WhBank;

    // Official 1.5.8 06000fe5. Slot mapping is rebuilt from authoritative WhItems each frame.
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        s = UIScale.Factor;
        bool unpositioned = float.IsNaN(winX);
        if (unpositioned) { winX = (Screen.width - NW * s) * .5f; winY = (Screen.height - NH * s) * .5f; }
        var frame = WinZ.Frame(this, L(0, 0, NW, NH), 10);
        Vector2 saved = Event.current.mousePosition;
        if (frame.Item2) Event.current.mousePosition = new Vector2(-9999, -9999);
        try
        {
            Vector2 mouse = Event.current.mousePosition;
            GUI.depth = frame.Item1;
            if (needDock)
            {
                float limit = Mathf.Max(0, Screen.width - 850 * s);
                winX = unpositioned ? limit * .5f : Mathf.Clamp(winX, 0, limit);
                winY = Mathf.Clamp(winY, 0, Mathf.Max(0, Screen.height - NH * s));
                InventoryDlg.DockRightOf(winX + 427 * s, winY);
                needDock = false;
            }
            if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg);
            else
            {
                GUI.color = new Color(.09f, .08f, .12f, .98f);
                GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            Lbl(67, 22, "我的仓库", GOLDc);
            for (int i = 0; i < Mathf.Clamp(TabCount, 1, 4); i++)
            {
                Rect r = L(9 + i * 38, 38, 38, 23);
                if (tabCover[i] != null)
                {
                    Color old = GUI.color;
                    GUI.color = new Color(1, 1, 1, i == tab ? 1 : .55f);
                    GUI.DrawTexture(r, tabCover[i]); GUI.color = old;
                }
                else
                {
                    GUI.color = i == tab ? new Color(.3f, .25f, .15f, .6f) : new Color(0, 0, 0, .3f);
                    GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white;
                    if (BitmapFont.Ready) BitmapFont.CenterOutline(r, "仓" + (i + 1), i == tab ? GOLDc : WhUI.GRAY, s);
                }
                if (WhUI.Click(r, mouse)) tab = i;
            }
            Rect sort = L(363, 38, 48, 21);
            Color previous = GUI.color;
            GUI.color = new Color(64f / 255, 54f / 255, 38f / 255, 235f / 255);
            GUI.DrawTexture(sort, Texture2D.whiteTexture); GUI.color = previous;
            if (BitmapFont.Ready) BitmapFont.Draw(sort.x + 6 * s, sort.y + 5 * s, "整理", new Color(235f / 255, 220f / 255, 180f / 255, 1), s);
            if (WhUI.Click(sort, mouse))
            {
                InventoryDlg.ClearHeld();
                if (NetClient.I != null) NetClient.I.SortWarehouseForIndependentRuntime(tab);
            }
            previous = GUI.color;
            GUI.color = new Color(20f / 255, 18f / 255, 15f / 255, 1);
            GUI.DrawTexture(L(12, 64, 400, 402), Texture2D.whiteTexture); GUI.color = previous;
            if (slotIdx == null || slotIdx.Length != Slots) slotIdx = new int[Slots];
            for (int i = 0; i < Slots; i++) slotIdx[i] = -1;
            for (int i = 0; i < WhItems.Count; i++)
            {
                WItem item = WhItems[i];
                if (item.tab == tab && (uint)item.slot < Slots) slotIdx[item.slot] = i;
            }
            for (int slot = 0; slot < Slots; slot++)
            {
                Rect cell = L(14 + slot % Cols * Pitch, GridTop + slot / Cols * Pitch, Cell, Cell);
                if (slotTex != null) GUI.DrawTexture(cell, slotTex);
                else
                {
                    GUI.color = new Color(0, 0, 0, .22f);
                    GUI.DrawTexture(cell, Texture2D.whiteTexture); GUI.color = Color.white;
                }
                if (cell.Contains(mouse) && !InventoryDlg.SplitOpen)
                {
                    previous = GUI.color; GUI.color = new Color(1, 1, 1, .18f);
                    GUI.DrawTexture(cell, Texture2D.whiteTexture); GUI.color = previous;
                }
                if (InventoryDlg.HeldFromWarehouse && !InventoryDlg.SplitOpen &&
                    Event.current.type == EventType.MouseDown && Event.current.button == 0 && cell.Contains(mouse) && NetClient.I != null)
                {
                    NetClient.I.WarehouseRearrangeForIndependentRuntime(InventoryDlg.HeldWhTab, InventoryDlg.HeldWhSlot, tab, slot);
                    InventoryDlg.ClearHeld(); Event.current.Use();
                }
                if (slotIdx[slot] < 0) continue;
                WItem item = WhItems[slotIdx[slot]];
                Rect icon = new Rect(cell.x + 4 * s, cell.y + 4 * s, cell.width - 8 * s, cell.height - 8 * s);
                if (item.icon != null) ItemIconUI.Draw(icon, item.icon, s);
                else if (BitmapFont.Ready && !string.IsNullOrEmpty(item.name))
                    BitmapFont.Draw(cell.x + 5 * s, cell.y + 11 * s, item.name.Substring(0, Mathf.Min(2, item.name.Length)),
                        item.quality > 0 ? (Color)UITip.QualityColor(item.quality) : Color.white, s);
                if (item.blocked != 0 || item.maxDur > 0 && item.curDur <= 0)
                {
                    Texture2D focus = InventoryDlg.FocusBk;
                    GUI.color = InventoryDlg.ItemFocusBkColor;
                    GUI.DrawTexture(icon, focus != null ? focus : Texture2D.whiteTexture); GUI.color = Color.white;
                }
                if (item.count > 1) GameHud.DrawCountBadge(cell, item.count.ToString(), Color.white, s);
                GameHud.DrawWashScrollTag(cell, item.id, s);
                if (cell.Contains(mouse) && !InventoryDlg.SplitOpen)
                    UITip.Show(TooltipContent.Fmt(TooltipContent.QualityName(item.name, item.quality), item.desc), s);
                bool pickup = Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                    cell.Contains(mouse) && !InventoryDlg.HasHeldAny && !InventoryDlg.SplitOpen;
                if (pickup && InventoryDlg.ShiftHeld(Event.current) && item.count > 1)
                {
                    InventoryDlg.OpenSplitWarehouse((byte)item.tab, item.slot, item.count, item.icon, mouse);
                    Event.current.Use(); continue;
                }
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && cell.Contains(mouse) &&
                    !InventoryDlg.HasHeldAny && !InventoryDlg.SplitOpen && NetClient.I != null)
                {
                    NetClient.I.WarehouseMoveForIndependentRuntime(1, 0, item.slot, item.tab, item.count, -1);
                    Event.current.Use(); continue;
                }
                if (pickup)
                {
                    InventoryDlg.PickUpFromWarehouse(item.tab, item.slot, item.icon, item.name, item.count);
                    Event.current.Use();
                }
            }
            float coinX = 64;
            Color coinColor = new Color(1, .95f, .7f, 1);
            Coin(icoGold, WhBank / 1000000, ref coinX, coinColor);
            Coin(icoSilver, unchecked((int)WhBank - (int)(WhBank / 1000000) * 1000000) / 1000, ref coinX, coinColor);
            Coin(icoCopper, WhBank % 1000, ref coinX, coinColor);
            if (BitmapFont.Ready)
            {
                int count = 0;
                foreach (WItem item in WhItems) if (item.tab == tab) count++;
                string text = "第" + (tab + 1) + "页  " + count + "/" + Slots;
                BitmapFont.Draw(winX + 362 * s - BitmapFont.Measure(text, s), winY + CoinY * s, text,
                    count < Slots ? Color.white : new Color(1, 120f / 255, 120f / 255, 1), s);
            }
            Rect save = L(11, BtnY, 23, 20), cash = L(35, BtnY, 23, 20);
            if (WhUI.Btn(save, icoSave, "存", mouse, s))
                MoneyInputDlg.Open(DepositMoney, save, DepositLimit, "身上");
            if (WhUI.Btn(cash, icoCash, "取", mouse, s))
                MoneyInputDlg.Open(WithdrawMoney, cash, WithdrawLimit, "仓库");
            if (icoHelp != null) GUI.DrawTexture(L(377, 17, 20, 19), icoHelp);
            if (icoClose != null) GUI.DrawTexture(L(398, 17, 20, 19), icoClose);
            if (WhUI.Click(L(398, 17, 20, 19), mouse)) { visible = false; InventoryDlg.ClearHeld(); }
            WinDrag.Handle(this, L(0, 0, NW, 36), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(mouse) &&
                !SysMsgBox.Visible && !InventoryDlg.HasHeldAny && !InventoryDlg.SplitOpen) Event.current.Use();
        }
        finally { if (frame.Item2) Event.current.mousePosition = saved; }
    }
}
}

using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    private readonly MobilePanelTouch mobileTouch = new MobilePanelTouch();
    private int mobileSlot = -1, mobileId, mobileTab = -1;
    private Rect mobileViewport, mobileActionsRect;
    private bool mobileActions;

    internal static void ClearMobileSelectionForWorldChange()
    {
        if (I == null) return;
        I.ClearMobileSelection();
        MobileClientPresentation.RemovePanel(I);
    }
    private void ClearMobileSelection()
    {
        mobileSlot = -1; mobileId = 0; mobileActions = false;
        mobileTouch.Reset();
    }
    private void OnApplicationFocus(bool focused)
    { if (!focused) ClearMobileSelectionForWorldChange(); }
    private void OnApplicationPause(bool paused)
    { if (paused) ClearMobileSelectionForWorldChange(); }
    private void OnDisable() { ClearMobileSelectionForWorldChange(); }

    private void PrepareMobileInventory()
    {
        Rect safe = MobileClientPresentation.SafeAreaGui;
        if (safe != mobileViewport || mobileTab != tab)
        { ClearMobileSelection(); mobileViewport = safe; mobileTab = tab; }
        Rect panel = MobilePanelLayout.Fit(new Rect(0, 0, 423, 501), safe, ref s, ref winX, ref winY);
        MobileClientPresentation.RegisterPanel(this, panel);
        if (splitOpen)
        {
            splitWin.x = Mathf.Clamp(splitWin.x, safe.x, safe.xMax - 147 * s);
            splitWin.y = Mathf.Clamp(splitWin.y, safe.y, safe.yMax - 41 * s);
        }
    }
    private int MobileInventoryHit(Vector2 point)
    {
        if (UITip.MobileContainsPointer(point) || (mobileActions && mobileActionsRect.Contains(point))) return -1;
        // The original 100 cells retain their artwork, pitch and order. The empty
        // two-pixel gutters belong to the nearest cell instead of becoming dead zones.
        float col = (point.x - winX) / s - 12f, row = (point.y - winY) / s - 64f;
        if (col < 0 || col >= 400 || row < 0 || row >= 400) return -1;
        int slot = (int)(row / 40) * Cols + (int)(col / 40);
        return ItemAt(tab, slot).id > 0 ? slot : -1;
    }
    private void ReadMobileInventory(Event ev)
    {
        if (splitOpen) { ClearMobileSelection(); return; }
        bool selected = mobileTouch.Read(ev, s, MobileInventoryHit, out int slot, out bool held);
        if (mobileTouch.Started && !UITip.MobileContainsPointer(mobileTouch.StartPoint) && (!mobileActions || !mobileActionsRect.Contains(mobileTouch.StartPoint)))
        { mobileSlot = -1; mobileId = 0; mobileActions = false; }
        if (selected)
        {
            Item item = ItemAt(tab, slot);
            if (item.id > 0)
            {
                mobileSlot = slot; mobileId = item.id; mobileTab = tab; mobileActions = held;
                mobileActionsRect = MobilePanelLayout.Actions(mobileTouch.Anchor, 5, out _);
                PlayerController.SuppressWorldClick();
            }
        }
        if (mobileTouch.Canceled) ClearMobileSelection();
    }
    private void DrawMobileInventorySelection(Event ev)
    {
        if (mobileSlot < 0 || splitOpen || !open) return;
        Item item = ItemAt(tab, mobileSlot);
        if (mobileTab != tab || item.id != mobileId) { ClearMobileSelection(); return; }
        if (!mobileActions)
        {
            Vector2 previous = ev.mousePosition;
            // Reuse the exact native tooltip, including pet cards and separate comparisons.
            ev.mousePosition = mobileTouch.Anchor;
            ShowSlotTooltip(item, mobileTouch.Anchor, !item.pet.HasValue || item.pet.Value.opened != 0);
            ev.mousePosition = previous;
            return;
        }
        Rect box = MobilePanelLayout.Actions(mobileTouch.Anchor, 5, out float scale);
        mobileActionsRect = box;
        int depth = GUI.depth; GUI.depth = 0;
        bool offered = DealDlg.IsOffered((byte)tab, item.bagIdx);
        bool available = !offered && !splitOpen && !HasHeldAny;
        if (MobilePanelLayout.Action(box, 0, MobilePrimaryLabel(item), scale,
            DealDlg.Visible ? !splitOpen && !HasHeldAny && DealDlg.CanEditBagOffer : available)) ExecuteMobileAction(item, 0);
        if (MobilePanelLayout.Action(box, 1, item.locked ? "解锁" : "加锁", scale, available)) ExecuteMobileAction(item, 1);
        if (MobilePanelLayout.Action(box, 2, "拆分", scale, available && !item.pet.HasValue && item.count > 1)) ExecuteMobileAction(item, 2);
        if (MobilePanelLayout.Action(box, 3, "分享", scale, available)) ExecuteMobileAction(item, 3);
        if (MobilePanelLayout.Action(box, 4, "取消", scale)) ClearMobileSelection();
        if (box.Contains(ev.mousePosition) && (ev.type == EventType.MouseDown || ev.type == EventType.MouseUp || ev.type == EventType.MouseDrag)) ev.Use();
        GUI.depth = depth;
    }
    private string MobilePrimaryLabel(Item item)
    {
        if (DealDlg.Visible && DealDlg.IsOffered((byte)tab, item.bagIdx)) return "撤回交易";
        if (EquipFuseDlg.Picking || ToolTarget.Active || PetForgeDlg.Picking || AttrWashDlg.Picking || AvatarFuse.Active || RepairDlg.Picking) return "选择此物品";
        if (DealDlg.Visible) return "放入交易";
        if (!item.pet.HasValue && EndgamePanel.CubeContainerOpen) return "放入周天鼎";
        if (!item.pet.HasValue && StoreDlg.Visible) return "存入仓库";
        if (!item.pet.HasValue && Win05.IsShowing("magicstoredlg")) return "放入衣橱";
        if (StallConfigDlg.Active) return "摆摊";
        return item.pet.HasValue ? (item.pet.Value.opened != 0 ? "装备宠物" : "孵化") : tab == 2 ? "装备 / 使用" : "使用";
    }
    private void ExecuteMobileAction(Item item, int action)
    {
        // Re-resolve admission at execution, never dispatch from a cached tooltip snapshot.
        Item live = ItemAt(tab, mobileSlot);
        if (mobileSlot < 0 || mobileTab != tab || live.id <= 0 ||
            live.id != mobileId || live.id != item.id || HasHeldAny || splitOpen) return;
        bool offered = DealDlg.IsOffered((byte)tab, live.bagIdx);
        if (offered)
        {
            if (action == 0) DealDlg.ChangeBagOffer((byte)tab, live.bagIdx);
            ClearMobileSelection(); return;
        }
        var command = new Event { type = EventType.MouseDown, button = 0, clickCount = 1, mousePosition = MobileClientPresentation.SafeAreaGui.center };
        // The original picker chain always gets first refusal, including split/lock/share.
        if (SlotPickers(command, live, live.pet.HasValue)) { ClearMobileSelection(); return; }
        // A picker label promises selection. An incompatible row must never fall
        // through to ordinary use, equipment, trade or another primary action.
        if (action == 0 && (EquipFuseDlg.Picking || ToolTarget.Active || PetForgeDlg.Picking ||
            AttrWashDlg.Picking || AvatarFuse.Active || RepairDlg.Picking))
        { ClearMobileSelection(); return; }
        if (action == 0 && DealDlg.Visible)
        { DealDlg.ChangeBagOffer((byte)tab, live.bagIdx); ClearMobileSelection(); return; }
        if (action == 0)
        {
            command.button = 1;
            bool consumed;
            SlotRightButton(command, live, live.pet.HasValue, out consumed);
            if (!consumed) SlotActivate(command, live, live.pet.HasValue, !live.pet.HasValue || live.pet.Value.opened != 0, StallConfigDlg.Active, live.bagIdx);
        }
        else if (action == 1) HandleLockForIndependentRuntime(tab, live, (message, colour) => ChatUI.Add("系统", message, colour));
        else if (action == 2 && !live.pet.HasValue && live.count > 1)
        { command.modifiers = EventModifiers.Shift; SlotLeftButton(command, live, false); }
        else if (action == 3)
        { command.modifiers = EventModifiers.Control; TryShareBagClickForIndependentRuntime(command, (byte)tab, live, ShareToChat); }
        ClearMobileSelection();
    }
}
}

using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class EquipDlg
{
    internal bool MobileOverlayContains(Vector2 point) => open &&
        mobileEquipmentActions && mobileEquipmentActionsRect.Contains(point);

    private readonly MobilePanelTouch mobileEquipmentTouch = new MobilePanelTouch();
    private int mobileEquipmentSlot = -1, mobileEquipmentId, mobileEquipmentTab = -1, mobileEquipmentPage;
    private bool mobileEquipmentActions, mobileEquipmentBinding;
    private Rect mobileEquipmentSafe, mobileEquipmentActionsRect;

    private void ClearMobileEquipmentSelection()
    {
        mobileEquipmentSlot = -1; mobileEquipmentId = 0;
        mobileEquipmentActions = mobileEquipmentBinding = false;
        mobileEquipmentTouch.Reset();
    }
    private void OnApplicationFocus(bool focused) { if (!focused) ClearMobileEquipmentSelection(); }
    private void OnApplicationPause(bool paused) { if (paused) ClearMobileEquipmentSelection(); }
    private void OnDisable() { ClearMobileEquipmentSelection(); MobileClientPresentation.RemovePanel(this); }
    private void PrepareMobileEquipment()
    {
        Rect safe = MobileClientPresentation.SafeAreaGui;
        if (safe != mobileEquipmentSafe || tab != mobileEquipmentTab)
        { ClearMobileEquipmentSelection(); mobileEquipmentSafe = safe; mobileEquipmentTab = tab; }
        // Include original vertical tabs and the title/task flyouts without rearranging them.
        float right = taskOpen && titleOpen ? 510f : titleOpen ? 382f : 287f;
        Rect panel = MobilePanelLayout.Fit(new Rect(-26, 0, right + 26, 506), safe, ref s, ref winX, ref winY);
        MobileClientPresentation.RegisterPanel(this, panel);
    }
    private int MobileEquipmentHit(Vector2 point)
    {
        if (UITip.MobileContainsPointer(point) || (mobileEquipmentActions && mobileEquipmentActionsRect.Contains(point))) return -1;
        Vector2[] positions = SlotPositions();
        for (int index = 0; index < positions.Length; index++)
        {
            if (tab != 0 && (1 << index & 0x610) == 0) continue;
            Vector2 at = positions[index];
            if (!new Rect(winX + (at.x - 2) * s, winY + (at.y - 2) * s, 34 * s, 34 * s).Contains(point)) continue;
            int part = SlotPart(index);
            int row = equipped.FindIndex(item => item.slot == part || item.slot == 13 && part == 4);
            return row < 0 ? -1 : equipped[row].slot;
        }
        if (tab == 0 && additionalEquipmentOpen)
            for (int index = 0; index < 12; index++)
                if (new Rect(winX + (77 + index % 3 * 36) * s, winY + (75 + index / 3 * 34) * s, 34 * s, 34 * s).Contains(point))
                    return equipped.Exists(item => item.slot == 14 + index) ? 14 + index : -1;
        return -1;
    }
    private void ReadMobileEquipment(Event ev)
    {
        bool selected = mobileEquipmentTouch.Read(ev, s, MobileEquipmentHit, out int slot, out bool held);
        if (mobileEquipmentTouch.Started && !UITip.MobileContainsPointer(mobileEquipmentTouch.StartPoint) && (!mobileEquipmentActions || !mobileEquipmentActionsRect.Contains(mobileEquipmentTouch.StartPoint)))
        { mobileEquipmentSlot = -1; mobileEquipmentId = 0; mobileEquipmentActions = mobileEquipmentBinding = false; }
        if (selected)
        {
            int index = equipped.FindIndex(item => item.slot == slot);
            if (index >= 0)
            {
                mobileEquipmentSlot = slot; mobileEquipmentId = equipped[index].id; mobileEquipmentActions = held;
                mobileEquipmentActionsRect = MobilePanelLayout.Actions(mobileEquipmentTouch.Anchor, 4, out _);
                PlayerController.SuppressWorldClick();
            }
        }
        if (mobileEquipmentTouch.Canceled) ClearMobileEquipmentSelection();
    }
    private static int MobileEquipmentSkill(EquippedItem item)
    {
        if (item.slot == 11 && CharData.skills != null)
            foreach (var skill in CharData.skills)
                if (skill.Key >= 14001 && skill.Key < 14020 && skill.Value > 0) return skill.Key;
        return 0;
    }
    private void DrawMobileEquipmentSelection(Event ev)
    {
        if (mobileEquipmentSlot < 0 || !open) return;
        int index = equipped.FindIndex(item => item.slot == mobileEquipmentSlot);
        if (index < 0 || equipped[index].id != mobileEquipmentId) { ClearMobileEquipmentSelection(); return; }
        EquippedItem item = equipped[index];
        if (!mobileEquipmentActions)
        {
            Vector2 previous = ev.mousePosition; ev.mousePosition = mobileEquipmentTouch.Anchor;
            DrawSlotAction(item, new Event { type = EventType.Repaint });
            ev.mousePosition = previous;
            return;
        }
        bool picking = AvatarFuse.Active || ToolTarget.Active || AttrWashDlg.Picking || RepairDlg.Picking;
        int skill = MobileEquipmentSkill(item);
        Rect box = MobilePanelLayout.Actions(mobileEquipmentTouch.Anchor, mobileEquipmentBinding ? 9 : 4, out float scale);
        mobileEquipmentActionsRect = box;
        int depth = GUI.depth; GUI.depth = 0;
        if (mobileEquipmentBinding)
        {
            int slot = MobilePanelLayout.HotbarSlot(box, scale, ref mobileEquipmentPage, out bool cancel);
            if (slot >= 0 && skill > 0 && !picking)
            { GameHud.BindMobileBookSkill(slot, skill, item.icon); ClearMobileEquipmentSelection(); }
            if (cancel) mobileEquipmentBinding = false;
        }
        else
        {
            if (MobilePanelLayout.Action(box, 0, picking ? "选择此装备" : "卸下", scale))
            {
                DrawSlotAction(item, new Event { type = EventType.MouseDown, button = picking ? 0 : 1 });
                ClearMobileEquipmentSelection();
            }
            if (MobilePanelLayout.Action(box, 1, "分享", scale, !picking))
            {
                DrawSlotAction(item, new Event { type = EventType.MouseDown, button = 0, modifiers = EventModifiers.Control });
                ClearMobileEquipmentSelection();
            }
            if (MobilePanelLayout.Action(box, 2, "放入快捷栏", scale, !picking && skill > 0))
            { mobileEquipmentBinding = true; mobileEquipmentPage = 0; }
            if (MobilePanelLayout.Action(box, 3, "取消", scale)) ClearMobileEquipmentSelection();
        }
        if (box.Contains(ev.mousePosition) && (ev.type == EventType.MouseDown || ev.type == EventType.MouseUp || ev.type == EventType.MouseDrag)) ev.Use();
        GUI.depth = depth;
    }
}
}

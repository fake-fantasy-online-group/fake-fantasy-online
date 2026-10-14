using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    private readonly MobileItemGesture mobileGesture = new MobileItemGesture();
    private int mobileSlot = -1, mobileId, mobilePage, mobileDetailRow, mobileTouchFrame = -1;
    private Rect mobileGrid, mobileViewport;
    private int mobileViewportWidth, mobileViewportHeight;
    private bool mobileSuppressRelease;
    private readonly Rect[] mobileCells = new Rect[20];

    internal static void ClearMobileSelectionForWorldChange()
    {
        if (I == null) return;
        I.ClearMobileSelection();
        MobileClientPresentation.RemovePanel(I);
    }
    private void ClearMobileSelection()
    {
        mobileSlot = -1; mobileId = 0; mobileDetailRow = 0;
        // A receipt must invalidate its active contact without swallowing a future tap.
        if (mobileGesture.Pointer != -1) mobileSuppressRelease = true;
        mobileGesture.Reset();
    }
    private void SelectMobileSlot(int slot)
    {
        Item item = ItemAt(tab, slot);
        if (item.id <= 0) return;
        mobileSlot = slot; mobileId = item.id; mobileDetailRow = 0;
        PlayerController.SuppressWorldClick();
    }
    private void OnApplicationFocus(bool focused)
    { if (!focused) ClearMobileSelectionForWorldChange(); }
    private void OnApplicationPause(bool paused)
    { if (paused) ClearMobileSelectionForWorldChange(); }
    private void OnDisable() { ClearMobileSelectionForWorldChange(); }
    private int MobileHit(Vector2 point)
    {
        for (int i = 0; i < mobileCells.Length; i++)
            if (mobileCells[i].Contains(point)) return mobilePage * 20 + i;
        return -1;
    }
    private void MobilePointer(int id, Vector2 point, bool begin, bool end, bool cancel)
    {
        float threshold = Mathf.Max(12f, 12f * s);
        if (begin)
        {
            int slot = MobileHit(point);
            if (slot >= 0 && mobileGesture.Pointer == -1)
            { mobileGesture.Begin(id, slot, point.x, point.y, Time.unscaledTime); mobileSuppressRelease = true; }
        }
        int selected = mobileGesture.Slot;
        if (mobileGesture.Sample(id, point.x, point.y, Time.unscaledTime, threshold, end, cancel)) SelectMobileSlot(selected);
    }
    private void ReadMobilePointer(Event ev)
    {
        // A platform can drop a touch without reporting Ended/Canceled (interruptions).
        // Detect that before accepting new fingers, so an old owner cannot block the next tap.
        bool ownerPresent = false;
        foreach (Touch touch in Input.touches)
            if (touch.fingerId == mobileGesture.Pointer) { ownerPresent = true; break; }
        if (mobileGesture.CancelMissingOwner(ownerPresent))
        { mobileSuppressRelease = true; ClearMobileSelection(); }
        if (Input.touchCount > 0)
        {
            if (mobileTouchFrame != Time.frameCount)
            {
                mobileTouchFrame = Time.frameCount;
                foreach (Touch touch in Input.touches)
                    MobilePointer(touch.fingerId, new Vector2(touch.position.x, Screen.height - touch.position.y),
                        touch.phase == TouchPhase.Began, touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled,
                        touch.phase == TouchPhase.Canceled);
            }
        }
        else if (!UnityEngine.Application.isMobilePlatform && ev.button == 0 && (ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag || ev.type == EventType.MouseUp))
        {
            // Editor preview uses the identical ownership/cancellation state machine.
            MobilePointer(-2, ev.mousePosition, ev.type == EventType.MouseDown, ev.type == EventType.MouseUp, false);
        }
        else if (mobileGesture.Pointer == -2 && ev.type == EventType.Repaint)
            MobilePointer(-2, ev.mousePosition, false, false, false);
        if (mobileSuppressRelease && ev.type == EventType.MouseUp)
        { mobileSuppressRelease = false; ev.Use(); }
        else if (mobileGesture.Pointer != -1 && (ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag)) ev.Use();
    }
    private bool MobileButton(Rect rect, string text, bool enabled = true)
    {
        bool old = GUI.enabled;
        GUI.enabled = old && enabled;
        ArtworkSkin.ButtonFace(rect, text, s, GUI.enabled, rect.Contains(Event.current.mousePosition), pressed: false);
        bool result = GUI.Button(rect, GUIContent.none, GUIStyle.none);
        GUI.enabled = old;
        return result;
    }
    private void DrawMobileInventory(Event ev)
    {
        s = Mathf.Max(1f, MobileClientPresentation.Scale);
        Rect safe = MobileClientPresentation.SafeAreaGui;
        if (safe != mobileViewport || Screen.width != mobileViewportWidth || Screen.height != mobileViewportHeight)
        {
            ClearMobileSelection();
            mobileViewport = safe; mobileViewportWidth = Screen.width; mobileViewportHeight = Screen.height;
        }
        Rect panel = new Rect(safe.x + 8 * s, safe.y + 8 * s, safe.width - 16 * s, safe.height - 16 * s);
        MobileClientPresentation.RegisterPanel(this, panel);
        var frame = WinZ.Frame(this, panel, 1);
        GUI.depth = frame.Item1;
        if (frame.Item2) { ClearMobileSelection(); return; }
        bool wide = panel.width >= panel.height * 1.2f;
        float header = 48 * s, footer = 48 * s;
        mobileGrid = new Rect(panel.x, panel.y + header * 2, wide ? panel.width * .55f : panel.width,
            panel.height - header * 2 - footer);
        float cellW = mobileGrid.width / 5, cellH = mobileGrid.height / 4;
        for (int i = 0; i < 20; i++)
            mobileCells[i] = (!wide && mobileSlot >= 0) ? new Rect(-9999, -9999, 0, 0) :
                new Rect(mobileGrid.x + i % 5 * cellW, mobileGrid.y + i / 5 * cellH, cellW, cellH);
        if (splitOpen)
        {
            GUI.DrawTexture(panel, bk);
            DrawMobileSplit(panel);
            if (ev.type == EventType.MouseDown || ev.type == EventType.MouseUp) ev.Use();
            return;
        }
        ReadMobilePointer(ev);
        GUI.DrawTexture(panel, bk);
        if (MobileButton(new Rect(panel.xMax - 88*s, panel.y, 88*s, header), "关闭"))
        { open = false; ClearMobileSelection(); MobileClientPresentation.RemovePanel(this); return; }
        if (MobileButton(new Rect(panel.x, panel.y, 96*s, header), "整理"))
        { ClearMobileSelection(); heldSlot = heldWhSlot = heldBarPet = -1; NetClient.I?.SortBagForIndependentRuntime(tab); }
        string[] tabs = { "药品", "材料", "装备", "特殊" };
        for (int i = 0; i < 4; i++)
            if (MobileButton(new Rect(panel.x + i * panel.width / 4, panel.y + header, panel.width / 4, header),
                (tab == i ? "● " : "") + tabs[i]))
            { tab = i; mobilePage = 0; ClearMobileSelection(); }
        if (wide || mobileSlot < 0)
        {
            for (int i = 0; i < 20; i++)
            {
                Rect cell = mobileCells[i];
                Rect icon = new Rect(cell.x + 4*s, cell.y + 4*s, Mathf.Min(cell.width - 8*s, cell.height - 8*s), Mathf.Min(cell.width - 8*s, cell.height - 8*s));
                if (slotTex != null) GUI.DrawTexture(icon, slotTex);
                Item item = ItemAt(tab, mobilePage * 20 + i);
                if (item.id <= 0) continue;
                Color iconColour = GUI.color;
                if (DealDlg.IsOffered((byte)tab, item.bagIdx)) GUI.color = new Color(.42f, .42f, .42f, 1);
                if (item.icon != null) ItemIconUI.Draw(icon, item.icon, s);
                else if (BitmapFont.Ready) BitmapFont.Draw(icon.x, icon.y, item.name ?? "物品", Color.white, s);
                GUI.color = iconColour;
                if (item.blocked != 0 || (item.maxDur > 0 && item.curDur < 1))
                { GUI.color = ItemFocusBkColor; GUI.DrawTexture(icon, FocusBk != null ? FocusBk : Texture2D.whiteTexture); GUI.color = iconColour; }
                if (!item.pet.HasValue) GameHud.DrawCdOverlay(icon, item.id);
                if (item.count > 1) GameHud.DrawCountBadge(icon, item.count.ToString(), Color.white, s);
                if (item.locked) DrawLockBadge(icon, s);
                if (item.bagIdx == mobileSlot)
                { Color old = GUI.color; GUI.color = new Color(1, 1, 1, .25f); GUI.DrawTexture(icon, Texture2D.whiteTexture); GUI.color = old; }
            }
            float navY = panel.yMax - footer;
            if (MobileButton(new Rect(panel.x, navY, mobileGrid.width / 2, footer), "上一页 " + (mobilePage + 1) + "/5", mobilePage > 0))
            { mobilePage--; ClearMobileSelection(); }
            if (MobileButton(new Rect(panel.x + mobileGrid.width / 2, navY, mobileGrid.width / 2, footer), "下一页", mobilePage < 4))
            { mobilePage++; ClearMobileSelection(); }
        }
        if (mobileSlot >= 0)
        {
            Item item = ItemAt(tab, mobileSlot);
            if (item.id != mobileId) { ClearMobileSelection(); return; }
            Rect details = new Rect(wide ? mobileGrid.xMax + 8*s : panel.x, mobileGrid.y,
                wide ? panel.xMax - mobileGrid.xMax - 8*s : panel.width, panel.height - header * 2);
            DrawMobileDetails(details, item, wide);
        }
        if (splitOpen) DrawMobileSplit(panel);
        if (panel.Contains(ev.mousePosition) && (ev.type == EventType.MouseDown || ev.type == EventType.MouseUp || ev.type == EventType.MouseDrag))
        { PlayerController.SuppressWorldClick(); ev.Use(); }
    }
    private void DrawMobileDetails(Rect rect, Item item, bool wide)
    {
        float h = 44*s;
        if (MobileButton(new Rect(rect.x, rect.y, rect.width, h), wide ? "收起详情" : "返回背包")) { ClearMobileSelection(); return; }
        string text = TooltipContent.Fmt(TooltipContent.QualityName(item.name, item.quality), item.desc);
        text = EndgamePanel.BagTooltip(tab, item, text);
        if (item.pet.HasValue)
        {
            text = "";
            foreach (var line in PetInfoTooltip.BuildLines(item.pet.Value)) text += line.Item1.Replace("双击可孵化", "点击孵化按钮可孵化").Replace("Shift+右键 解锁", "点击解锁按钮") + "\n";
        }
        if (!item.pet.HasValue && item.slot >= 0 && !item.cosmetic)
            foreach (EquipDlg.EquippedItem equipped in EquipDlg.Equipped)
            {
                if (equipped.id <= 0 || (equipped.slot != item.slot && !(IsWeap(item.slot) && IsWeap(equipped.slot)))) continue;
                text += "\n（当前穿戴）\n" + EndgamePanel.EquippedTooltip(equipped,
                    TooltipContent.Fmt(TooltipContent.QualityName(equipped.name, equipped.quality), equipped.desc));
                break;
            }
        if (item.locked) text += "\n【已加锁】";
        bool offered = DealDlg.IsOffered((byte)tab, item.bagIdx);
        if (offered) text += "\n【交易中】请先从交易窗撤回";
        UITip.DrawMobilePage(new Rect(rect.x, rect.y+h, rect.width, Mathf.Max(h, rect.height-4*h)), text, s, ref mobileDetailRow);
        bool available = !offered && !splitOpen && !HasHeldAny;
        float y = rect.yMax - 2*h;
        if (MobileButton(new Rect(rect.x, y, rect.width/2, h), MobilePrimaryLabel(item), available)) ExecuteMobileAction(item, 0);
        if (MobileButton(new Rect(rect.x+rect.width/2, y, rect.width/2, h), item.locked ? "解锁" : "加锁", available)) ExecuteMobileAction(item, 1);
        if (MobileButton(new Rect(rect.x, y+h, rect.width/2, h), "拆分", available && !item.pet.HasValue && item.count > 1)) ExecuteMobileAction(item, 2);
        if (MobileButton(new Rect(rect.x+rect.width/2, y+h, rect.width/2, h), "分享", available)) ExecuteMobileAction(item, 3);
    }
    private void DrawMobileSplit(Rect panel)
    {
        float width = Mathf.Min(panel.width, 360*s), h = 48*s;
        Rect box = new Rect(panel.center.x-width/2, panel.center.y-2*h, width, 4*h);
        if (splitBk != null) GUI.DrawTexture(box, splitBk);
        if (BitmapFont.Ready) BitmapFont.Draw(box.x+12*s, box.y+12*s,
            "拆分数量 " + splitVal + " / " + splitMax, Color.white, s);
        if (MobileButton(new Rect(box.x, box.y+h, width/2, h), "－")) SetSplitVal(splitVal-1);
        if (MobileButton(new Rect(box.x+width/2, box.y+h, width/2, h), "＋")) SetSplitVal(splitVal+1);
        if (MobileButton(new Rect(box.x, box.y+2*h, width/2, h), "最少")) SetSplitVal(1);
        if (MobileButton(new Rect(box.x+width/2, box.y+2*h, width/2, h), "最多")) SetSplitVal(splitMax);
        if (MobileButton(new Rect(box.x, box.y+3*h, width/2, h), "取消")) CloseSplit();
        if (MobileButton(new Rect(box.x+width/2, box.y+3*h, width/2, h), "确认拆分")) CommitSplit();
    }
    private string MobilePrimaryLabel(Item item)
    {
        if (EquipFuseDlg.Picking || ToolTarget.Active || PetForgeDlg.Picking || AttrWashDlg.Picking || AvatarFuse.Active || RepairDlg.Picking) return "选择此物品";
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
        if (live.id != mobileId || live.id != item.id || HasHeldAny || splitOpen || DealDlg.IsOffered((byte)tab, live.bagIdx)) return;
        var command = new Event { type = EventType.MouseDown, button = 0, clickCount = 1, mousePosition = MobileClientPresentation.SafeAreaGui.center };
        // The original picker chain always gets first refusal, including split/lock/share.
        if (SlotPickers(command, live, live.pet.HasValue)) { ClearMobileSelection(); return; }
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

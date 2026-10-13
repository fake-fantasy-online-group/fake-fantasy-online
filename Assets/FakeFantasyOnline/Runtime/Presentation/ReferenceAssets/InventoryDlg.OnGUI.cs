using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    // Official instance layout +0x24 / +0x30 / +0x34. Unity is the sole drawing driver.
    public static System.Action<int, int> OnUseItem; // Official static +0x28 fallback.
    private int tab;                    // +0x24
    private bool dragging;              // +0x30
    private Vector2 dragOff;            // +0x34

    // Official InventoryDlg.Item layout +0x20 / +0x24, needed by the slot painter focus test, and
    // +0x28, which the picker/offer chain passes on to every picker as "has an avatar".
    public partial struct Item
    {
        public int curDur;              // +0x20
        public int maxDur;              // +0x24
        public int avatar;              // +0x28
        public int slot;                // +0x30, equipment category (not bagIdx)
    }

    // Official 0x06000527, instance System.Void InventoryDlg.OnGUI().
    //
    // Restored from the arm64 body (rva 0x45c98c, 23788 bytes) and the x86_64 body (0x473df0,
    // 25840 bytes) against the decoded region map in
    // docs/contracts/VERSION_UPGRADE_1_5_8_INVENTORY_ONGGUI_V1.md. The map-scoped adapter
    // only borrows observations; it neither disables nor manually calls this entry.
    private void OnGUI()
    {
        DragCursorOverlay.Ensure();
        if (Event.current.type == EventType.Repaint) TickLockWatchdog();
        if (!open) return;
        if (!loaded) Load();
        if (bk == null) return;
        s = UIScale.Factor;
        if (!placed)
        {
            winX = Screen.width + s * -443f;
            winY = (Screen.height + s * -501f) * 0.5f;
            placed = true;
        }

        // A window covered by a higher one still draws, but it must not answer the mouse: the official
        // body parks the mouse at (-9999, -9999) instead of skipping the event.
        Event ev = Event.current;
        int depth = splitOpen ? 1 : 10;
        var frame = WinZ.Frame(this, new Rect(winX, winY, s * 423f, s * 501f), depth);
        GUI.depth = frame.Item1;
        Vector2 previousMouse = ev.mousePosition;
        bool covered = frame.Item2;
        // The original finally begins after the pointer was successfully parked.
        if (covered) ev.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            DrawOpenWindow(ev);
        }
        finally
        {
            if (covered) ev.mousePosition = previousMouse;
        }
    }

    private void DrawOpenWindow(Event ev)
    {
        if (splitOpen) SplitInput(ev);
        Vector2 hit = ev.mousePosition;
        // DisplayClass132_0 captures the origin after SplitInput and before drag writes.
        float X = winX, Y = winY;
        Rect helpRect = new Rect(X + s * 377f, Y + s * 17f, s * 20f, s * 19f);
        Rect closeRect = new Rect(X + s * 398f, Y + s * 17f, s * 20f, s * 19f);

        // Native title strip is 423x40 and excludes both title buttons; its MouseDown
        // gate does not restrict the mouse button.
        if (ev.type == EventType.MouseDown
            && new Rect(X, Y, s * 423f, s * 40f).Contains(hit)
            && !helpRect.Contains(hit) && !closeRect.Contains(hit))
        {
            dragging = true;
            dragOff = hit - new Vector2(X, Y);
            ev.Use();
        }

        if (dragging)
        {
            if (ev.type == EventType.MouseDrag)
            {
                winX = hit.x - dragOff.x;
                winY = hit.y - dragOff.y;
                ev.Use();
            }
        }
        if (ev.type == EventType.MouseUp) dragging = false;

        GUI.DrawTexture(new Rect(X, Y, s * 423f, s * 501f), bk);
        Btn(helpRect, helpBtn, ev);
        bool closed = Btn(closeRect, closeBtn, ev);
        if (closed)
        {
            open = false;
            if (heldSlot >= 0 || heldWhSlot >= 0 || heldBarPet >= 0)
            {
                heldSlot = -1;
                heldWhSlot = -1;
                heldBarPet = -1;
                heldBound = false;
            }
            PlayerController.SuppressWorldClick();
            return;
        }
        {
            // --- four-page tab strip: design y 40, x 15, 50x23 each, frame 1 when selected ---
            int index = 0;
            float x = 15f;
            do
            {
                Texture2D[] frames = tabBtn[index];
                Texture2D tex = frames[index == tab ? 1 : 0] ?? frames[0];
                if (tex != null)
                    GUI.DrawTexture(new Rect(X + s * x, Y + s * 40f, s * 50f, s * 23f), tex);
                if (GUI.Button(new Rect(X + s * x, Y + s * 40f, s * 50f, s * 23f), GUIContent.none, GUIStyle.none))
                    tab = index;
                index++;
                x += 50f;
            }
            while (index != 4);
        }

        // --- sort button: design (365, 41) sized 48x21 ---
        Rect sort = new Rect(X + s * 365f, Y + s * 41f, s * 48f, s * 21f);
        bool sortHover = sort.Contains(ev.mousePosition);
        // Keep the native hit box and click timing; only the blank artwork is nine-sliced.
        ArtworkSkin.ButtonFace(sort, "整理", s, GUI.enabled, sortHover,
            pressed: sortHover && Input.GetMouseButton(0));
        if (GUI.Button(sort, GUIContent.none, GUIStyle.none))
        {
            // Native clears all three carried indices even when no client is available.
            // Unlike the close branch it does not reset heldBound.
            heldSlot = -1;
            heldWhSlot = -1;
            heldBarPet = -1;
            if (NetClient.I != null) NetClient.I.SortBagForIndependentRuntime(tab);
        }

        // --- bag page: 10x10, pitch 40/40, first slot at design (14, 66), slot box 36x36 ---
        Color previous = GUI.color;
        GUI.color = new Color(20f / 255f, 18f / 255f, 15f / 255f, 1f);
        GUI.DrawTexture(new Rect(X + s * 12f, Y + s * 64f, s * 400f, s * 402f), Texture2D.whiteTexture);
        GUI.color = previous;

        List<Item> page = Items[tab];
        for (int i = 0; i < Slots; i++)
        {
            float x = X + s * ((i % Cols) * 40f + 14f);
            float y = Y + s * ((i / Cols) * 40f + GridTop);
            float size = s * 36f;
            Rect cell = new Rect(x, y, size, size);
            if (slotTex != null) GUI.DrawTexture(cell, slotTex);

            bool over = SlotHit(cell, ev.mousePosition);
            if (over)
            {
                previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.18f);
                GUI.DrawTexture(cell, Texture2D.whiteTexture);
                GUI.color = previous;
            }
            int row = page.FindIndex(item => SlotOwnsIndex(item, i));
            bool offered = DealDlg.IsOffered((byte)tab, i);
            if (over) PlaceHeldBagForIndependentRuntime(ev, tab, i, offered);
            if (row < 0) continue;
            Item it = page[row];

            bool isPet = it.pet.HasValue;

            // Two discriminators that an earlier pass had to leave open are now read, and both
            // architectures agree on them.
            //
            // arm64 bVar7 / x86_64 local_39 is initialised to true and overwritten only when the row
            // carries a pet, where it becomes pet.Value.opened != 0 (PetBrief.opened is the
            // System.Byte at +0x22, read as a bool). So it is "this row is not an unhatched egg".
            bool hatched = !isPet || it.pet.Value.opened != 0;
            // arm64 uVar10 / x86_64 bVar8, computed once per row just before the painter:
            // DealDlg.IsOffered(tab, bagIdx), i.e. the row is on the table in a trade window.
            // arm64 bVar6 / x86_64 bVar24: true unless this row is exactly the one being carried,
            // which is the opposite polarity from the earlier guess in this file.
            bool notCarried = heldSlot != it.bagIdx || tab != HeldTab;

            if (notCarried)
            {
                // (0.42, 0.42, 0.42, 1) = 0x3ed70a3d, 0x3ed70a3d, 0x3ed70a3d, 0x3f800000.
                if (offered) GUI.color = new Color(0.42f, 0.42f, 0.42f, 1f);
                // (0.6, 0.85, 1, 1) = 0x3f19999a, 0x3f59999a, 0x3f800000, 0x3f800000.
                else if (!hatched) GUI.color = new Color(0.6f, 0.85f, 1f, 1f);

                float inset = cell.width * 4f / 36f;
                Rect iconRect = new Rect(cell.x + inset, cell.y + inset,
                    cell.width - inset * 2f, cell.height - inset * 2f);
                if (it.icon == null)
                {
                    if (BitmapFont.Ready)
                    {
                        string label;
                        Color colour;
                        if (!isPet)
                        {
                            string name = it.name;
                            label = name.Substring(0, Mathf.Min(2, name.Length));
                            // The native body divides the quality Color32 components by 255, which is what
                            // the Color32 -> Color conversion already does, so the conversion is the
                            // faithful expression here and no second division is applied.
                            // Raw ARM64 sets all three RGB components to 0.5 for an offered row.
                            colour = offered ? new Color(0.5f, 0.5f, 0.5f, 1f)
                                : it.quality < 1 ? Color.white
                                : (Color)UITip.QualityColor(it.quality);
                        }
                        else
                        {
                            // Usage 18271 魂 when the egg has been opened, usage 17972 蛋 otherwise.
                            label = hatched ? "魂" : "蛋";
                            colour = offered ? new Color(0.5f, 0.5f, 0.5f, 1f) : Color.white;
                        }
                        BitmapFont.Draw(x + s * 5f, y + s * 11f, label, colour, s);
                    }
                }
                else
                {
                    ItemIconUI.Draw(iconRect, it.icon, s);
                }

                GUI.color = Color.white;
                if (it.blocked != 0 || (it.maxDur > 0 && it.curDur < 1))
                {
                    Texture2D focus = FocusBk;
                    GUI.color = ItemFocusBkColor;
                    GUI.DrawTexture(iconRect, focus != null ? focus : Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                if (!isPet) GameHud.DrawCdOverlay(iconRect, it.id);
                if (it.count > 1)
                    GameHud.DrawCountBadge(cell, it.count.ToString(), Color.white, s);
                if (it.locked) DrawLockBadge(cell, s);
                GameHud.DrawWashScrollTag(cell, it.id, s);
            }

            // Tooltip admission also gates row input: offered items and modal selection
            // states must never fall through to picking, sharing or activation.
            if (!over || splitOpen || heldBarPet >= 0) continue;
            if (offered)
            {
                string text;
                if (isPet)
                {
                    PetBrief pet = it.pet.Value;
                    text = TooltipContent.Fmt(string.IsNullOrEmpty(pet.nickname) ? pet.species : pet.nickname, "");
                }
                else text = TooltipContent.Fmt(TooltipContent.QualityName(it.name, it.quality), it.desc);
                UITip.Show(text + "\n【交易中】已放入交易窗, 结束前不能移动/使用。撤回请点交易窗里的它", s);
                if (ev.type == EventType.MouseDown) ev.Use();
                continue;
            }
            if (notCarried) ShowSlotTooltip(it, ev.mousePosition, hatched);
            SlotInput(ev, it, isPet, hatched, i);
        }

        // --- bottom row: three coins, the status plate, 彩玉 and the weight label ---
        var cursor = new DrawCursor { X = X, Y = Y, coinX = 18f };
        long money = Money;
        long gold = money / 1000000L;
        CoinFlow(icoGold, gold, ref cursor);
        CoinFlow(icoSilver, (money - gold * 1000000L) / 1000L, ref cursor);
        CoinFlow(icoCopper, money % 1000L, ref cursor);

        if (BitmapFont.Ready)
        {
            float plateX = X + s * 226f;
            float plateY = Y + s * 475f;
            if (bk == null)
            {
                previous = GUI.color;
                GUI.color = new Color(17f / 255f, 17f / 255f, 17f / 255f, 1f);
                GUI.DrawTexture(new Rect(plateX, plateY, s * 190f, s * 22f), Texture2D.whiteTexture);
                GUI.color = previous;
            }
            else
            {
                GUI.DrawTextureWithTexCoords(new Rect(plateX, plateY, s * 190f, s * 22f), bk,
                    new Rect(0.052f, 0.008f, 0.4492f, 0.0439f));
            }
            string weight = "负重 " + Weight.ToString() + "/" + MaxWeight.ToString();
            float dim = (MaxWeight < 1 || Weight <= MaxWeight) ? 1f : 80f / 255f;
            float width = BitmapFont.Measure(weight, s);
            BitmapFont.Draw(X + s * 408f - width, Y + s * CoinY, weight,
                new Color(1f, dim, dim, 1f), s);
            BitmapFont.Draw(X + s * 232f, Y + s * CoinY, "彩玉 " + CaiYu.ToString(),
                new Color(0.55f, 0.92f, 0.85f, 1f), s);
        }

        // The full native held/drop and split tail is connected after the bottom row.
        HeldClickForIndependentRuntime(new Rect(X, Y, s * 423f, s * 501f), ev);
        if (splitOpen) SplitDraw();
    }

    // Official OnGUI hover branch. Preserve collection order: the first compatible
    // equipped slot supplies comparison text, including the 4/13 weapon pairing.
    private void ShowSlotTooltip(Item it, Vector2 mouse, bool hatched)
    {
        if (it.pet.HasValue)
        {
            PetInfoTooltip.Show(it.pet.Value, mouse, s, hatched ? "双击装备进宠物栏" : null);
            return;
        }
        string text = TooltipContent.Fmt(TooltipContent.QualityName(it.name, it.quality), it.desc);
        text = EndgamePanel.BagTooltip(tab, it, text);
        if (it.locked) text += "\n【已加锁】不能卖店/交易/摆摊/邮寄/丢弃 (Shift+右键 解锁)";
        string comparison = null;
        if (it.slot >= 0 && !it.cosmetic)
        {
            foreach (EquipDlg.EquippedItem equipped in EquipDlg.Equipped)
            {
                if (equipped.id <= 0) continue;
                if (equipped.slot != it.slot && !(IsWeap(it.slot) && IsWeap(equipped.slot))) continue;
                comparison = "\u0007\u0006（当前穿戴）\n" + EndgamePanel.EquippedTooltip(equipped, TooltipContent.Fmt(
                    TooltipContent.QualityName(equipped.name, equipped.quality), equipped.desc));
                break;
            }
        }
        if (comparison == null) UITip.Show(text, s);
        else UITip.ShowCompare(text, comparison, s);
    }

    // The shared row hit test, arm64 C lines 863-926. A row is live only when the pointer is inside
    // the cell and three windows that also consume the pointer are not over it.
    private bool SlotHit(Rect cell, Vector2 point)
    {
        if (!cell.Contains(point)) return false;
        if (Win05.I != null && Win05.I.MouseOver(point, s)) return false;
        if (AttrWashDlg.MouseOver()) return false;
        if (PetForgeDlg.MouseOver()) return false;
        return true;
    }

    // The row input chain, arm64 C lines 1626-2549, cross-checked against x86_64.
    //
    // The native body computes two flags first: bVar5, the double click, and bVar6, "this row is in
    // the right-button stage" (MouseDown, button 1 and nothing held). Then
    //   if (!bVar6 && !bVar5) { share / split / pick-up; the row always ends here; }
    //   activation tail, reached only when bVar6 or bVar5 holds,
    // which is why the right-button stage that consumed nothing still falls into activation: a right
    // click with an empty hand activates the row, exactly as InventoryDlg.CanRightActivate-
    // ForIndependentRuntime already expresses for the migrated inventory owner.
    private void SlotInput(Event ev, Item it, bool isPet, bool hatched, int gridIndex)
    {
        if (SlotPickers(ev, it, isPet)) return;

        bool doubleClick = ev != null && ev.type == EventType.MouseDown
            && ev.button == 0 && ev.clickCount == 2;

        bool consumed;
        if (SlotRightButton(ev, it, isPet, out consumed)) { if (consumed) return; }
        else if (!doubleClick) { SlotLeftButton(ev, it, isPet); return; }

        SlotActivate(ev, it, isPet, hatched, doubleClick, gridIndex);
        // Common native activation exit, including blocked trade/stall and cooldown paths.
        DropBagReferenceForIndependentRuntime();
        if (ev != null) ev.Use();
    }

    // arm64 C lines 1960-2184. Returns true when the right-button stage was entered at all; the out
    // flag says whether one of its three actions fired and consumed the event.
    private bool SlotRightButton(Event ev, Item it, bool isPet, out bool consumed)
    {
        consumed = false;
        if (ev == null || ev.type != EventType.MouseDown || ev.button != 1) return false;
        // heldSlot < 0 && heldWhSlot < 0, the same gate as CanRightActivateForIndependentRuntime.
        if (heldSlot >= 0 || heldWhSlot >= 0) return false;

        // 1. The lock modifier: lock or unlock the row, with the security-code branch.
        //    Display class 132_3 (0x06000541) captures the tab, the bag index and the pet-aware
        //    name; the bodies are 0x06000542 / 0x06000543.
        if (LockModHeld(ev))
        {
            HandleLockForIndependentRuntime(tab, it,
                (text, colour) => ChatUI.Add("系统", text, colour));
            ev.Use();
            consumed = true;
            return true;
        }

        // The live 周天鼎 reserves this bag row locally instead of activating it.
        if (!isPet && EndgamePanel.CubeContainerOpen)
        {
            EndgamePanel.TryPutCubeBagItem(tab, it.bagIdx);
            ev.Use(); consumed = true; return true;
        }

        // 2. A warehouse window that is showing takes the row as a deposit.
        //    WarehouseMove(0, tab, bagIdx, StoreDlg.CurTab, count, -1); the -1 is 0xffffffff.
        if (!isPet && StoreDlg.Visible && NetClient.I != null)
        {
            NetClient.I.WarehouseMoveForIndependentRuntime(0, (byte)tab, it.bagIdx, StoreDlg.CurTab, it.count, -1);
            ev.Use();
            consumed = true;
            return true;
        }

        // 3. The magic wardrobe: ask first, store on confirm. Display class 132_4 (0x06000544),
        //    body 0x06000545. The gate is Win05.IsShowing("magicstoredlg"), metadata usage 15345,
        //    and the question is usages 17338 + name + 16921.
        if (!isPet && Win05.IsShowing("magicstoredlg") && NetClient.I != null)
        {
            byte capturedTab = (byte)tab;
            int capturedSlot = it.bagIdx;
            SysMsgBox.Show("将【" + it.name + "】放入魔法衣橱?\n(取出需消耗1个提取凭证)",
                () => WardrobeStoreConfirmed(capturedTab, capturedSlot), null);
            ev.Use();
            consumed = true;
            return true;
        }

        return true;
    }

    // arm64 C lines 2185-2338. Every stage ends the row when it fires; when none fires the pick-up
    // stage still runs and the row ends unconditionally, which is the unconditional
    // `goto LAB_0045dc24` at arm64 C line 2339.
    private void SlotLeftButton(Event ev, Item it, bool isPet)
    {
        // ShareModifier: Ctrl/Cmd + left click while the hand is empty. 0x06000510 / 0x06000511.
        if (TryShareBagClickForIndependentRuntime(ev, (byte)tab, it, ShareToChat)) return;

        // Shift + left click on a stack of more than one opens the split popup. 0x06000528.
        if (ev != null && ev.type == EventType.MouseDown && ev.button == 0 && heldSlot < 0
            && ShiftHeld(ev) && it.count > 1 && !isPet)
        {
            OpenSplit((byte)tab, it.bagIdx, it.count, it.id, it.icon, ev.mousePosition);
            ev.Use();
            return;
        }

        // Plain left click picks the row up: heldIcon, heldSlot, heldTab, heldName, heldId,
        // heldPet, heldBound, in that order (statics +0x38, +0x30, +0x34, +0x40, +0x48, +0x4c,
        // +0x4d). 0x06000522.
        PickUpBagForIndependentRuntime(ev, (byte)tab, it, null);
    }

    // arm64 C lines 2341-2549. `stallActive` is the double click while the stall window is open;
    // the native body computes it as `bVar5 ? StallConfigDlg.Active : false`, so a right click can
    // never reach the stall branch.
    private void SlotActivate(Event ev, Item it, bool isPet, bool hatched, bool doubleClick, int gridIndex)
    {
        bool stallActive = doubleClick && StallConfigDlg.Active;

        if (stallActive && it.locked)
        {
            // Usage 18039, colour (1, 180/255, 120/255, 1) = 0x3f800000, 0x3f34b4b5, 0x3ef0f0f1.
            ChatUI.Add("系统", "该物品已加锁, 无法摆摊出售。Shift+右键 可解锁",
                new Color(1f, 180f / 255f, 120f / 255f, 1f));
            ev.Use();
            return;
        }

        if (stallActive)
        {
            // A buy stall has no count to offer, so 9999 stands in for it.
            StallDetailDlg.Open((byte)tab, it.bagIdx, it.id,
                StallConfigDlg.IsSell ? it.count : 9999,
                it.name, it.icon, it.sell, it.quality, it.desc);
            ev.Use();
            return;
        }

        // Usage 17000, colour (1, 200/255, 120/255, 1) = 0x3f800000, 0x3f48c8c9, 0x3ef0f0f1.
        if (SocialClient.TradeActive)
        {
            ChatUI.Add("系统", "交易中不能使用/装备背包物品(防手滑)。把它【拖】进交易窗即可",
                new Color(1f, 200f / 255f, 120f / 255f, 1f));
            ev.Use();
            return;
        }

        if (isPet && NetClient.I != null)
        {
            // An unhatched egg is hatched, an opened pet is deployed; 0x0600073a / 0x06000739.
            if (!hatched) NetClient.I.PetHatchForIndependentRuntime(it.bagIdx);
            else NetClient.I.PetDeployForIndependentRuntime(it.bagIdx);
            return;
        }

        ActivateOrdinaryItem(ref tab, it, gridIndex);
    }

    // Existing explicit-item fixture callers have no separate grid-loop cell.
    private void ActivateOrdinaryItem(ref int bagTab, Item it) =>
        ActivateOrdinaryItem(ref bagTab, it, it.bagIdx);

    // Shared source suffix of 06000527. Keep the actual tab field live across Handle/CdFrac.
    // The original no-client callback uses the captured loop cell; network sends use row.bagIdx.
    private void ActivateOrdinaryItem(ref int bagTab, Item it, int gridIndex)
    {
        System.Action equip = bagTab == 2 && NetClient.I != null
            ? (System.Action)(() => EquipHeld(it)) : null;
        if (!ItemActions.Handle(it.id, (byte)bagTab, it.bagIdx, it.bound, it.cosmetic, it.name, equip))
        {
            if (NetClient.I == null) OnUseItem?.Invoke(bagTab, gridIndex);
            else if (GameHud.CdFrac(it.id) <= 0f)
            {
                NetClient.I.UseItem(it.id, (byte)bagTab, it.bagIdx);
                GameHud.StartItemCd(it.id);
            }
        }
    }

    // Official picker/offer chain, arm64 C lines 1626-1931, cross-checked line by line against
    // x86_64 C lines 1636-1910. Every branch is guarded by a picking static plus, where the native
    // has it, tab == 2 and !isPet, and every one ends with Event.Use().
    //
    // The native body does not return after Use(); it falls into the double-click and right-button
    // stages that follow. Returning true here is equivalent, because Event.Use() makes every later
    // ev.type test false.
    private bool SlotPickers(Event ev, Item it, bool isPet)
    {
        byte bagTab = (byte)tab;

        // EquipFuseDlg is the only picker whose guard also excludes cosmetic rows (Item.cosmetic at
        // +0x36); when its guard holds but the event is not a left MouseDown the native body falls
        // through into the same chain below, which is why this is written as a fall-through test.
        if (EquipFuseDlg.Picking && tab == 2 && !isPet && !it.cosmetic
            && ev != null && ev.type == EventType.MouseDown && ev.button == 0)
        {
            EquipFuseDlg.SetEquip(bagTab, it.bagIdx, it.id, it.quality, it.name, it.icon);
            ev.Use();
            return true;
        }

        // ToolTarget accepts the row when the tool window is active and either the row is on the
        // equipment page or the tool it is waiting for is the one with id 0x6311 (25361).
        if (ToolTarget.Active && (tab == 2 || ToolTarget.ToolId == 0x6311) && !isPet
            && ev != null && ev.type == EventType.MouseDown && ev.button == 0)
        {
            ToolTarget.PickBag(bagTab, it.bagIdx, it.name, it.bound, it.avatar > 0);
            ev.Use();
            return true;
        }

        // A pet row goes to the forge only once it has been opened; an egg cannot be forged.
        if (PetForgeDlg.Picking && isPet && it.pet.Value.opened != 0
            && ev != null && ev.type == EventType.MouseDown && ev.button == 0)
        {
            PetForgeDlg.SetPet(bagTab, it.bagIdx, it.pet.Value, it.icon);
            ev.Use();
            return true;
        }

        // Pet-forge materials: kind 0 talisman, 1 book, 2 pot, which is IsPot ? 2 : (IsTalisman ? 0 : 1).
        if (!isPet && PetForgeDlg.Picking
            && (PetForgeIds.IsTalisman(it.id) || PetForgeIds.IsBook(it.id) || PetForgeIds.IsPot(it.id))
            && ev != null && ev.type == EventType.MouseDown && ev.button == 0)
        {
            int kind = PetForgeIds.IsPot(it.id) ? 2 : (PetForgeIds.IsTalisman(it.id) ? 0 : 1);
            PetForgeDlg.SetItem(kind, tab, it.bagIdx, it.id, it.name, it.icon);
            ev.Use();
            return true;
        }

        if (AttrWashDlg.Picking && tab == 2 && !isPet
            && ev != null && ev.type == EventType.MouseDown && ev.button == 0)
        {
            AttrWashDlg.SetTarget(0, bagTab, it.bagIdx, it.name, it.icon);
            ev.Use();
            return true;
        }

        // The two right-button cancels: the tool window and the avatar-fusion window.
        if (ToolTarget.Active && ev != null && ev.type == EventType.MouseDown && ev.button == 1)
        {
            ToolTarget.Cancel();
            ev.Use();
            return true;
        }

        if (AvatarFuse.Active && tab == 2 && !isPet
            && ev != null && ev.type == EventType.MouseDown && ev.button == 0)
        {
            AvatarFuse.PickTargetBag(bagTab, it.bagIdx, it.name, it.avatar > 0);
            ev.Use();
            return true;
        }

        if (AvatarFuse.Active && ev != null && ev.type == EventType.MouseDown && ev.button == 1)
        {
            AvatarFuse.Cancel();
            ev.Use();
            return true;
        }

        if (RepairDlg.Picking && tab == 2 && !isPet
            && ev != null && ev.type == EventType.MouseDown && ev.button == 0)
        {
            RepairDlg.PickBag(bagTab, it.bagIdx);
            ev.Use();
            return true;
        }

        return false;
    }
}
}

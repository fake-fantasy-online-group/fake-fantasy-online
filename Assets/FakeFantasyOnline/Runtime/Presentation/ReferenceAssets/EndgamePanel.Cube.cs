using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    private const float CubeWindowWidth = 450f, CubeWindowHeight = 484f;
    private const int CubeColumns = 5, CubeRows = 7;
    private sealed class CubeSlot
    {
        public string UID, Name;
        public int ItemID, Count, Cell;
    }
    private readonly List<CubeSlot> cubeSlots = new List<CubeSlot>();
    private object cubeInventory;
    private string cubeQuantity = "1", cubeAwaitingQuote;
    private int cubeEditing = -1, cubeMapID, cubeItemID, cubeItemTab, cubeItemSlot;
    private bool cubeBagOpened;
    private void ClearCubeInputs()
    {
        WinDrag.Forget(this);
        cubeSlots.Clear(); cubeInventory = null; cubeEditing = -1; cubeQuantity = "1";
        cubeMapID = cubeItemID = 0; cubeItemTab = cubeItemSlot = -1; cubeAwaitingQuote = null; cubeBagOpened = false;
    }
    private bool CubeSourceCurrent() => client != null && client.EndgameInWorld && State != null && generation == State.Generation && cubeItemID > 0 &&
        State.CubeItemID == cubeItemID && cubeMapID == GameBootstrap.CurMapId && CubeBagItemCurrent(State, cubeItemTab, cubeItemSlot);
    private bool CubeEditable => open && module == EndgameModule.Cube && CubeSourceCurrent() &&
        State.InventorySnapshotCurrent && !State.Pending && !State.QuotePending;
    private void ChangedCubeInputs()
    {
        quotedInputs = null; cubeAwaitingQuote = null; State.InvalidateQuote();
    }
    private void SyncCubeInputs()
    {
        if (ReferenceEquals(cubeInventory, State.Items)) return;
        cubeInventory = State.Items; cubeSlots.Clear(); orderedRunes.Clear(); cubeEditing = -1;
        ChangedCubeInputs();
    }
    private bool CubeHas(string uid, int itemID)
    {
        foreach (var slot in cubeSlots) if (uid.Length > 0 ? slot.UID == uid : slot.UID.Length == 0 && slot.ItemID == itemID) return true;
        return false;
    }
    private int CubeOwnedMaterial(int itemID)
    {
        foreach (var material in State.Materials) if (material.ItemID == itemID) return material.Count;
        return 0;
    }
    private int CubeSlotAt(int cell)
    {
        for (int i = 0; i < cubeSlots.Count; i++) if (cubeSlots[i].Cell == cell) return i;
        return -1;
    }
    private void PutCubeItem(string uid, int itemID, string name) => ReserveCubeItem(uid, itemID, name, -1);
    private bool ReserveCubeItem(string uid, int itemID, string name, int cell)
    {
        if (!CubeEditable || itemID == State.CubeItemID || cubeSlots.Count >= EndgameCubeInput.Capacity || CubeHas(uid, itemID)) return false;
        if (uid.Length == 0) { if (CubeOwnedMaterial(itemID) < 1) return false; }
        else
        {
            bool valid = false;
            foreach (var item in State.Items)
                if (item.UID == uid && item.ItemID == itemID && item.Location == "bag" && !item.Locked && MatchesNative(item)) { valid = true; break; }
            if (!valid) return false;
        }
        if (cell < 0) { cell = 0; while (cell < EndgameCubeInput.Capacity && CubeSlotAt(cell) >= 0) cell++; }
        if (cell >= EndgameCubeInput.Capacity || CubeSlotAt(cell) >= 0) return false;
        cubeSlots.Add(new CubeSlot { UID = uid, ItemID = itemID, Name = name, Count = 1, Cell = cell });
        cubeEditing = uid.Length == 0 ? cubeSlots.Count - 1 : -1;
        cubeQuantity = "1"; orderedRunes.Clear(); ChangedCubeInputs(); return true;
    }
    // Native inventory right-click and carried-item drop reserve the actual observed
    // bag identity. There is no inventory move packet and no second inventory view.
    internal static bool TryPutCubeBagItem(int tab, int bagSlot, int cell = -1)
    {
        if (!WorldEntryAvailable || SysMsgBox.Visible || !Current.CubeEditable || InventoryDlg.IsLocked((byte)tab, bagSlot)) return false;
        var native = InventoryDlg.ItemAt(tab, bagSlot);
        if (native.id == 0 || native.id == Current.State.CubeItemID) return false;
        Current.SyncCubeInputs();
        foreach (var item in Current.State.Items)
            if (item.Tab == tab && item.Slot == bagSlot && item.Location == "bag" && item.ItemID == native.id)
                return Current.ReserveCubeItem(item.UID, item.ItemID, item.Name, cell);
        return Current.ReserveCubeItem("", native.id, native.name, cell);
    }
    internal static bool CubeContainerOpen => WorldEntryAvailable && Current.open && Current.module == EndgameModule.Cube && Current.CubeSourceCurrent();
    private IReadOnlyList<EndgameCubeInput> CubeInputs()
    {
        var result = new List<EndgameCubeInput>();
        foreach (var slot in cubeSlots) result.Add(new EndgameCubeInput(slot.UID, slot.UID.Length > 0 ? 0 : slot.ItemID, slot.Count));
        return result;
    }
    private bool CubeQuantitiesReady => cubeEditing < 0 || cubeEditing >= cubeSlots.Count ||
        cubeQuantity == cubeSlots[cubeEditing].Count.ToString();
    private bool CubeQuoteFresh()
    {
        var quote = State.Quote;
        return quote != null && quotedInputs != null && CubeEditable && CubeQuantitiesReady &&
            EndgameClientState.CompareRevision(quote.Revision, State.Revision) == 0 &&
            DateTimeOffset.TryParse(quote.ExpiresAt, out DateTimeOffset expiry) && expiry > DateTimeOffset.UtcNow;
    }
    private void ConfirmCubeQuote()
    {
        if (!CubeQuoteFresh() || SysMsgBox.Visible) return;
        var quote = State.Quote; var inputs = quotedInputs;
        SysMsgBox.Show(quote.Description + "\n铜币：" + quote.Money + "\n\n确认合成？", () =>
        {
            // The native prompt can outlive a close, tool loss or map change, inventory update,
            // expiry or a different selection. Never execute its stale captured quote.
            if (!ReferenceEquals(quotedInputs, inputs) || !ReferenceEquals(State?.Quote, quote) || !CubeQuoteFresh()) return;
            Send(new EndgameRequest("cube.execute", quote.Revision, quoteID: quote.QuoteID));
        }, null);
    }
    private void SubmitCube()
    {
        if (SysMsgBox.Visible || !CubeEditable || !CubeQuantitiesReady || cubeSlots.Count == 0) return;
        if (CubeQuoteFresh()) { ConfirmCubeQuote(); return; }
        var request = new EndgameRequest("cube.quote", State.Revision, itemUIDs: orderedRunes.Count > 0 ? orderedRunes : null,
            cubeInputs: CubeInputs());
        if (Send(request)) { quotedInputs = request; cubeAwaitingQuote = request.RequestID; }
    }
    private void DrawCube()
    {
        SyncCubeInputs();
        if (!cubeBagOpened) { cubeBagOpened = true; if (!InventoryDlg.IsOpen) InventoryDlg.Toggle(); }
        DrawCubeArtwork();
        Vector2 mouse = Event.current.mousePosition;
        bool editable = CubeEditable && !SysMsgBox.Visible;
        for (int cellIndex = 0; cellIndex < EndgameCubeInput.Capacity; cellIndex++)
        {
            Rect cell = CubeCellRect(cellIndex); int index = CubeSlotAt(cellIndex);
            DrawCubeCell(cell, index == cubeEditing && index >= 0);
            if (index >= 0)
            {
                CubeSlot slot = cubeSlots[index];
                Texture2D icon = NetClient.LoadItemIcon(slot.ItemID);
                if (icon != null) GUI.DrawTexture(new Rect(cell.x + 3, cell.y + 3, 30, 30), icon, ScaleMode.ScaleToFit);
                if (slot.Count > 1) CubeText(new Rect(cell.x + 1, cell.y + 25, 34, 11), slot.Count.ToString(), Gold);
                if (cell.Contains(mouse))
                {
                    string tip = slot.Name + " ×" + slot.Count;
                    if (slot.UID.Length > 0) foreach (var item in State.Items) if (item.UID == slot.UID) { tip = OwnedItemTooltip(item); break; }
                    ShowPanelTooltip(tip + "\n右键取回" + (slot.UID.Length == 0 ? "；左键调整数量" : "；左键取回"));
                    if (editable && Event.current.type == EventType.MouseDown && !InventoryDlg.HasHeld)
                    {
                        if (Event.current.button == 0 && slot.UID.Length == 0) { cubeEditing = index; cubeQuantity = slot.Count.ToString(); }
                        else if (Event.current.button == 1 || Event.current.button == 0)
                        { cubeSlots.RemoveAt(index); cubeEditing = -1; orderedRunes.Clear(); ChangedCubeInputs(); }
                        Event.current.Use();
                    }
                }
            }
            else if (cell.Contains(mouse))
            {
                ShowPanelTooltip("从背包拖入物品；背包右键放入。\n确认合成前不会消耗物品。");
                if (editable && InventoryDlg.HasHeld && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    if (TryPutCubeBagItem(InventoryDlg.HeldTab, InventoryDlg.HeldBagSlot, cellIndex)) InventoryDlg.ClearHeld();
                    Event.current.Use(); PlayerController.SuppressWorldClick();
                }
            }
        }
        DrawCubeFooter(editable);
        if (CubeNativeButton(new Rect(110, 447, 90, 22), CubeMakePath, "确认合成", editable && CubeQuantitiesReady && cubeSlots.Count > 0)) SubmitCube();
        if (CubeNativeButton(new Rect(273, 447, 70, 22), CubeCancelPath, "取消", !SysMsgBox.Visible)) Close();
        if (CubeNativeButton(new Rect(340, 40, 20, 19), CubeClosePath, "×", !SysMsgBox.Visible)) Close();
        if (cubeAwaitingQuote != null && !State.QuotePending)
        {
            cubeAwaitingQuote = null;
            if (CubeQuoteFresh()) ConfirmCubeQuote();
        }
    }
    private void DrawCubeFooter(bool editable)
    {
        if (cubeEditing >= 0 && cubeEditing < cubeSlots.Count && cubeSlots[cubeEditing].UID.Length == 0)
        {
            CubeSlot slot = cubeSlots[cubeEditing];
            CubeText(new Rect(106, 387, 45, 18), "数量", Gold);
            bool enabled = GUI.enabled; GUI.enabled = editable;
            string quantity = GUI.TextField(new Rect(153, 387, 88, 19), cubeQuantity, 10, small);
            GUI.enabled = enabled;
            if (editable && quantity != cubeQuantity) { cubeQuantity = quantity; ChangedCubeInputs(); }
            int owned = CubeOwnedMaterial(slot.ItemID);
            if (ArtworkSkin.Button(new Rect(247, 387, 94, 20), "确认数量", enabled: editable && int.TryParse(cubeQuantity, out int count) && count > 0 && count <= owned))
            { slot.Count = int.Parse(cubeQuantity); cubeEditing = -1; ChangedCubeInputs(); }
        }
        else DrawCubeSocketOrder(new Rect(105, 385, 239, 22), editable);
        string status = State.Pending ? "结果确认中，点击查询" : State.QuotePending ? "辨物中…" :
            State.Error.Length > 0 ? State.Message : State.Quote != null ? "铜币：" + State.Quote.Money :
            State.SnapshotLoading ? "正在同步背包…" : !State.InventorySnapshotCurrent ? "背包未同步，点击重试" : "背包放入 · 右键取回";
        Rect summary = new Rect(105, 417, 239, 18);
        CubeText(summary, status, State.Error.Length > 0 ? new Color(1f, .6f, .48f) : Gold);
        if (summary.Contains(Event.current.mousePosition))
        {
            ShowPanelTooltip(State.Quote != null ? State.Quote.Description + "\n铜币：" + State.Quote.Money : status);
            if (!SysMsgBox.Visible && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            { if (State.Pending) client.RetryEndgame(); else if (!State.InventorySnapshotCurrent) Refresh(); Event.current.Use(); }
        }
    }
    private void DrawCubeSocketOrder(Rect rect, bool editable)
    {
        EndgameItemView target = null;
        foreach (var slot in cubeSlots) foreach (var item in State.Items) if (item.UID == slot.UID && item.SocketCount > 0) target = item;
        if (target == null) return;
        foreach (var rune in target.Sockets)
        {
            if (rune.UID.Length == 0 || rune.UID == "0" || rune.ItemID == 0) continue;
            int position = orderedRunes.IndexOf(rune.UID);
            Rect cell = new Rect(rect.x + rune.Index * 47, rect.y, 44, 22);
            if (cell.Contains(Event.current.mousePosition)) ShowPanelTooltip("按新顺序点击孔内星篆：" + rune.Name);
            if (ArtworkSkin.Button(cell, (position < 0 ? "" : (position + 1) + "·") + rune.Name, enabled: editable, selected: position >= 0))
            { if (!orderedRunes.Remove(rune.UID)) orderedRunes.Add(rune.UID); ChangedCubeInputs(); }
        }
    }
}
}

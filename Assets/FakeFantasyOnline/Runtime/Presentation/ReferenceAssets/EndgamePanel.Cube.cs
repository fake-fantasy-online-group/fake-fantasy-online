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
    private string cubeQuantity = "1", cubeAwaitingQuote, cubeAwaitingExecute;
    private EndgameResponse cubeObservedReceipt;
    private string cubeTransportNotice;
    private EndgameClientState cubeExecuteState;
    private long cubeExecuteGeneration;
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
        cubeInventory = State.Items;
        // A refresh is not a failed conversion: retain still-owned reservations.
        for (int i = cubeSlots.Count - 1; i >= 0; i--)
        {
            var slot = cubeSlots[i];
            bool valid = slot.UID.Length == 0 && CubeOwnedMaterial(slot.ItemID) >= slot.Count;
            if (slot.UID.Length > 0) foreach (var item in State.Items)
                if (item.UID == slot.UID && item.ItemID == slot.ItemID && item.Location == "bag" && !item.Locked && MatchesNative(item)) { valid = true; break; }
            if (!valid) cubeSlots.RemoveAt(i);
        }
        orderedRunes.Clear(); cubeEditing = -1; cubeQuantity = "1";
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
        int.TryParse(cubeQuantity, out int quantity) && quantity > 0 && quantity == cubeSlots[cubeEditing].Count;
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
            var request = new EndgameRequest("cube.execute", quote.Revision, quoteID: quote.QuoteID);
            if (Send(request)) { TrackCubeExecution(request); }
        }, () => { if (ReferenceEquals(quotedInputs, inputs)) ChangedCubeInputs(); });
    }
    private void SubmitCube()
    {
        if (SysMsgBox.Visible || !open || module != EndgameModule.Cube || !CubeSourceCurrent()) return;
        if (State.Pending)
        {
            if (State.PendingRequest.Op == "cube.execute" && State.Error.Length > 0) client.RetryEndgame();
            return;
        }
        if (!State.InventorySnapshotCurrent)
        { Refresh(); NewbieTipBox.PushText("器物尚未同步，请稍候再试。"); return; }
        if (!CubeEditable || !CubeQuantitiesReady) return;
        if (cubeSlots.Count == 0) { NewbieTipBox.PushText("鼎中尚空，请先置入材料。"); return; }
        if (CubeQuoteFresh()) { ConfirmCubeQuote(); return; }
        var request = new EndgameRequest("cube.quote", State.Revision, itemUIDs: orderedRunes.Count > 0 ? orderedRunes : null,
            cubeInputs: CubeInputs());
        if (Send(request)) { quotedInputs = request; cubeAwaitingQuote = request.RequestID; }
    }
    private void TakeBackCubeSelection()
    {
        if (!CubeEditable || SysMsgBox.Visible || InventoryDlg.HasHeldAny ||
            cubeEditing < 0 || cubeEditing >= cubeSlots.Count) return;
        cubeSlots.RemoveAt(cubeEditing); cubeEditing = -1; cubeQuantity = "1";
        orderedRunes.Clear(); ChangedCubeInputs();
    }
    // Reservations are local only. Clearing a read cancels its intent, not inventory.
    private void ClearCubeSelection()
    {
        if (!open || module != EndgameModule.Cube || State == null || State.Pending || SysMsgBox.Visible) return;
        ResetCubeSelection();
    }
    private void ResetCubeSelection()
    {
        cubeSlots.Clear(); cubeEditing = -1; cubeQuantity = "1"; orderedRunes.Clear(); ChangedCubeInputs();
    }
    private void SetCubeQuantity(string quantity)
    {
        if (!CubeEditable || cubeEditing < 0 || cubeEditing >= cubeSlots.Count || cubeSlots[cubeEditing].UID.Length > 0 || quantity == cubeQuantity) return;
        cubeQuantity = quantity;
        if (int.TryParse(quantity, out int count) && count > 0 && count <= CubeOwnedMaterial(cubeSlots[cubeEditing].ItemID))
            cubeSlots[cubeEditing].Count = count;
        ChangedCubeInputs();
    }
    private void TrackCubeExecution(EndgameRequest request)
    {
        cubeAwaitingExecute = request.RequestID; cubeObservedReceipt = null; cubeTransportNotice = null;
        cubeExecuteState = State; cubeExecuteGeneration = State.Generation;
    }
    private void ObserveCubeRequest()
    {
        if (State == null || !client.EndgameInWorld) return;
        if (!ReferenceEquals(cubeExecuteState, State) || cubeExecuteGeneration != State.Generation)
            cubeAwaitingExecute = null;
        // Pending mutations outlive the window, including restored journal requests.
        if (cubeAwaitingExecute == null && State.PendingRequest?.Op == "cube.execute") TrackCubeExecution(State.PendingRequest);
        if (cubeAwaitingExecute != null)
        {
            var response = State.ResponseFor(cubeAwaitingExecute);
            if (response != null && !ReferenceEquals(response, cubeObservedReceipt))
            {
                cubeObservedReceipt = response;
                // Server failures already arrive through native 0x8008 mode 5; do not duplicate them.
                if (!State.Pending)
                {
                    cubeAwaitingExecute = null;
                    if (response.OK && open && module == EndgameModule.Cube) ResetCubeSelection();
                }
            }
            if (response == null && State.Pending && State.PendingRequest.RequestID == cubeAwaitingExecute &&
                State.Error.Length > 0 && cubeTransportNotice != State.Message)
            { cubeTransportNotice = State.Message; NewbieTipBox.PushText(State.Message); }
        }
        if (!open || module != EndgameModule.Cube || !CubeSourceCurrent() || cubeAwaitingQuote == null || State.QuotePending) return;
        string requestID = cubeAwaitingQuote; cubeAwaitingQuote = null;
        var receipt = State.ResponseFor(requestID);
        if (receipt != null && receipt.OK && CubeQuoteFresh()) ConfirmCubeQuote();
        else if (receipt == null && State.Error.Length > 0) NewbieTipBox.PushText(State.Message);
    }
    // Socket ordering remains a contextual row of item icons, not a recipe/help panel.
    private void DrawCubeSocketCells(bool editable)
    {
        EndgameItemView target = null;
        foreach (var slot in cubeSlots) foreach (var item in State.Items)
            if (item.UID == slot.UID && item.SocketCount > 0) target = item;
        if (target == null) return;
        foreach (var rune in target.Sockets)
        {
            if (rune.UID.Length == 0 || rune.UID == "0" || rune.ItemID == 0 || rune.Index < 0 || rune.Index >= CubeColumns) continue;
            Rect cell = new Rect(116 + rune.Index * 43, 382, 40, 40);
            int position = orderedRunes.IndexOf(rune.UID);
            DrawCubeCell(cell, position >= 0);
            Texture2D icon = NetClient.LoadItemIcon(rune.ItemID);
            if (icon != null) GUI.DrawTexture(new Rect(cell.x + 3, cell.y + 3, 30, 30), icon, ScaleMode.ScaleToFit);
            if (position >= 0) CubeText(new Rect(cell.x + 1, cell.y + 25, 34, 11), (position + 1).ToString(), Gold);
            if (cell.Contains(Event.current.mousePosition)) ShowPanelTooltip(rune.Name);
            if (editable && !InventoryDlg.HasHeldAny && CraftUI.Click(cell, Event.current.mousePosition))
            { if (!orderedRunes.Remove(rune.UID)) orderedRunes.Add(rune.UID); ChangedCubeInputs(); }
        }
    }
    private void DrawCube()
    {
        ObserveCubeRequest();
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
                Rect countRect = new Rect(cell.x + 1, cell.y + 24, 38, 16);
                bool editingCount = index == cubeEditing && slot.UID.Length == 0;
                if (editingCount)
                {
                    bool enabled = GUI.enabled; GUI.enabled = editable;
                    string quantity = GUI.TextField(countRect, cubeQuantity, 10, small);
                    GUI.enabled = enabled;
                    if (editable) SetCubeQuantity(quantity);
                }
                else if (slot.Count > 1) CubeText(countRect, slot.Count.ToString(), Gold);
                if (cell.Contains(mouse))
                {
                    string tip = slot.Name + " ×" + slot.Count;
                    if (slot.UID.Length > 0) foreach (var item in State.Items) if (item.UID == slot.UID) { tip = OwnedItemTooltip(item); break; }
                    ShowPanelTooltip(tip);
                    if (editable && Event.current.type == EventType.MouseDown && !InventoryDlg.HasHeldAny &&
                        !(editingCount && countRect.Contains(mouse) && Event.current.button == 0))
                    {
                        if (Event.current.button == 0 && slot.UID.Length == 0 && cubeEditing != index)
                        { cubeEditing = index; cubeQuantity = slot.Count.ToString(); }
                        else if (Event.current.button == 1 || Event.current.button == 0)
                        { cubeEditing = index; TakeBackCubeSelection(); }
                        Event.current.Use();
                    }
                }
            }
            else if (cell.Contains(mouse) && editable && InventoryDlg.HasHeld && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (TryPutCubeBagItem(InventoryDlg.HeldTab, InventoryDlg.HeldBagSlot, cellIndex)) InventoryDlg.ClearHeld();
                Event.current.Use(); PlayerController.SuppressWorldClick();
            }
        }
        DrawCubeSocketCells(editable);
        if (ArtworkSkin.Button(new Rect(110, 432, 100, 38), "转换", enabled: !SysMsgBox.Visible && !State.QuotePending && CubeQuantitiesReady)) SubmitCube();
        if (ArtworkSkin.Button(new Rect(239, 432, 100, 38), "清空", enabled: !SysMsgBox.Visible && !State.Pending)) ClearCubeSelection();
        if (CubeNativeButton(new Rect(340, 40, 20, 19), CubeClosePath, "×", !SysMsgBox.Visible)) Close();
    }
}
}

using System;
using System.Text;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    private sealed class CubeSlot
    {
        public string UID, Name;
        public int ItemID, Count;
    }
    private readonly List<CubeSlot> cubeSlots = new List<CubeSlot>();
    private object cubeInventory;
    private string cubeQuantity = "1";
    private int cubeEditing = -1, cubeNpcEntityID, cubeMapID;
    private Func<bool> cubeSourceCurrent;
    private void ClearCubeInputs()
    {
        cubeSlots.Clear(); cubeInventory = null; cubeEditing = -1; cubeQuantity = "1";
        cubeNpcEntityID = cubeMapID = 0; cubeSourceCurrent = null;
    }
    private bool CubeSourceCurrent() => State != null && State.CubeInputsV1 && cubeNpcEntityID != 0 &&
        cubeMapID == GameBootstrap.CurMapId && cubeSourceCurrent != null && cubeSourceCurrent();
    private void ChangedCubeInputs()
    {
        quotedInputs = null; State.InvalidateQuote();
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
    private void PutCubeItem(string uid, int itemID, string name)
    {
        if (State.Pending || State.QuotePending || cubeSlots.Count >= 12 || CubeHas(uid, itemID)) return;
        cubeSlots.Add(new CubeSlot { UID = uid, ItemID = itemID, Name = name, Count = 1 });
        cubeEditing = cubeSlots.Count - 1; cubeQuantity = "1"; orderedRunes.Clear(); ChangedCubeInputs();
    }
    private IReadOnlyList<EndgameCubeInput> CubeInputs()
    {
        var result = new List<EndgameCubeInput>();
        foreach (var slot in cubeSlots) result.Add(new EndgameCubeInput(slot.UID, slot.UID.Length > 0 ? 0 : slot.ItemID, slot.Count));
        return result;
    }
    private void DrawCube()
    {
        SyncCubeInputs();
        bool editable = !State.Pending && !State.QuotePending && State.InventorySnapshotCurrent;
        ArtworkSkin.Panel(new Rect(20, 103, 420, 483));
        ArtworkSkin.Panel(new Rect(452, 103, 466, 483));
        Text(new Rect(36, 111, 385, 28), "鼎中物品", heading);
        Text(new Rect(466, 111, 426, 28), "背包", heading);
        Text(new Rect(36, 145, 389, 36), "点击背包物品放入，点击鼎中物品取回。确认炼化前，物品仍留在背包。", small);
        for (int i = 0; i < 12; i++)
        {
            Rect cell = new Rect(39 + (i % 4) * 96, 189 + (i / 4) * 72, 87, 65);
            ArtworkSkin.Panel(cell, highlighted: i == cubeEditing);
            if (i >= cubeSlots.Count) continue;
            CubeSlot slot = cubeSlots[i]; Icon(new Rect(cell.x + 3, cell.y + 3, 36, 36), slot.ItemID);
            Text(new Rect(cell.x + 40, cell.y + 4, 44, 22), "×" + slot.Count, small);
            Text(new Rect(cell.x + 3, cell.y + 39, 81, 23), slot.Name, small);
            if (cell.Contains(Event.current.mousePosition)) ShowPanelTooltip(slot.Name + " ×" + slot.Count + "\n左键取回；右键修改数量。");
            if (editable && cell.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 1 && slot.UID.Length == 0) { cubeEditing = i; cubeQuantity = slot.Count.ToString(); }
                else if (Event.current.button == 0) { cubeSlots.RemoveAt(i); cubeEditing = -1; orderedRunes.Clear(); ChangedCubeInputs(); }
                Event.current.Use(); break;
            }
        }
        if (cubeEditing >= 0 && cubeEditing < cubeSlots.Count && cubeSlots[cubeEditing].UID.Length == 0)
        {
            CubeSlot slot = cubeSlots[cubeEditing];
            Text(new Rect(39, 411, 149, 24), slot.Name + "数量", small);
            string quantity = GUI.TextField(new Rect(189, 411, 111, 25), cubeQuantity, 10, label);
            if (editable && quantity != cubeQuantity) { cubeQuantity = quantity; ChangedCubeInputs(); }
            int owned = 0; foreach (var material in State.Materials) if (material.ItemID == slot.ItemID) owned = material.Count;
            if (ArtworkSkin.Button(new Rect(310, 411, 111, 25), "确认数量", enabled: editable && int.TryParse(cubeQuantity, out int count) && count > 0 && count <= owned))
            {
                if (int.TryParse(cubeQuantity, out int n) && n > 0 && n <= owned) { slot.Count = n; cubeEditing = -1; ChangedCubeInputs(); }
            }
        }
        DrawCubeBag(new Rect(465, 151, 439, 307), editable);
        DrawCubeSocketOrder(new Rect(39, 443, 382, 49), editable);
        var quote = State.Quote;
        string preview = State.QuotePending ? "鼎火辨物中…" : "放入物品后问鼎。未起反应时不会消耗物品。";
        if (quote != null)
        {
            var text = new StringBuilder(quote.Description).Append("\n铜币：").Append(quote.Money);
            preview = text.ToString();
        }
        ArtworkSkin.Panel(new Rect(464, 465, 440, 108), highlighted: quote != null);
        float previewHeight = Math.Max(92, small.CalcHeight(new GUIContent(preview), 407));
        detailScroll = GUI.BeginScrollView(new Rect(473, 471, 423, 97), detailScroll, new Rect(0, 0, 407, previewHeight));
        Text(new Rect(0, 0, 407, previewHeight), preview, small, quote == null ? (Color?)null : Gold); GUI.EndScrollView();
        Text(new Rect(39, 496, 382, 40), "问鼎只核对投料。点击炼化才会消耗鼎中物品与所示铜币。", small);
        bool quantitiesReady = cubeEditing < 0 || cubeEditing >= cubeSlots.Count || cubeSlots[cubeEditing].UID.Length > 0 || cubeQuantity == cubeSlots[cubeEditing].Count.ToString();
        if (ArtworkSkin.Button(new Rect(39, 544, 177, 30), "问鼎", enabled: editable && quantitiesReady && CubeSourceCurrent() && cubeSlots.Count > 0))
        {
            quotedInputs = new EndgameRequest("cube.quote", State.Revision, itemUIDs: orderedRunes.Count > 0 ? orderedRunes : null, npcEntityID: cubeNpcEntityID, cubeInputs: CubeInputs());
            Send(quotedInputs);
        }
        bool fresh = quote != null && quotedInputs != null && EndgameClientState.CompareRevision(quote.Revision, State.Revision) == 0 &&
            DateTimeOffset.TryParse(quote.ExpiresAt, out DateTimeOffset expiry) && expiry > DateTimeOffset.UtcNow;
        if (ArtworkSkin.Button(new Rect(230, 544, 191, 30), "炼化", enabled: fresh && editable && quantitiesReady && CubeSourceCurrent()))
            Send(new EndgameRequest("cube.execute", quote.Revision, quoteID: quote.QuoteID, npcEntityID: cubeNpcEntityID));
    }
    private void DrawCubeBag(Rect rect, bool editable)
    {
        var rows = new List<CubeSlot>();
        foreach (var item in State.Items)
            if (item.Location == "bag" && !item.Locked && MatchesNative(item)) rows.Add(new CubeSlot { UID = item.UID, ItemID = item.ItemID, Name = item.Name, Count = 1 });
        foreach (var material in State.Materials)
        {
            bool instanced = false; foreach (var item in State.Items) if (item.ItemID == material.ItemID) { instanced = true; break; }
            if (material.Count > 0 && !instanced) rows.Add(new CubeSlot { UID = "", ItemID = material.ItemID, Name = material.Name, Count = material.Count });
        }
        itemScroll = GUI.BeginScrollView(rect, itemScroll, new Rect(0, 0, rect.width - 20, Math.Max(rect.height, ((rows.Count + 3) / 4) * 84)));
        for (int i = 0; i < rows.Count; i++)
        {
            CubeSlot row = rows[i]; Rect cell = new Rect((i % 4) * 104, (i / 4) * 84, 96, 77);
            Icon(new Rect(cell.x + 5, cell.y + 3, 40, 40), row.ItemID);
            Text(new Rect(cell.x + 48, cell.y + 9, 46, 26), "×" + row.Count, small);
            Text(new Rect(cell.x + 3, cell.y + 46, 91, 28), row.Name, small);
            bool selected = CubeHas(row.UID, row.ItemID);
            if (cell.Contains(Event.current.mousePosition))
            {
                string tip = row.Name + " ×" + row.Count;
                if (row.UID.Length > 0) foreach (var item in State.Items) if (item.UID == row.UID) { tip = OwnedItemTooltip(item); break; }
                ShowPanelTooltip(tip + (selected ? "\n已放入鼎中" : "\n点击放入鼎中"));
                if (editable && !selected && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                { PutCubeItem(row.UID, row.ItemID, row.Name); Event.current.Use(); }
            }
            if (selected) Text(new Rect(cell.x + 5, cell.y + 29, 88, 20), "已放入", small, Gold);
        }
        GUI.EndScrollView();
    }
    private void DrawCubeSocketOrder(Rect rect, bool editable)
    {
        EndgameItemView target = null;
        foreach (var slot in cubeSlots) foreach (var item in State.Items) if (item.UID == slot.UID && item.SocketCount > 0) target = item;
        if (target == null) return;
        Text(new Rect(rect.x, rect.y, rect.width, 21), "孔内顺序：按新顺序点击星篆", small);
        foreach (var rune in target.Sockets)
        {
            if (rune.UID.Length == 0 || rune.UID == "0" || rune.ItemID == 0) continue;
            int position = orderedRunes.IndexOf(rune.UID);
            if (ArtworkSkin.Button(new Rect(rect.x + rune.Index * 76, rect.y + 22, 73, 25), (position < 0 ? "" : (position + 1) + "·") + rune.Name, enabled: editable, selected: position >= 0))
            { if (!orderedRunes.Remove(rune.UID)) orderedRunes.Add(rune.UID); ChangedCubeInputs(); }
        }
    }
}
}

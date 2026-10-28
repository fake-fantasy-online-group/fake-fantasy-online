using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    private string selectedPowerID = "", observedPowerImprint = "";
    private Vector2 powerScroll, powerFilterScroll;
    private string powerSourceUID = "";
    private long powerSourceGeneration;
    private string powerClass = "", powerCategory = "", powerSlot = "";
    private bool hideUnobtained;
    private int powerFilterOpen = -1;
    private int pendingPowerTab = -1, pendingPowerSlot, pendingPowerItem;
    private long pendingPowerGeneration;
    // There is deliberately no inventory-header or world shortcut: the permanent item is the entry.
    internal static bool TryOpenPowerBookFromWorld() => false;
    internal static bool HandlePowerItemForIndependentRuntime(int id, int tab, int slot)
    {
        var state = Current?.State;
        if (state == null || id <= 0 || (id != state.PowerBookItemID && id != state.PowerScrollItemID)) return false;
        if (!WorldEntryAvailable || !state.PowerBookAvailable || SysMsgBox.Visible || state.Pending ||
            InventoryDlg.ItemAt(tab, slot).id != id || InventoryDlg.IsLocked((byte)tab, slot)) return true;
        if (!state.InventorySnapshotCurrent)
        {
            Current.pendingPowerTab = tab; Current.pendingPowerSlot = slot; Current.pendingPowerItem = id;
            Current.pendingPowerGeneration = state.Generation;
            Current.Send(new EndgameRequest("snapshot"));
            return true;
        }
        var item = state.Find("bag", tab, slot, id);
        if (item == null || item.Locked || !item.Bound || !MatchesNative(item)) return true;
        if (id == state.PowerBookItemID) TryOpenWorldView(EndgameModule.PowerBook);
        else if (item.PowerScroll != null)
        { Current.powerSourceUID = item.UID; Current.powerSourceGeneration = state.Generation; AvatarFuse.Begin((byte)tab, slot, 3); }
        return true;
    }
    internal static bool ApplyPowerScroll(int sourceTab, int sourceSlot, string location, int targetTab, int targetSlot)
    {
        if (!WorldEntryAvailable || Current.State.Pending || !Current.State.InventorySnapshotCurrent)
        { NewbieTipBox.PushText("器物尚未同步，请稍候再试。"); return false; }
        var state = Current.State;
        var source = state.Find("bag", sourceTab, sourceSlot, state.PowerScrollItemID);
        EndgameItemView target = null;
        foreach (var item in state.Items)
            if (item.Location == location && item.Slot == targetSlot && (location == "equipped" || item.Tab == targetTab)) { target = item; break; }
        // Target eligibility (including growth wings) is authoritative on the server.
        // A wing need not have a legendary payload before receiving its first power.
        if (Current.powerSourceGeneration != state.Generation || source?.UID != Current.powerSourceUID || source?.PowerScroll == null || !source.Bound || source.Locked || target == null || target.Locked ||
            source.UID == target.UID || !MatchesNative(source) || !MatchesNative(target))
        { NewbieTipBox.PushText("请选一件未锁定的暗金装备或羽翼，以承此卷威能。"); return false; }
        var request = new EndgameRequest("powerbook.apply", state.Revision, targetUID: target.UID, runeUID: source.UID);
        if (!Current.Send(request)) return false;
        Current.observedPowerImprint = request.RequestID;
        return true;
    }
    internal static bool RecoverPowerBookFromNpc(EndgameClientState state, int entityID)
    {
        if (!WorldEntryAvailable || !ReferenceEquals(Current.State, state) || !state.PowerBookAvailable || entityID == 0) return false;
        if (state.Pending) return state.PendingRequest.Op == "powerbook.recover" && Current.client.RetryEndgame();
        return state.InventorySnapshotCurrent && Current.Send(new EndgameRequest("powerbook.recover", state.Revision, npcEntityID: entityID));
    }
    private bool HasPowerBookItem()
    {
        if (!State.InventorySnapshotCurrent) return false;
        foreach (var item in State.Items)
            if (item.Location == "bag" && item.ItemID == State.PowerBookItemID && item.Bound && !item.Locked && MatchesNative(item)) return true;
        return false;
    }
    private void ExtractPower(EndgamePowerRecordView power)
    {
        if (!power.Unlocked || !HasPowerBookItem() || !State.PowerBookCurrent || State.PowerBookLoading || State.Pending) return;
        var request = new EndgameRequest("powerbook.extract", State.Revision, powerID: power.PowerID);
        if (Send(request)) observedPowerImprint = request.RequestID;
    }
    private void ObservePowerBookRequest()
    {
        if (pendingPowerTab >= 0)
        {
            if (pendingPowerGeneration != State.Generation) pendingPowerTab = -1;
            else if (State.InventorySnapshotCurrent)
            {
                int tab = pendingPowerTab; pendingPowerTab = -1;
                HandlePowerItemForIndependentRuntime(pendingPowerItem, tab, pendingPowerSlot);
            }
        }
        if (open && module == EndgameModule.PowerBook && State.InventorySnapshotCurrent && !HasPowerBookItem()) Close();
        if (open && module == EndgameModule.PowerBook && State.PowerBookCurrent && !State.PowerBookLoading && State.PowerBookNextCursor.Length > 0)
            Send(new EndgameRequest("powerbook.list", cursor: State.PowerBookNextCursor));
        if (observedPowerImprint.Length == 0) return;
        var response = State.ResponseFor(observedPowerImprint);
        if (response == null || State.PendingRequest?.RequestID == observedPowerImprint) return;
        observedPowerImprint = "";
        if (response.OK && open && module == EndgameModule.PowerBook) RefreshPowerBook();
    }
    private void RefreshPowerBook()
    {
        if (State.PowerBookLoading) return;
        Send(new EndgameRequest("snapshot"));
        Send(new EndgameRequest("powerbook.list"));
    }
    private static string PowerDescription(EndgamePowerRecordView power)
    {
        string prefix = power.Name + "：";
        return (power.Description.StartsWith(prefix, StringComparison.Ordinal) ? power.Description : prefix + power.Description) +
            (power.Unlocked ? "" : "\n尚未获得");
    }
    internal static bool PowerMatches(EndgamePowerRecordView power, string scope, string type, string slot, bool hide)
    {
        if (hide && !power.Unlocked || scope.Length > 0 && power.Class != scope || type.Length > 0 && power.Category != type) return false;
        if (slot.Length == 0) return true;
        foreach (string allowed in power.Slots) if (allowed == slot) return true;
        return false;
    }
    private List<string> PowerFilterOptions(int filter)
    {
        var options = new List<string> { "" };
        foreach (var power in State.Powers)
        {
            if (filter == 2) { foreach (string slot in power.Slots) if (!options.Contains(slot)) options.Add(slot); }
            else { string value = filter == 0 ? power.Class : power.Category; if (value.Length > 0 && !options.Contains(value)) options.Add(value); }
        }
        return options;
    }
    private void DrawPowerBook()
    {
        ArtworkSkin.Panel(new Rect(0, 0, W, H));
        Text(new Rect(24, 15, W - 80, 32), State.PowerBookName, heading);
        if (ArtworkSkin.Button(new Rect(W - 48, 18, 27, 27), "×")) { Close(); return; }
        string[] values = { powerClass, powerCategory, powerSlot }; string[] names = { "范围", "类型", "部位" };
        for (int i = 0; i < 3; i++)
            if (ArtworkSkin.Button(new Rect(24 + i * 216, 62, 202, 30), names[i] + "：" + (values[i].Length == 0 ? "全部" : values[i]) + " ▾"))
                powerFilterOpen = powerFilterOpen == i ? -1 : i;
        if (ArtworkSkin.Button(new Rect(684, 62, 220, 30), (hideUnobtained ? "☑ " : "☐ ") + "隐藏未获得")) { hideUnobtained = !hideUnobtained; powerScroll = Vector2.zero; }
        var rows = new List<EndgamePowerRecordView>();
        foreach (var power in State.Powers) if (PowerMatches(power, powerClass, powerCategory, powerSlot, hideUnobtained)) rows.Add(power);
        Rect area = new Rect(24, 108, W - 48, 470);
        ArtworkSkin.Panel(area);
        powerScroll = GUI.BeginScrollView(area, powerScroll, new Rect(0, 0, area.width - 20, Math.Max(area.height, rows.Count * 44)));
        for (int i = 0; i < rows.Count; i++)
        {
            var power = rows[i]; Rect row = new Rect(0, i * 44, area.width - 22, 42);
            ArtworkSkin.Row(row, highlighted: power.PowerID == selectedPowerID);
            Text(new Rect(12, row.y + 7, 400, 28), power.Name, label, power.Unlocked ? DarkGoldColor : (Color?)null);
            Text(new Rect(430, row.y + 7, 400, 28), power.Class + " · " + power.Category + (power.Unlocked ? "" : " · 未获得"), small);
            if (powerFilterOpen < 0 && row.Contains(Event.current.mousePosition))
            {
                ShowPanelTooltip(PowerDescription(power));
                if (!State.Pending && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                { selectedPowerID = power.PowerID; ExtractPower(power); Event.current.Use(); }
            }
        }
        if (State.PowerBookLoaded && rows.Count == 0) Text(new Rect(12, 16, area.width - 30, 45), "暂无符合筛选条件的威能。", small);
        GUI.EndScrollView();
        Text(new Rect(26, 596, W - 165, 45), State.Pending ? "正在提取…" : State.PowerBookLoading ? "正在翻阅…" : State.Message, small);
        if (ArtworkSkin.Button(new Rect(W - 130, 602, 100, 30), State.Pending ? "查询原请求" : "刷新", enabled: State.Pending || !State.PowerBookLoading))
        { if (State.Pending) client.RetryEndgame(); else RefreshPowerBook(); }
        if (powerFilterOpen >= 0)
        {
            int filter = powerFilterOpen; var options = PowerFilterOptions(filter);
            Rect popup = new Rect(24 + filter * 216, 94, 202, Math.Min(410, options.Count * 27 + 8));
            ArtworkSkin.Panel(popup);
            powerFilterScroll = GUI.BeginScrollView(popup, powerFilterScroll, new Rect(0, 0, 182, options.Count * 27 + 8));
            for (int i = 0; i < options.Count; i++)
                if (ArtworkSkin.Button(new Rect(4, 4 + i * 27, 174, 26), options[i].Length == 0 ? "全部" : options[i]))
                { if (filter == 0) powerClass = options[i]; else if (filter == 1) powerCategory = options[i]; else powerSlot = options[i]; powerFilterOpen = -1; powerScroll = Vector2.zero; }
            GUI.EndScrollView();
        }
    }
}
}

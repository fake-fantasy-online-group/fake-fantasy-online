using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    private string selectedPowerID = "", observedPowerImprint = "";
    private Vector2 powerScroll;
    private int powerFilterScroll, powerFilterHighlight;
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
        string effect = power.Description.StartsWith(prefix, StringComparison.Ordinal) ? power.Description : prefix + power.Description;
        // The same native per-line effect marker used by legendary gear and scrolls.
        return Orange(effect.Replace("\\n", "\n").Replace("\n", "\n\u0006")) +
            (power.Unlocked ? "" : "\n尚未获得");
    }
    // Prefer the server's explicitly paired primary endpoints. The old range
    // prose fallback is only for older servers that omit primaryRangeText.
    private static readonly Regex PowerRangePair = new Regex(@"[-+]?\d+(?:\.\d+)?(?:个百分点|点法力|点生命|距离|秒|点|次|米|倍|层|%)?\s*～\s*[-+]?\d+(?:\.\d+)?(?:个百分点|点法力|点生命|距离|秒|点|次|米|倍|层|%)?", RegexOptions.CultureInvariant);
    internal static string PowerRangeText(EndgamePowerRecordView power)
    {
        if (!string.IsNullOrWhiteSpace(power.PrimaryRangeText)) return power.PrimaryRangeText;
        var match = PowerRangePair.Match(power.RangeText ?? "");
        return match.Success ? match.Value : "—";
    }
    internal static string PowerCurrentValueText(EndgamePowerRecordView power) =>
        power.Unlocked && !string.IsNullOrWhiteSpace(power.CurrentValueText) ? power.CurrentValueText : "—";
    private float PowerBookRowHeight(EndgamePowerRecordView power) =>
        Math.Max(MobileClientPresentation.Enabled ? 44 / panelScale : 32, (BitmapFont.Ready ? BitmapFont.Wrap(power.Name, 280, 1).Count * 16 : label.CalcHeight(new GUIContent(power.Name), 280)) + 12);
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
    private void PowerText(Rect rect, string text, GUIStyle style, Color? color = null)
    {
        if (!BitmapFont.Ready) { Text(rect, text, style, color); return; }
        float size = style == heading ? 1.5f : 1f;
        var lines = BitmapFont.Wrap(text ?? "", rect.width, size);
        float y = rect.y + Math.Max(0, (rect.height - lines.Count * 16 * size) * .5f);
        foreach (string line in lines)
        {
            if (y + 12 * size > rect.yMax) break;
            BitmapFont.DrawOutline(rect.x, y, line, color ?? style.normal.textColor, size);
            y += 16 * size;
        }
    }
    private void DrawPowerBook()
    {
        if (Event.current.type != EventType.Repaint) DrawPowerFilterDropdown();
        DrawRiftWindowArtwork(new Rect(0, 0, W, H));
        PowerText(new Rect(24, 15, W - 80, 32), State.PowerBookName, heading);
        if (ArtworkSkin.Close(new Rect(W - 44, 20, 20, 19))) { Close(); return; }
        // Match the existing rift selector: input + native arrow, not a cycling button.
        for (int i = 0; i < 3; i++) DrawPowerFilterField(i);
        // Same original checkbox frames and label hit area as SysConfigDlg.
        Texture2D check = RiftTexture("res/uires/common/checkbox.gsa", hideUnobtained ? 1 : 0);
        if (check != null) GUI.DrawTexture(new Rect(692, PowerFilterField(0).y + (PowerFilterField(0).height - 17) / 2, 18, 17), check);
        PowerText(new Rect(720, PowerFilterField(0).y + (PowerFilterField(0).height - 26) / 2, 184, 26), "隐藏未获得", label, Gold);
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && new Rect(684, PowerFilterField(0).y, 220, PowerFilterField(0).height).Contains(Event.current.mousePosition))
        { hideUnobtained = !hideUnobtained; powerScroll = Vector2.zero; Event.current.Use(); PlayerController.SuppressWorldClick(); }
        var rows = new List<EndgamePowerRecordView>();
        foreach (var power in State.Powers) if (PowerMatches(power, powerClass, powerCategory, powerSlot, hideUnobtained)) rows.Add(power);
        float headerY = Math.Max(106, PowerFilterField(0).yMax + 8);
        PowerText(new Rect(36, headerY, 280, 26), "威能", small);
        PowerText(new Rect(340, headerY, 130, 26), "当前数值", small);
        PowerText(new Rect(488, headerY, 220, 26), "数值范围", small);
        PowerText(new Rect(744, headerY, 140, 26), "操作", small);
        Rect area = new Rect(24, headerY + 30, W - 72, 578 - headerY - 30);
        float contentHeight = 0;
        foreach (var power in rows) contentHeight += PowerBookRowHeight(power);
        // Same clipped viewport + original scrollbar as ShopDlg.Window. Never show
        // Unity's default scrollbar or a second custom black panel inside the frame.
        powerScroll.y = Mathf.Clamp(powerScroll.y, 0, Math.Max(0, contentHeight - area.height));
        if (powerFilterOpen < 0 || Event.current.type == EventType.Repaint)
        {
            if (powerFilterOpen < 0) NativeScroll.Wheel(area, area.height, contentHeight, ref powerScroll, 52);
            NativeScroll.BarAt(this, new Rect(900, area.y, 15, area.height), area.height, contentHeight, ref powerScroll);
        }
        powerScroll = GUI.BeginScrollView(area, powerScroll, new Rect(0, 0, area.width, Math.Max(area.height, contentHeight)), GUIStyle.none, GUIStyle.none);
        float rowY = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            var power = rows[i]; float height = PowerBookRowHeight(power);
            Rect row = new Rect(0, rowY, area.width - 2, height - 2); rowY += height;
            ArtworkSkin.Row(row, highlighted: power.PowerID == selectedPowerID);
            PowerText(new Rect(12, row.y + 7, 280, height - 14), power.Name, label, power.Unlocked ? DarkGoldColor : (Color?)null);
            PowerText(new Rect(316, row.y + 7, 130, height - 14), PowerCurrentValueText(power), label);
            PowerText(new Rect(464, row.y + 7, 220, height - 14), PowerRangeText(power), label);
            if (ArtworkSkin.ButtonWithHitArea(new Rect(710, row.y + 1, 140, height - 2), new Rect(730, row.y + (height - 22) / 2, 100, 22), "提取卷轴",
                enabled: powerFilterOpen < 0 && power.Unlocked && HasPowerBookItem() && State.PowerBookCurrent && !State.PowerBookLoading && !State.Pending))
            { selectedPowerID = power.PowerID; ExtractPower(power); }
            if (powerFilterOpen < 0 && row.Contains(Event.current.mousePosition))
            {
                ShowPanelTooltip(PowerDescription(power));
            }
        }
        if (State.PowerBookLoaded && rows.Count == 0) PowerText(new Rect(12, 16, area.width - 30, 45), "暂无符合筛选条件的威能。", small);
        GUI.EndScrollView();
        PowerText(new Rect(26, 596, W - 165, 45), State.Pending ? "正在提取…" : State.PowerBookLoading ? "正在翻阅…" : State.Message, small);
        if (ArtworkSkin.ButtonWithHitArea(new Rect(W - 142, 590, 116, 44), new Rect(W - 130, 601, 92, 22), State.Pending ? "查询原请求" : "刷新", enabled: State.Pending || !State.PowerBookLoading))
        { if (State.Pending) client.RetryEndgame(); else RefreshPowerBook(); }
        if (Event.current.type == EventType.Repaint) DrawPowerFilterDropdown();
    }
    private Rect PowerFilterField(int filter) => new Rect(68 + filter * 216, 62, 158, MobileClientPresentation.Enabled ? Math.Max(32, 44 / panelScale) : 32);
    private string PowerFilterValue(int filter) => filter == 0 ? powerClass : filter == 1 ? powerCategory : powerSlot;
    private bool PowerFiltersEditable => !State.Pending && !State.PowerBookLoading && State.PowerBookCurrent;
    private void ResetPowerFilterDropdown()
    {
        powerFilterOpen = -1; powerFilterScroll = powerFilterHighlight = 0;
    }
    private void OpenPowerFilter(int filter)
    {
        powerFilterOpen = filter;
        powerFilterHighlight = Math.Max(0, PowerFilterOptions(filter).IndexOf(PowerFilterValue(filter)));
        powerFilterScroll = powerFilterHighlight;
    }
    private void SelectPowerFilter(string value)
    {
        if (powerFilterOpen == 0) powerClass = value;
        else if (powerFilterOpen == 1) powerCategory = value;
        else powerSlot = value;
        powerScroll = Vector2.zero;
        ResetPowerFilterDropdown();
    }
    private void DrawPowerFilterField(int filter)
    {
        if (!PowerFiltersEditable) ResetPowerFilterDropdown();
        Rect field = PowerFilterField(filter);
        bool expanded = powerFilterOpen == filter;
        ArtworkSkin.Input(field, focused: expanded);
        string name = filter == 0 ? "范围" : filter == 1 ? "类型" : "部位";
        string value = PowerFilterValue(filter);
        // Traditional label:control layout. Only the input itself owns clicks.
        PowerText(new Rect(field.x - 44, field.y + (field.height - 26) / 2, 40, 26), name + "：", label, Gold);
        PowerText(new Rect(field.x + 10, field.y + (field.height - 26) / 2, field.width - 42, 26), value.Length == 0 ? "全部" : value, label, PowerFiltersEditable ? Gold : Muted);
        Texture2D arrow = RiftTexture(RiftDropdownPath, expanded ? 1 : 2);
        if (arrow != null) GUI.DrawTexture(new Rect(field.xMax - 28, field.y + (field.height - 19) / 2, 20, 19), arrow);
        Event current = Event.current;
        if (PowerFiltersEditable && current.type == EventType.MouseDown && current.button == 0 && field.Contains(current.mousePosition))
        {
            if (expanded) ResetPowerFilterDropdown(); else OpenPowerFilter(filter);
            current.Use(); PlayerController.SuppressWorldClick();
        }
    }
    private const float PowerPopupTop = 4, PowerPopupBottom = 4;
    private Rect PowerFilterPopup(int count, out int visible, out float rowHeight)
    {
        Rect field = PowerFilterField(powerFilterOpen);
        rowHeight = MobileClientPresentation.Enabled ? Math.Max(24, 44 / panelScale) : 24;
        // Match the field's editbk 3px border; content starts inside its gold outline.
        visible = Math.Min(count, Math.Min(8, Math.Max(1,
            (int)((H - field.yMax - 3 - PowerPopupTop - PowerPopupBottom - 20) / rowHeight))));
        return new Rect(field.x, field.yMax + 3, field.width, PowerPopupTop + visible * rowHeight + PowerPopupBottom);
    }
    private static Rect PowerFilterOptionRect(Rect popup, int index, float rowHeight, bool overflow)
        => new Rect(popup.x + 4, popup.y + PowerPopupTop + index * rowHeight,
            popup.width - 8 - (overflow ? 19 : 0), rowHeight);

    // The rift selector's native popup primitives and event-first/paint-last ordering.
    // Never let a selection/dismissal fall through to an extractable power row.
    private void DrawPowerFilterDropdown()
    {
        if (powerFilterOpen < 0) return;
        if (!PowerFiltersEditable) { ResetPowerFilterDropdown(); return; }
        var options = PowerFilterOptions(powerFilterOpen);
        int visible; float rowHeight;
        Rect popup = PowerFilterPopup(options.Count, out visible, out rowHeight);
        powerFilterScroll = Math.Max(0, Math.Min(options.Count - visible, powerFilterScroll));
        powerFilterHighlight = Math.Max(0, Math.Min(options.Count - 1, powerFilterHighlight));
        Event current = Event.current;
        if (current.type == EventType.KeyDown)
        {
            switch (current.keyCode)
            {
                case KeyCode.Escape: ResetPowerFilterDropdown(); break;
                case KeyCode.Return: case KeyCode.KeypadEnter: SelectPowerFilter(options[powerFilterHighlight]); break;
                case KeyCode.Home: powerFilterHighlight = 0; break;
                case KeyCode.End: powerFilterHighlight = options.Count - 1; break;
                case KeyCode.UpArrow: powerFilterHighlight = Math.Max(0, powerFilterHighlight - 1); break;
                case KeyCode.DownArrow: powerFilterHighlight = Math.Min(options.Count - 1, powerFilterHighlight + 1); break;
                case KeyCode.PageUp: powerFilterHighlight = Math.Max(0, powerFilterHighlight - visible); break;
                case KeyCode.PageDown: powerFilterHighlight = Math.Min(options.Count - 1, powerFilterHighlight + visible); break;
                default: return;
            }
            powerFilterScroll = Math.Max(0, Math.Min(options.Count - visible, Math.Max(powerFilterHighlight - visible + 1, Math.Min(powerFilterScroll, powerFilterHighlight))));
            current.Use(); PlayerController.SuppressWorldClick(); return;
        }
        if (current.type == EventType.MouseDown && !popup.Contains(current.mousePosition))
        {
            int next = -1;
            for (int i = 0; i < 3; i++) if (PowerFilterField(i).Contains(current.mousePosition) && i != powerFilterOpen) next = i;
            ResetPowerFilterDropdown();
            if (next >= 0 && current.button == 0) OpenPowerFilter(next);
            current.Use(); PlayerController.SuppressWorldClick(); return;
        }
        if (current.type == EventType.ScrollWheel)
        {
            if (popup.Contains(current.mousePosition))
                powerFilterScroll = Math.Max(0, Math.Min(options.Count - visible, powerFilterScroll + (current.delta.y >= 0 ? 1 : -1)));
            current.Use(); PlayerController.SuppressWorldClick();
        }
        ArtworkSkin.Input(popup, focused: true, opaque: true);
        bool overflow = options.Count > visible;
        if (overflow) NativeScroll.BarRows(this, new Rect(popup.xMax - 19, popup.y + PowerPopupTop, 15, visible * rowHeight), visible, options.Count, ref powerFilterScroll);
        for (int i = 0; i < visible; i++)
        {
            int index = powerFilterScroll + i;
            Rect row = PowerFilterOptionRect(popup, i, rowHeight, overflow);
            bool hover = row.Contains(current.mousePosition), selected = options[index] == PowerFilterValue(powerFilterOpen);
            ArtworkSkin.Row(row, highlighted: hover || index == powerFilterHighlight);
            PowerText(new Rect(row.x + 6, row.y + (rowHeight - 18) / 2, row.width - 12, 18), options[index].Length == 0 ? "全部" : options[index], label, selected ? Gold : Jade);
            if (hover && current.type == EventType.MouseDown && current.button == 0)
            { SelectPowerFilter(options[index]); current.Use(); PlayerController.SuppressWorldClick(); return; }
        }
        if (popup.Contains(current.mousePosition) && (current.type == EventType.MouseDown || current.type == EventType.MouseUp))
        { current.Use(); PlayerController.SuppressWorldClick(); }
    }
}
}

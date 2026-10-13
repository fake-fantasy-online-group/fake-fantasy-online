using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    private static readonly Color DarkGoldColor = new Color(183f / 255f, 155f / 255f, 98f / 255f);
    private static bool IsSpecial(EndgameItemView item) => item.UniqueID > 0 || item.FormationID > 0;
    private static string DisplayName(EndgameItemView item) => item.FormationID > 0 ? FormationName(Formation(item.FormationID)?.Name ?? item.Name) : item.Name;
    private EndgameItemView Selected()
    {
        foreach (var item in State.Items) if (item.UID == selectedUID && MatchesNative(item)) return item;
        return null;
    }
    internal static bool MatchesNative(EndgameItemView item)
    {
        if (item == null || !WorldEntryAvailable || Current.State.InventorySnapshotCurrent != true) return false;
        if (item.Location == "bag") return InventoryDlg.ItemAt(item.Tab, item.Slot).id == item.ItemID;
        if (item.Location == "equipped") foreach (var row in EquipDlg.Equipped) if (row.slot == item.Slot && row.id == item.ItemID) return true;
        return false;
    }
    private static bool IsRune(EndgameItemView item) => item.ItemID >= 1900028001 && item.ItemID <= 1900028028;
    private void Select(EndgameItemView item)
    {
        if (State.Pending) return;
        if (IsRune(item) || item.SocketCount > 0) LoadCatalog("runes");
        selectedUID = item.UID; selectedRuneUID = ""; orderedRunes.Clear(); quotedInputs = null; State.InvalidateQuote(); detailScroll = Vector2.zero;
    }
    private static void Icon(Rect rect, int itemID, bool unique = false)
    {
        ArtworkSkin.Panel(rect, highlighted: unique);
        Texture2D icon = NetClient.LoadItemIcon(itemID);
        if (icon != null) GUI.DrawTexture(new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6), icon, ScaleMode.ScaleToFit);
    }
    private static string OwnedItemTooltip(EndgameItemView item)
    {
        // Workbench picking uses the same native tooltip, without a second gear card.
        if (item.Location == "bag")
        {
            var native = InventoryDlg.ItemAt(item.Tab, item.Slot);
            return BagTooltip(item.Tab, native, TooltipContent.Fmt(TooltipContent.QualityName(native.name, native.quality), native.desc));
        }
        foreach (var native in EquipDlg.Equipped)
            if (native.slot == item.Slot && native.id == item.ItemID)
                return EquippedTooltip(native, TooltipContent.Fmt(TooltipContent.QualityName(native.name, native.quality), native.desc));
        return "";
    }
    private void DrawItemList(Rect rect, bool bagOnly = false)
    {
        ArtworkSkin.Panel(rect);
        Text(new Rect(rect.x + 10, rect.y + 7, rect.width - 20, 22), bagOnly ? "选择背包器物" : "选择器物", small);
        var rows = new List<EndgameItemView>();
        foreach (var item in State.Items) if (MatchesNative(item) && !IsRune(item) && (!bagOnly || item.Location == "bag")) rows.Add(item);
        Rect area = new Rect(rect.x + 5, rect.y + 34, rect.width - 10, rect.height - 39);
        itemScroll = GUI.BeginScrollView(area, itemScroll, new Rect(0, 0, area.width - 20, Math.Max(area.height, rows.Count * 57)));
        for (int i = 0; i < rows.Count; i++)
        {
            EndgameItemView item = rows[i]; Rect row = new Rect(0, i * 57, area.width - 22, 54);
            ArtworkSkin.Row(row, highlighted: item.UID == selectedUID);
            Icon(new Rect(6, row.y + 7, 36, 36), item.ItemID, item.UniqueID > 0);
            Text(new Rect(47, row.y + 4, row.width - 49, 26), DisplayName(item), small, IsSpecial(item) ? (Color?)DarkGoldColor : null);
            Text(new Rect(47, row.y + 29, row.width - 49, 20), (item.Location == "equipped" ? "穿戴 " + item.Slot : "背包 " + (item.Slot + 1)) + (item.SocketCount > 0 ? " · " + item.SocketCount + "窍" : "") + (item.Locked ? " · 已锁" : ""), small);
            if (row.Contains(Event.current.mousePosition)) ShowPanelTooltip(OwnedItemTooltip(item));
            if (row.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0) { Select(item); Event.current.Use(); }
        }
        if (rows.Count == 0) Text(new Rect(10, 15, area.width - 25, 85), State.InventorySnapshotCurrent ? "尚无可用器物。试炼可获得洞天符钥；向龙城八仙使者问询演劫洞天。" : "正在等待服务器器物星图…", small);
        GUI.EndScrollView();
    }
}
}

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
    private static string DisplayName(EndgameItemView item) => Formation(item.FormationID)?.Name ?? item.Name;
    private EndgameItemView Selected()
    {
        foreach (var item in State.Items) if (item.UID == selectedUID && MatchesNative(item)) return item;
        return null;
    }
    internal static bool MatchesNative(EndgameItemView item)
    {
        if (item == null) return false;
        if (item.Location == "bag") return InventoryDlg.ItemAt(item.Tab, item.Slot).id == item.ItemID;
        if (item.Location == "equipped") foreach (var row in EquipDlg.Equipped) if (row.slot == item.Slot && row.id == item.ItemID) return true;
        return false;
    }
    private static bool IsRune(EndgameItemView item) => item.ItemID >= 1900028001 && item.ItemID <= 1900028028;
    private void Select(EndgameItemView item)
    {
        if (State.Pending) return;
        if (IsRune(item) || item.SocketCount > 0) LoadCatalog("runes");
        selectedUID = item.UID; socket = 0; selectedRuneUID = ""; orderedRunes.Clear(); quotedInputs = null; State.InvalidateQuote(); detailScroll = Vector2.zero;
    }
    private static void Icon(Rect rect, int itemID, bool unique = false)
    {
        ArtworkSkin.Panel(rect, highlighted: unique);
        Texture2D icon = NetClient.LoadItemIcon(itemID);
        if (icon != null) GUI.DrawTexture(new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6), icon, ScaleMode.ScaleToFit);
    }
    private void DrawItemList(Rect rect, bool includeRunes = false)
    {
        ArtworkSkin.Panel(rect);
        Text(new Rect(rect.x + 10, rect.y + 7, rect.width - 20, 22), "随身器物 · 穿戴", small);
        var rows = new List<EndgameItemView>();
        foreach (var item in State.Items) if (MatchesNative(item) && (includeRunes || !IsRune(item))) rows.Add(item);
        Rect area = new Rect(rect.x + 5, rect.y + 34, rect.width - 10, rect.height - 39);
        itemScroll = GUI.BeginScrollView(area, itemScroll, new Rect(0, 0, area.width - 20, Math.Max(area.height, rows.Count * 57)));
        for (int i = 0; i < rows.Count; i++)
        {
            EndgameItemView item = rows[i]; Rect row = new Rect(0, i * 57, area.width - 22, 54);
            ArtworkSkin.Row(row, highlighted: item.UID == selectedUID);
            Icon(new Rect(6, row.y + 7, 36, 36), item.ItemID, item.UniqueID > 0);
            Text(new Rect(47, row.y + 4, row.width - 49, 26), DisplayName(item), small, IsSpecial(item) ? (Color?)DarkGoldColor : null);
            Text(new Rect(47, row.y + 29, row.width - 49, 20), (item.Location == "equipped" ? "穿戴 " + item.Slot : "背包 " + (item.Slot + 1)) + (item.SocketCount > 0 ? " · " + item.SocketCount + "窍" : "") + (item.Locked ? " · 已锁" : ""), small);
            if (row.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0) { Select(item); Event.current.Use(); }
        }
        if (rows.Count == 0) Text(new Rect(10, 15, area.width - 25, 85), State.HasSnapshot ? "尚无可用器物。试炼可获得洞天符钥；向妖魔结界守卫问询演劫洞天。" : "正在等待服务器器物星图…", small);
        GUI.EndScrollView();
    }
    private void DrawEquipment()
    {
        if (ArtworkSkin.Button(new Rect(23, 140, 210, 29), showCatalog ? "返回随身器物" : "翻阅仙遗图鉴"))
        { showCatalog = !showCatalog; catalogScroll = Vector2.zero; if (showCatalog) LoadCatalog("uniques"); }
        if (showCatalog) { DrawUniqueCatalog(); return; }
        DrawItemList(new Rect(20, 180, 247, 408), true);
        EndgameItemView item = Selected(); ArtworkSkin.Panel(new Rect(280, 141, 638, 447));
        if (item == null)
        { Text(new Rect(305, 173, 578, 110), "选择左侧已有器物或星篆，查看特性、孔位与下一步。\n仙遗可由洞天寻得；巧匠或左慈的周天鼎可淬器成真，能否炼化以问鼎预览为准。", label); return; }
        DrawItemDetail(item, new Rect(300, 157, 596, 359));
        bool usable = !State.Pending && !item.Locked;
        if (ArtworkSkin.Button(new Rect(302, 535, 165, 31), item.Location == "equipped" ? "卸下器物" : "穿戴器物", enabled: usable && !IsRune(item)))
        { if (item.Location == "equipped") client.UnequipEndgameItem(item); else client.EquipEndgameItem(item); }
        if (ArtworkSkin.Button(new Rect(484, 535, 164, 31), "调理星篆", enabled: usable))
        {
            tab = 1; showFormationCatalog = showRuneCatalog = false; EnsureCatalogs();
            if (IsRune(item))
            {
                selectedRuneUID = item.UID; selectedUID = ""; socket = 0;
                foreach (var gear in State.Items)
                    if (!IsRune(gear) && gear.SocketCount > 0 && !gear.Locked && MatchesNative(gear)) { selectedUID = gear.UID; break; }
            }
        }
        if (ArtworkSkin.Button(new Rect(666, 535, 210, 31), "送往周天鼎", enabled: usable && item.Location == "bag")) { tab = 2; EnsureCatalogs(); }
    }
    private void DrawItemDetail(EndgameItemView item, Rect rect)
    {
        Icon(new Rect(rect.x, rect.y, 60, 60), item.ItemID, item.UniqueID > 0);
        Text(new Rect(rect.x + 73, rect.y, rect.width - 73, 32), DisplayName(item), heading, IsSpecial(item) ? DarkGoldColor : Jade);
        Text(new Rect(rect.x + 73, rect.y + 36, rect.width - 73, 24), (IsRune(item) ? "星篆 · 可镶入器物空窍" : "需求 " + item.Level + "级 · " + ProfessionNames(item.Profession)) + (item.Bound ? " · 已绑定" : ""), small);
        string detail = ItemDescription(item, false);
        Rect area = new Rect(rect.x, rect.y + 73, rect.width, rect.height - 73);
        float height = Math.Max(area.height, label.CalcHeight(new GUIContent(detail), area.width - 24) + 12);
        detailScroll = GUI.BeginScrollView(area, detailScroll, new Rect(0, 0, area.width - 20, height));
        Text(new Rect(4, 3, area.width - 28, height), detail, label); GUI.EndScrollView();
    }
    private void DrawUniqueCatalog()
    {
        ArtworkSkin.Input(new Rect(282, 141, 462, 29)); search = GUI.TextField(new Rect(288, 144, 450, 23), search, 64, textField);
        var rows = new List<EndgameItemView>();
        foreach (var row in State.Uniques) if (search.Length == 0 || (row.Name + ProfessionNames(row.Profession)).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) rows.Add(row);
        Rect area = new Rect(22, 181, 894, 369);
        catalogScroll = GUI.BeginScrollView(area, catalogScroll, new Rect(0, 0, 872, Math.Max(area.height, rows.Count * 94)));
        for (int i = 0; i < rows.Count; i++)
        {
            var item = rows[i]; float y = i * 94; ArtworkSkin.Panel(new Rect(0, y, 868, 89)); Icon(new Rect(9, y + 13, 57, 57), item.ItemID, true);
            Text(new Rect(81, y + 8, 770, 25), item.Name + " · " + ProfessionNames(item.Profession) + " · " + item.Level + "级", label, DarkGoldColor);
            Text(new Rect(81, y + 37, 770, 49), item.Trait.Length > 0 ? item.Trait : item.Description, small);
            if (new Rect(0, y, 868, 89).Contains(Event.current.mousePosition)) ShowPanelTooltip("\u0007" + DarkGold(item.Name) + "\n" + ItemDescription(item, true));
        }
        GUI.EndScrollView(); MoreCatalog("uniques", new Rect(669, 559, 240, 27));
        Text(new Rect(28, 560, 620, 27), "已载 " + State.Uniques.Count + " 件 · 搜索已载名称与职业", small);
    }
}
}

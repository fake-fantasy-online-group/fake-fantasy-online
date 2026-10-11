using System;
using System.Collections.Generic;
using System.Text;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    private bool showFormationCatalog;
    private void DrawRunes()
    {
        DrawItemList(new Rect(20, 142, 246, 444));
        if (showRuneCatalog) { DrawRuneDiscovery(); return; }
        if (showFormationCatalog) { DrawFormationCatalog(); return; }
        EndgameItemView item = Selected();
        ArtworkSkin.Panel(new Rect(279, 142, 639, 444));
        Text(new Rect(297, 151, 598, 32), item == null ? "星篆有序，循宿成阵" : item.Name, heading);
        Text(new Rect(298, 187, 596, 38), "先选器物，再选空窍与背包星篆；悬停查看星篆属性。取出保留原星篆，宿序从左到右。", small);
        for (int index = 0; index < 5; index++)
        {
            EndgameSocketView installed = null; if (item != null) foreach (var row in item.Sockets) if (row.Index == index) installed = row;
            string name = item == null || index >= item.SocketCount ? "未开窍" : installed == null || installed.ItemID == 0 ? "空窍" : installed.Name.Length > 0 ? installed.Name : RuneName(installed.ItemID);
            Rect cell = new Rect(298 + index * 117, 232, 110, 47);
            if (ArtworkSkin.Tab(cell, (index + 1) + " · " + name, enabled: item != null && index < item.SocketCount && !State.Pending, selected: socket == index)) socket = index;
            if (installed != null && installed.ItemID > 0 && cell.Contains(Event.current.mousePosition))
                ShowPanelTooltip(name + "\n" + RuneDetails(installed.ItemID, false) + "\n此为第 " + (index + 1) + " 窍，顺序参与宿阵判定。");
        }
        DrawRuneBank(new Rect(297, 292, 594, 112), false);
        bool can = item != null && !item.Locked && !State.Pending && socket < item.SocketCount;
        bool occupied = false; if (item != null) foreach (var row in item.Sockets) if (row.Index == socket && row.ItemID > 0) occupied = true;
        if (ArtworkSkin.Button(new Rect(298, 414, 174, 29), "嵌入所选星篆", enabled: can && !occupied && selectedRuneUID.Length > 0))
            Send(new EndgameRequest("socket.insert", State.Revision, item.UID, selectedRuneUID, socket));
        if (ArtworkSkin.Button(new Rect(486, 414, 151, 29), "取出此孔星篆", enabled: can && occupied))
            Send(new EndgameRequest("socket.remove", State.Revision, item.UID, socket: socket));
        if (ArtworkSkin.Button(new Rect(652, 414, 239, 29), "周天鼎 · 开窍 / 调序", enabled: item != null && !State.Pending)) { tab = 2; recipeID = 1006; quotedInputs = null; State.InvalidateQuote(); EnsureCatalogs(); }
        DrawFormationDiscovery(new Rect(297, 454, 594, 116));
    }
    private void DrawRuneBank(Rect rect, bool multiple)
    {
        var runes = new List<EndgameItemView>();
        foreach (var item in State.Items) if (item.Location == "bag" && IsRune(item) && MatchesNative(item)) runes.Add(item);
        ArtworkSkin.Panel(rect);
        runeScroll = GUI.BeginScrollView(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8), runeScroll,
            new Rect(0, 0, rect.width - 29, Math.Max(rect.height - 8, ((runes.Count + 2) / 3) * 43)));
        for (int i = 0; i < runes.Count; i++)
        {
            EndgameItemView rune = runes[i]; bool chosen = multiple ? orderedRunes.Contains(rune.UID) : selectedRuneUID == rune.UID;
            string prefix = multiple && chosen ? (orderedRunes.IndexOf(rune.UID) + 1) + "·" : "";
            Rect cell = new Rect((i % 3) * 185, (i / 3) * 43, 178, 37);
            if (cell.Contains(Event.current.mousePosition)) ShowPanelTooltip(rune.Name + "\n" + ItemDescription(rune, true));
            if (ArtworkSkin.Button(cell, prefix + rune.Name, enabled: !State.Pending && !rune.Locked, selected: chosen))
            {
                State.InvalidateQuote(); quotedInputs = null;
                if (multiple) { if (!orderedRunes.Remove(rune.UID) && orderedRunes.Count < 5) orderedRunes.Add(rune.UID); }
                else selectedRuneUID = rune.UID;
            }
        }
        if (runes.Count == 0) Text(new Rect(10, 13, rect.width - 40, 55), "背包暂无星篆。演劫洞天可寻得星篆，周天鼎可凝髓成篆。", small);
        GUI.EndScrollView();
    }
    private string RuneName(int id)
    {
        foreach (var row in State.Runes) if (row.ItemID == id) return row.Name;
        return "星篆 " + id;
    }
    private void DrawFormationDiscovery(Rect rect)
    {
        EndgameItemView item = Selected();
        var text = new StringBuilder(item != null && item.FormationID > 0 ? FormationStatus(item) : "成阵须孔数、宿序与部位完全吻合。穿戴后才判定生效。");
        text.Append("\n同类宿阵只生效一个；未成阵也保留星篆自身属性。\n当前生效：");
        if (State.ActiveFormations.Count == 0) text.Append("无");
        for (int i = 0; i < State.ActiveFormations.Count; i++) text.Append(i == 0 ? "" : "、").Append(State.ActiveFormations[i].Name);
        Rect area = new Rect(rect.x, rect.y, rect.width - 131, rect.height);
        float height = Math.Max(area.height, small.CalcHeight(new GUIContent(text.ToString()), area.width - 22));
        catalogScroll = GUI.BeginScrollView(area, catalogScroll, new Rect(0, 0, area.width - 20, height));
        Text(new Rect(3, 2, area.width - 25, height), text.ToString(), small); GUI.EndScrollView();
        if (ArtworkSkin.Button(new Rect(rect.xMax - 123, rect.y, 120, 27), "宿阵图鉴"))
        { showFormationCatalog = true; catalogScroll = Vector2.zero; LoadCatalog("formations"); }
        if (ArtworkSkin.Button(new Rect(rect.xMax - 123, rect.y + 38, 120, 27), "二十八宿图鉴"))
        { showRuneCatalog = true; catalogScroll = Vector2.zero; LoadCatalog("runes"); }
    }
    private static string FormationSlotName(int slot)
    {
        switch (slot)
        {
            case 1: return "面饰"; case 2: return "头部"; case 3: return "项链"; case 4: return "单手武器";
            case 5: return "盾"; case 6: return "手套"; case 7: return "戒指"; case 8: return "衣服";
            case 9: return "鞋"; case 13: return "双手武器"; default: return "部位 " + slot;
        }
    }
    private string FormationDescription(EndgameFormationView formation)
    {
        var text = new StringBuilder(formation.Name).Append(" · ").Append(formation.Channel).Append(" · ").Append(formation.MinLevel).Append("级 · ")
            .Append(formation.Professions == 0 ? "不限职业" : ProfessionNames(formation.Professions));
        text.Append("\n部位：");
        for (int i = 0; i < formation.Slots.Count; i++) text.Append(i == 0 ? "" : "/").Append(FormationSlotName(formation.Slots[i]));
        text.Append("\n");
        // Ownership is informational: it is not a client-side activation prediction.
        var available = new Dictionary<int, int>();
        foreach (var item in State.Items)
            if (item.Location == "bag" && IsRune(item) && !item.Locked && MatchesNative(item))
                available[item.ItemID] = (available.TryGetValue(item.ItemID, out int count) ? count : 0) + 1;
        for (int i = 0; i < formation.Runes.Count; i++)
        {
            int id = formation.Runes[i]; int count = available.TryGetValue(id, out int owned) ? owned : 0;
            text.Append(i == 0 ? "" : " → ").Append(RuneName(id)).Append(count > 0 ? "（背包有）" : "（背包缺）");
            if (count > 0) available[id] = count - 1;
        }
        text.Append('\n').Append(formation.Description);
        foreach (var stat in formation.Stats) text.Append('\n').Append(StatText(stat));
        return text.ToString();
    }
    private void DrawFormationCatalog()
    {
        ArtworkSkin.Panel(new Rect(279, 142, 639, 444), highlighted: true);
        Text(new Rect(298, 154, 480, 29), "宿阵图鉴 · 依器寻序", heading);
        if (ArtworkSkin.Button(new Rect(818, 154, 75, 27), "返回")) showFormationCatalog = false;
        Text(new Rect(298, 191, 588, 42), "标记仅统计背包未锁定星篆，不含已嵌星篆。成阵后仍须穿戴并满足职业、等级、耐久与同类优先级。", small);
        var descriptions = new List<string>(); var heights = new List<float>(); float total = 0;
        foreach (var formation in State.Formations)
        {
            string description = FormationDescription(formation); descriptions.Add(description);
            float height = small.CalcHeight(new GUIContent(description), 554) + 18; heights.Add(height); total += height;
        }
        Rect area = new Rect(298, 239, 591, 288);
        catalogScroll = GUI.BeginScrollView(area, catalogScroll, new Rect(0, 0, 569, Math.Max(area.height, total)));
        float y = 0;
        for (int i = 0; i < descriptions.Count; i++) { Text(new Rect(3, y, 554, heights[i]), descriptions[i], small); y += heights[i]; }
        if (descriptions.Count == 0) Text(new Rect(3, 4, 554, 40), State.CatalogRequested("formations") ? "正在翻阅宿阵图鉴…" : "暂无已载宿阵，可刷新星图后重试。", small);
        GUI.EndScrollView(); MoreCatalog("formations", new Rect(654, 544, 235, 28));
    }
    private void DrawRuneDiscovery()
    {
        ArtworkSkin.Panel(new Rect(279, 142, 639, 444), highlighted: true);
        Text(new Rect(298, 154, 480, 29), "二十八宿 · 星篆图鉴", heading);
        if (ArtworkSkin.Button(new Rect(818, 154, 75, 27), "返回")) showRuneCatalog = false;
        Rect area = new Rect(298, 193, 591, 335);
        catalogScroll = GUI.BeginScrollView(area, catalogScroll, new Rect(0, 0, 569, Math.Max(area.height, State.Runes.Count * 112)));
        for (int i = 0; i < State.Runes.Count; i++)
        {
            var rune = State.Runes[i]; Icon(new Rect(0, i * 112 + 8, 42, 42), rune.ItemID);
            Text(new Rect(52, i * 112 + 2, 510, 25), rune.Name + " · " + rune.Quadrant, label, Jade);
            var description = new StringBuilder(rune.Description);
            foreach (var stat in rune.Stats) description.Append('\n').Append(StatText(stat));
            Text(new Rect(52, i * 112 + 29, 510, 79), description.ToString(), small);
        }
        GUI.EndScrollView(); MoreCatalog("runes", new Rect(654, 544, 235, 28));
    }
}
}

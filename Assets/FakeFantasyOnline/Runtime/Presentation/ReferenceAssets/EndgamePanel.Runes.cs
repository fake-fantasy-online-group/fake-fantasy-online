using System;
using System.Collections.Generic;
using System.Text;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    private void DrawRunes()
    {
        DrawItemList(new Rect(20, 142, 246, 444)); EndgameItemView item = Selected();
        ArtworkSkin.Panel(new Rect(279, 142, 639, 444));
        Text(new Rect(297, 151, 598, 32), item == null ? "星篆有序，循宿成阵" : item.Name, heading);
        Text(new Rect(298, 187, 596, 38), "先选器物，再选孔位与背包星篆。取出保留原星篆，宿阵按孔位从左到右判定。", small);
        for (int index = 0; index < 5; index++)
        {
            EndgameSocketView installed = null; if (item != null) foreach (var row in item.Sockets) if (row.Index == index) installed = row;
            string name = item == null || index >= item.SocketCount ? "未开窍" : installed == null || installed.ItemID == 0 ? "空窍" : installed.Name.Length > 0 ? installed.Name : RuneName(installed.ItemID);
            if (ArtworkSkin.Tab(new Rect(298 + index * 117, 232, 110, 47), (index + 1) + " · " + name, enabled: item != null && index < item.SocketCount && !State.Pending, selected: socket == index)) socket = index;
        }
        DrawRuneBank(new Rect(297, 292, 594, 112), false);
        bool can = item != null && !item.Locked && !State.Pending && socket < item.SocketCount;
        if (ArtworkSkin.Button(new Rect(298, 414, 174, 29), "嵌入所选星篆", enabled: can && selectedRuneUID.Length > 0))
            Send(new EndgameRequest("socket.insert", State.Revision, item.UID, selectedRuneUID, socket));
        bool occupied = false; if (item != null) foreach (var row in item.Sockets) if (row.Index == socket && row.ItemID > 0) occupied = true;
        if (ArtworkSkin.Button(new Rect(486, 414, 151, 29), "取出此孔星篆", enabled: can && occupied))
            Send(new EndgameRequest("socket.remove", State.Revision, item.UID, socket: socket));
        if (ArtworkSkin.Button(new Rect(652, 414, 239, 29), "周天鼎 · 开窍 / 调序", enabled: item != null && !State.Pending)) { tab = 2; recipeID = 1006; State.InvalidateQuote(); }
        DrawFormationDiscovery(new Rect(297, 454, 594, 116));
        if (showRuneCatalog) DrawRuneDiscovery();
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
            if (ArtworkSkin.Button(new Rect((i % 3) * 185, (i / 3) * 43, 178, 37), prefix + rune.Name, enabled: !State.Pending && !rune.Locked, selected: chosen))
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
        var text = new StringBuilder("当前生效宿阵：");
        if (State.ActiveFormations.Count == 0) text.Append("无");
        foreach (var active in State.ActiveFormations) text.Append(active.Name).Append(" · ");
        text.Append("\n\n宿阵图鉴 · 次序必须相同\n");
        foreach (var formation in State.Formations)
        {
            text.Append(formation.Name).Append("（").Append(formation.Channel).Append(" / ").Append(formation.MinLevel).Append("级 / ").Append(ProfessionNames(formation.Professions)).Append("）：");
            for (int i = 0; i < formation.Runes.Count; i++) text.Append(i == 0 ? "" : " → ").Append(RuneName(formation.Runes[i]));
            text.Append('\n').Append(formation.Description);
            foreach (var stat in formation.Stats) text.Append('\n').Append(StatText(stat));
            text.Append("\n\n");
        }
        Rect area = new Rect(rect.x, rect.y, rect.width - 131, rect.height);
        float height = Math.Max(area.height, small.CalcHeight(new GUIContent(text.ToString()), area.width - 22));
        catalogScroll = GUI.BeginScrollView(area, catalogScroll, new Rect(0, 0, area.width - 20, height));
        Text(new Rect(3, 2, area.width - 25, height), text.ToString(), small); GUI.EndScrollView();
        MoreCatalog("formations", new Rect(rect.xMax - 123, rect.y, 120, 27));
        if (ArtworkSkin.Button(new Rect(rect.xMax - 123, rect.y + 38, 120, 27), "二十八宿图鉴")) { showRuneCatalog = true; catalogScroll = Vector2.zero; }
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

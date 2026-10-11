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
        DrawItemList(new Rect(20, 142, 246, 444));
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
            if (cell.Contains(Event.current.mousePosition)) ShowPanelTooltip(OwnedItemTooltip(rune));
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
        text.Append("\n宿阵须用无原生额外词条的装备；不看颜色，基础攻防与后加星篆、精炼不计。");
        text.Append("\n同类宿阵只生效一个；装备满足星篆等级要求后，自身属性生效。\n当前生效：");
        if (State.ActiveFormations.Count == 0) text.Append("无");
        for (int i = 0; i < State.ActiveFormations.Count; i++) text.Append(i == 0 ? "" : "、").Append(State.ActiveFormations[i].Name);
        Rect area = rect;
        float height = Math.Max(area.height, small.CalcHeight(new GUIContent(text.ToString()), area.width - 22));
        detailScroll = GUI.BeginScrollView(area, detailScroll, new Rect(0, 0, area.width - 20, height));
        Text(new Rect(3, 2, area.width - 25, height), text.ToString(), small); GUI.EndScrollView();
    }
}
}

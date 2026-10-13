using System;
using System.Collections.Generic;
using System.Text;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
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
        for (int i = 0; i < State.ActiveFormations.Count; i++) text.Append(i == 0 ? "" : "、").Append(FormationName(State.ActiveFormations[i].Name));
        Rect area = rect;
        float height = Math.Max(area.height, small.CalcHeight(new GUIContent(text.ToString()), area.width - 22));
        detailScroll = GUI.BeginScrollView(area, detailScroll, new Rect(0, 0, area.width - 20, height));
        Text(new Rect(3, 2, area.width - 25, height), text.ToString(), small); GUI.EndScrollView();
    }
}
}

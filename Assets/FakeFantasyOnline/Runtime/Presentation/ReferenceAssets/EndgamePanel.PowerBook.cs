using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    private string selectedPowerID = "", observedPowerImprint = "";
    private Vector2 powerScroll;
    internal static bool TryOpenPowerBookFromWorld() => WorldEntryAvailable && Current.State.PowerBookAvailable &&
        !SysMsgBox.Visible && TryOpenWorldView(EndgameModule.PowerBook);
    private EndgamePowerRecordView SelectedPower()
    {
        foreach (var power in State.Powers) if (power.PowerID == selectedPowerID) return power;
        return null;
    }
    private static bool PowerBookTarget(EndgameItemView item) => item != null && item.Location == "bag" && item.Legendary != null && !item.Locked;
    private bool CanImprintPower() => State.PowerBookAvailable && State.PowerBookCurrent && State.InventorySnapshotCurrent &&
        !State.PowerBookLoading && !State.Pending && SelectedPower() != null && PowerBookTarget(Selected());
    private void ImprintPower()
    {
        if (!CanImprintPower()) return;
        var request = new EndgameRequest("powerbook.imprint", State.Revision, targetUID: selectedUID, powerID: selectedPowerID);
        Send(request);
        if (State.PendingRequest?.RequestID == request.RequestID) observedPowerImprint = request.RequestID;
    }
    private void ObservePowerBookRequest()
    {
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
        return power.Description.StartsWith(prefix, StringComparison.Ordinal) ? power.Description : prefix + power.Description;
    }
    private void DrawPowerBook()
    {
        ArtworkSkin.Panel(new Rect(0, 0, W, H));
        Text(new Rect(24, 15, W - 80, 32), "威能录", heading);
        if (ArtworkSkin.Button(new Rect(W - 48, 18, 27, 27), "×")) { Close(); return; }
        Text(new Rect(26, 53, W - 52, 38), "分解暗金铭记威能，较高数值替换旧录。刻印仅覆盖威能，装备其余属性不变。", small);
        DrawPowerRecords(new Rect(24, 98, 360, 470));
        DrawPowerTargets(new Rect(400, 98, 516, 260));
        var power = SelectedPower();
        var target = Selected();
        ArtworkSkin.Panel(new Rect(400, 370, 516, 198));
        string description = power == null ? "先选择已铭记的威能，再选择背包中的暗金装备。" : PowerDescription(power);
        float detailHeight = Math.Max(140, label.CalcHeight(new GUIContent(description), 464));
        detailScroll = GUI.BeginScrollView(new Rect(414, 382, 488, 140), detailScroll, new Rect(0, 0, 464, detailHeight));
        Text(new Rect(0, 0, 464, detailHeight), description, label, power == null ? (Color?)null : DarkGoldColor);
        GUI.EndScrollView();
        if (power != null) Text(new Rect(414, 526, 488, 30), power.Class + " · " + power.Category + " · 来源等级 " + power.MinLevel + " 起", small);
        Text(new Rect(26, 574, W - 52, 25), "铭记提升后，已有装备需重新刻印。无需材料。", small);
        string status = State.Pending ? "上一笔操作结果确认中，请勿重复操作。" : State.PowerBookLoading ? "正在翻阅威能录…" : State.Error.Length > 0 ? State.Message :
            !State.InventorySnapshotCurrent || !State.PowerBookCurrent ? "资料已有变化，请刷新后刻印。" : target != null ? "当前装备：" + target.Name : "";
        Text(new Rect(26, 608, 510, 35), status, small, State.Error.Length > 0 ? new Color(1f, .6f, .48f) : Jade);
        if (ArtworkSkin.Button(new Rect(548, 607, 100, 30), "刷新", enabled: !State.PowerBookLoading)) RefreshPowerBook();
        if (ArtworkSkin.Button(new Rect(664, 607, 220, 30), State.Pending ? "查询原请求" : "刻印威能", enabled: State.Pending || CanImprintPower()))
        {
            if (State.Pending) client.RetryEndgame(); else ImprintPower();
        }
    }
    private void DrawPowerRecords(Rect rect)
    {
        ArtworkSkin.Panel(rect);
        Text(new Rect(rect.x + 10, rect.y + 7, rect.width - 20, 24), "已铭记威能", label);
        Rect area = new Rect(rect.x + 6, rect.y + 38, rect.width - 12, rect.height - 82);
        powerScroll = GUI.BeginScrollView(area, powerScroll, new Rect(0, 0, area.width - 20, Math.Max(area.height, State.Powers.Count * 55)));
        for (int i = 0; i < State.Powers.Count; i++)
        {
            var power = State.Powers[i]; Rect row = new Rect(0, i * 55, area.width - 22, 52);
            ArtworkSkin.Row(row, highlighted: power.PowerID == selectedPowerID);
            Text(new Rect(8, row.y + 3, row.width - 16, 24), power.Name, label, DarkGoldColor);
            Text(new Rect(8, row.y + 28, row.width - 16, 21), power.Class + " · " + power.Category, small);
            if (!State.Pending && row.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            { selectedPowerID = power.PowerID; detailScroll = Vector2.zero; Event.current.Use(); }
        }
        if (State.PowerBookLoaded && State.Powers.Count == 0) Text(new Rect(8, 12, area.width - 25, 70), "尚未铭记威能。分解暗金装备后，即可在此查阅。", small);
        GUI.EndScrollView();
        if (ArtworkSkin.Button(new Rect(rect.x + 10, rect.yMax - 35, rect.width - 20, 25), "更多威能", enabled: State.PowerBookCurrent && !State.PowerBookLoading && State.PowerBookNextCursor.Length > 0))
            Send(new EndgameRequest("powerbook.list", cursor: State.PowerBookNextCursor));
    }
    private void DrawPowerTargets(Rect rect)
    {
        ArtworkSkin.Panel(rect);
        Text(new Rect(rect.x + 10, rect.y + 7, rect.width - 20, 24), "选择背包暗金", label);
        var rows = new List<EndgameItemView>();
        foreach (var item in State.Items) if (PowerBookTarget(item) && MatchesNative(item)) rows.Add(item);
        Rect area = new Rect(rect.x + 6, rect.y + 38, rect.width - 12, rect.height - 44);
        itemScroll = GUI.BeginScrollView(area, itemScroll, new Rect(0, 0, area.width - 20, Math.Max(area.height, rows.Count * 51)));
        for (int i = 0; i < rows.Count; i++)
        {
            var item = rows[i]; Rect row = new Rect(0, i * 51, area.width - 22, 48);
            ArtworkSkin.Row(row, highlighted: item.UID == selectedUID);
            Icon(new Rect(5, row.y + 5, 36, 36), item.ItemID, true);
            Text(new Rect(48, row.y + 2, row.width - 54, 23), item.Name, label, DarkGoldColor);
            Text(new Rect(48, row.y + 25, row.width - 54, 21), item.Legendary.Name, small);
            if (row.Contains(Event.current.mousePosition)) ShowPanelTooltip(OwnedItemTooltip(item));
            if (!State.Pending && row.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            { selectedUID = item.UID; Event.current.Use(); }
        }
        if (rows.Count == 0) Text(new Rect(8, 12, area.width - 25, 70), State.InventorySnapshotCurrent ? "背包中暂无未锁定的暗金装备。" : "正在同步背包…", small);
        GUI.EndScrollView();
    }
}
}

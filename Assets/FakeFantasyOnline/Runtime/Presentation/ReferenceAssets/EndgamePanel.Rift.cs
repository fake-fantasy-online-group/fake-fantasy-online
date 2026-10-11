using System;
using System.Text;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    private string tierInput = "1";
    private void DrawRift()
    {
        ArtworkSkin.Panel(new Rect(20, 142, 898, 444)); EndgameRiftView rift = State.Rift;
        Text(new Rect(44, 158, 570, 37), "演劫洞天", heading);
        Text(new Rect(625, 166, 255, 29), "洞天符钥：" + (rift == null ? "查询中" : rift.KeyCount.ToString()) + " 把", label, Gold);
        Text(new Rect(45, 203, 827, 52), "四村试炼小仙接取试炼，完成领钥后可传送至妖魔结界守卫。\n队友须在队长附近；一钥开启一场，多层共用进度与倒计时。", label);
        bool selectedTierValid = true;
        if (rift != null && rift.RunID.Length > 0 && (PollRun(rift) || rift.Claimable || rift.Phase == "interrupted"))
        {
            Text(new Rect(45, 264, 826, 34), rift.Name + " · " + RiftDepth(rift) + " · " + PhaseName(rift.Phase), heading);
            Text(new Rect(45, 300, 825, 26), (rift.MapName.Length > 0 ? rift.MapName : "洞天地图") + (ActiveRun(rift) ? " · 余时 " + Remaining(rift) : ""), label, Jade);
            DrawProgress(new Rect(46, 334, 825, 24), rift.Progress, rift.ProgressMax);
            Text(new Rect(45, 365, 808, 29), RiftObjective(rift), label);
            if (rift.Phase == "active" || rift.Transitioning)
            {
                if (ArtworkSkin.Button(new Rect(45, 404, 240, 32), rift.Transitioning ? "正在前往下一层…" : "前往下一层 · 跳过余敌", enabled: CanAdvanceRift(rift))) AdvanceRift(rift);
                Text(new Rect(301, 400, 570, 43), "开启者靠近出口后可换层；可跳过难打的敌群。\n阶数不变，跳过余敌，未拾灵珠不计入进度。", small);
            }
        }
        else
        {
            int maximum = rift != null && rift.MaxTier > 0 ? rift.MaxTier : 150;
            Text(new Rect(45, 265, 825, 32), "选择劫数 · 第 " + tier + " 阶（1–" + maximum + " 阶）", heading);
            bool editable = !State.Pending;
            if (ArtworkSkin.Button(new Rect(45, 310, 66, 32), "−10", enabled: editable && tier > 1)) SetTier(Math.Max(1, tier - 10));
            if (ArtworkSkin.Button(new Rect(121, 310, 45, 32), "−", enabled: editable && tier > 1)) SetTier(tier - 1);
            string input = GUI.TextField(new Rect(179, 311, 94, 31), tierInput, 3, textField);
            if (editable) tierInput = input;
            selectedTierValid = int.TryParse(tierInput, out int selectedTier) && selectedTier >= 1 && selectedTier <= maximum;
            if (selectedTierValid) tier = selectedTier;
            if (ArtworkSkin.Button(new Rect(286, 310, 45, 32), "+", enabled: editable && tier < maximum)) { SetTier(tier + 1); selectedTierValid = true; }
            if (ArtworkSkin.Button(new Rect(341, 310, 66, 32), "+10", enabled: editable && tier < maximum)) { SetTier(Math.Min(maximum, tier + 10)); selectedTierValid = true; }
            bool unlocked = rift == null || rift.UnlockedTier == 0 || tier <= rift.UnlockedTier;
            Text(new Rect(426, 313, 445, 29), !selectedTierValid ? "请输入 1–" + maximum + " 的整数阶数" : !unlocked ? "尚未解锁；当前可选至 " + rift.UnlockedTier + " 阶" : "阶数决定难度，层数是本场探索进度", small, selectedTierValid && unlocked ? Jade : Gold);
            selectedTierValid &= unlocked;
            Text(new Rect(45, 360, 826, 73), "每场随机安排地图、敌群与出口。可去下一层寻找合适敌群，镇煞至满后劫影现身。\n换层不升阶、不重置时间，也不额外耗钥。败退、离场不会自动返钥。", small);
        }
        if (rift != null)
        {
            var text = new StringBuilder("同道：");
            foreach (var member in rift.Members) text.Append(member.Name.Length > 0 ? member.Name : member.CharID).Append('(').Append(member.Level).Append(") ").Append(member.Claimed ? "已领 " : " ");
            Text(new Rect(45, 452, 825, 40), text.ToString(), small);
        }
        bool reenter = CanReenter(rift), waiting = rift != null && (rift.Transitioning || rift.Phase == "reward_pending" || rift.Phase == "interrupted" || rift.Claimable && !reenter);
        if (ArtworkSkin.Button(new Rect(45, 513, 252, 39), reenter ? "进入已有洞天" : "消耗一钥 · 开启洞天", enabled: !State.Pending && !waiting && selectedTierValid && rift != null && (reenter || rift.KeyCount > 0)))
            Send(new EndgameRequest("rift.start", State.Revision, runID: reenter ? rift.RunID : "", tier: reenter ? rift.Tier : tier));
        if (ArtworkSkin.Button(new Rect(314, 513, 254, 39), rift != null && rift.Phase == "interrupted" ? "领取补偿符钥" : "领取我的洞天所得", enabled: !State.Pending && rift != null && rift.Claimable))
            Send(new EndgameRequest("rift.claim", State.Revision, runID: rift.RunID));
        if (ArtworkSkin.Button(new Rect(585, 513, 285, 39), confirmLeave ? "确认离境 · 无自动返钥" : "离开洞天", enabled: !State.Pending && rift != null && rift.RunID.Length > 0 && !rift.Transitioning))
        { if (confirmLeave) { Send(new EndgameRequest("rift.leave", State.Revision, runID: rift.RunID)); confirmLeave = false; } else confirmLeave = true; }
    }
    private void SetTier(int value) { tier = value; tierInput = value.ToString(); }
    private static string RiftDepth(EndgameRiftView rift) => "第 " + rift.Tier + " 阶" + (rift.FloorIndex > 0 ? " · 第 " + rift.FloorIndex + " 层" : "");
    private bool CanAdvanceRift(EndgameRiftView rift) => rift != null && rift.Phase == "active" && rift.CanAdvance && !rift.Transitioning && rift.FloorIndex > 0 && rift.RunID.Length > 0 && !State.Pending && State.HasSnapshot && !State.SnapshotLoading && State.SnapshotRevision == State.Revision;
    private void AdvanceRift(EndgameRiftView rift)
    {
        // Inventory revision guards concurrent economic changes; floorIndex guards navigation.
        // Combat progress may change Rift.Revision and must not invalidate the floor intent.
        if (CanAdvanceRift(rift)) Send(new EndgameRequest("rift.advance", State.Revision, runID: rift.RunID, floorIndex: rift.FloorIndex));
        PlayerController.SuppressWorldClick();
    }
    private static bool ActiveRun(EndgameRiftView rift) => rift != null && (rift.Phase == "reserved" || rift.Phase == "active" || rift.Phase == "boss");
    private static bool CanReenter(EndgameRiftView rift) => ActiveRun(rift) || rift != null && rift.Phase == "completed";
    private static bool PollRun(EndgameRiftView rift) => CanReenter(rift) || rift != null && rift.Phase == "reward_pending";
    private static string PhaseName(string phase)
    {
        switch (phase)
        {
            case "reserved": return "同道集结"; case "active": return "镇煞"; case "boss": return "镇境劫影";
            case "reward_pending": return "战果保存中"; case "completed": return "功成待领"; case "closed": return "洞天已闭";
            case "failed": return "演劫未竟"; case "aborted": return "洞天中断"; case "interrupted": return "洞天中断 · 可领补偿";
            default: return "静候开境";
        }
    }
    private string Remaining(EndgameRiftView rift)
    {
        int seconds = Math.Max(0, rift.RemainingSeconds - (int)Math.Max(0, (DateTime.UtcNow - State.RiftReceivedUtc).TotalSeconds));
        return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
    }
    private void DrawProgress(Rect rect, int progress, int target)
    {
        ArtworkSkin.Panel(rect); float ratio = target > 0 ? Mathf.Clamp01((float)progress / target) : 0;
        Color previous = GUI.color; GUI.color = Jade;
        GUI.DrawTexture(new Rect(rect.x + 3, rect.y + 3, (rect.width - 6) * ratio, rect.height - 6), Texture2D.whiteTexture); GUI.color = previous;
        Text(new Rect(rect.x + 8, rect.y, rect.width - 16, rect.height), progress + " / " + target, small);
    }
    private string observedRun = "", observedPhase = "", announcement = "";
    private long observedGeneration = -1;
    private int observedFloor;
    private float announcementUntil;
    // Phase notices are presentation only. A restored snapshot sets the baseline, never replays a victory.
    private void ObserveRiftPhase()
    {
        if (observedGeneration != State.Generation)
        { observedGeneration = State.Generation; observedRun = observedPhase = announcement = ""; observedFloor = 0; }
        var rift = State.Rift;
        if (rift == null || rift.RunID.Length == 0)
        { observedRun = observedPhase = announcement = ""; observedFloor = 0; return; }
        if (observedRun != rift.RunID) announcement = "";
        bool transition = observedRun == rift.RunID && observedPhase.Length > 0 && observedPhase != rift.Phase;
        bool floorTransition = observedRun == rift.RunID && observedFloor > 0 && rift.FloorIndex > observedFloor;
        if (observedRun != rift.RunID || floorTransition) confirmLeave = false;
        observedRun = rift.RunID; observedPhase = rift.Phase; observedFloor = rift.FloorIndex;
        if (!transition && !floorTransition) return;
        announcement = floorTransition && !transition ? "抵达第 " + rift.FloorIndex + " 层 · " + rift.MapName + " · 难度与余时延续" : RiftObjective(rift);
        announcementUntil = Time.unscaledTime + 6f;
    }
    private static string RiftObjective(EndgameRiftView rift)
    {
        if (rift == null) return "静候开境";
        if (rift.Transitioning) return "正在换层：保留镇煞进度与倒计时，请稍候。";
        switch (rift.Phase)
        {
            case "reserved": return "同道集结：队友选择进入已有洞天。";
            case "active": return rift.CanAdvance ? "可换层：收取灵珠，或从出口去下一层。" : rift.FloorIndex > 0 ? "镇煞：收取灵珠；开启者可靠近出口换层。" : "镇煞：击败敌人，靠近灵珠收取进度。";
            case "boss": return "劫影现身：" + (rift.FloorIndex > 0 ? "前往本层出口，击败" : "击败") + (rift.BossName.Length > 0 ? rift.BossName : "镇境劫影") + "。";
            case "reward_pending": return "劫影已退，战果正在保存；请稍候。";
            case "completed": return rift.Claimable ? "洞天已成：战果已保存，可领取你的所得。" : "洞天已成：奖励资格与领取状态以服务器为准。";
            case "interrupted": return rift.Claimable ? "洞天中断：可领取服务器确认的补偿符钥。" : "洞天中断：请查询服务器结算。";
            case "failed": return rift.Claimable ? "演劫未竟：仍有服务器确认的所得可领取。" : "演劫未竟：本次已结束，可返回守卫再战。";
            case "closed": return rift.Claimable ? "洞天已闭：战果已保存，仍可领取你的所得。" : "洞天已闭：所得请在背包查看，炼化可拜访巧匠左慈。";
            case "aborted": return "洞天已中断，请返回守卫查看后续。";
            default: return "静候开境";
        }
    }
    private void DrawRiftAnnouncement(float scale)
    {
        if (announcement.Length == 0 || Time.unscaledTime >= announcementUntil) return;
        Rect rect = new Rect(Screen.width * .5f - 260 * scale, 202 * scale, 520 * scale, 39 * scale);
        ArtworkSkin.Panel(rect, scale, true);
        BitmapFont.CenterOutline(rect, announcement, Gold, scale);
    }
    private void DrawRiftHud(float scale)
    {
        if (open) return; // The modal view owns overlapping pointer regions.
        var rift = State.Rift;
        if (rift == null || (!PollRun(rift) && !rift.Claimable)) return;
        Rect hud = new Rect(Screen.width * .5f - 225 * scale, 48 * scale, 450 * scale, 143 * scale);
        ArtworkSkin.Panel(hud, scale, true);
        string clock = ActiveRun(rift) ? " · " + Remaining(rift) : "";
        BitmapFont.CenterOutline(new Rect(hud.x, hud.y + 8 * scale, hud.width, 18 * scale), "演劫 " + RiftDepth(rift) + " · " + PhaseName(rift.Phase) + clock, Gold, scale);
        DrawProgress(new Rect(hud.x + 12 * scale, hud.y + 33 * scale, hud.width - 24 * scale, 20 * scale), rift.Progress, rift.ProgressMax);
        BitmapFont.CenterOutline(new Rect(hud.x + 8 * scale, hud.y + 59 * scale, hud.width - 16 * scale, 18 * scale), RiftObjective(rift), Jade, scale);
        if (ArtworkSkin.Button(new Rect(hud.x + 135 * scale, hud.y + 81 * scale, 180 * scale, 24 * scale), rift.Claimable ? "查看战果 · 领取" : "查看洞天", scale))
        {
            generation = State.Generation; tab = 3; open = true; detailScroll = Vector2.zero;
            Refresh(); PlayerController.SuppressWorldClick();
        }
        if (rift.Phase == "active" && rift.FloorIndex > 0)
        {
            if (ArtworkSkin.Button(new Rect(hud.x + 95 * scale, hud.y + 111 * scale, 260 * scale, 24 * scale), rift.Transitioning ? "正在换层…" : "下一层 · 出口 (" + rift.ExitX + ", " + rift.ExitY + ")", scale, enabled: CanAdvanceRift(rift))) AdvanceRift(rift);
        }
        else if (rift.Phase == "boss" && rift.FloorIndex > 0)
        {
            BitmapFont.CenterOutline(new Rect(hud.x + 12 * scale, hud.y + 111 * scale, hud.width - 24 * scale, 24 * scale), "镇境劫影 · 出口 (" + rift.ExitX + ", " + rift.ExitY + ")", Gold, scale);
        }
        if (Event.current.type == EventType.MouseDown && hud.Contains(Event.current.mousePosition))
        { Event.current.Use(); PlayerController.SuppressWorldClick(); }
    }
    private void DrawRiftMarkers(float scale)
    {
        var rift = State.Rift; Camera camera = Camera.main;
        if (rift == null || !ActiveRun(rift) || rift.Transitioning || camera == null || GameBootstrap.CurPpu <= 0) return;
        DrawRiftTelegraphs(rift, camera, scale);
        foreach (var aura in rift.Auras)
        {
            Transform entity = CombatWorld.EndgameMonsterTransform(aura.NativeEntityID); if (entity == null) continue;
            Vector3 point = camera.WorldToScreenPoint(entity.position + Vector3.up * .9f); if (point.z <= 0) continue;
            Color color = aura.Class == 1 ? new Color(.4f, .66f, 1f) : aura.Class == 2 ? Gold : new Color(1f, .45f, .38f);
            string name = aura.Class == 1 ? "◆ 蓝煞" : aura.Class == 2 ? "◆ 金煞" : aura.Class == 3 ? "◆ 镇境劫影" : "";
            if (aura.Affixes.Count > 0) name += " · " + string.Join(" / ", aura.Affixes);
            if (name.Length > 0) BitmapFont.CenterOutline(new Rect(point.x - 150 * scale, Screen.height - point.y, 300 * scale, 18 * scale), name, color, scale);
        }
        if ((rift.Phase == "active" || rift.Phase == "boss") && rift.FloorIndex > 0)
        {
            Vector3 exit = RiftScreenPoint(camera, rift.ExitX, rift.ExitY);
            if (exit.z > 0)
            {
                bool guardian = rift.Phase == "boss";
                Color markerColor = guardian ? Gold : Jade;
                DrawRiftRing(camera, rift.ExitX, rift.ExitY, 45, markerColor, scale);
                BitmapFont.CenterOutline(new Rect(exit.x - 135 * scale, exit.y - 33 * scale, 270 * scale, 24 * scale), guardian ? "◆ 镇境劫影 · 本层出口" : "◇ 下一层出口 · 可跳过余敌", markerColor, scale);
            }
        }
        foreach (var globe in rift.Globes)
        {
            Vector3 point = camera.WorldToScreenPoint(new Vector3(globe.X / GameBootstrap.CurPpu, (GameBootstrap.CurWorldH - globe.Y) / GameBootstrap.CurPpu, 0));
            if (point.z <= 0) continue;
            BitmapFont.CenterOutline(new Rect(point.x - 70 * scale, Screen.height - point.y - 19 * scale, 140 * scale, 18 * scale), "◇ 镇煞灵珠 +" + globe.Value, Jade, scale);
        }
    }
}
public partial class CombatWorld
{
    internal static Transform EndgameMonsterTransform(int id)
    {
        if (I != null && I.monsters.TryGetValue(id, out MonsterEntity entity) && entity != null) return entity.transform;
        return null;
    }
}
}

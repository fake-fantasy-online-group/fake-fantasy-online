using System;
using System.Text;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    private void DrawRift()
    {
        if (!RiftEntryContextAvailable) return;
        EndgameRiftView rift = State.Rift;
        bool selectedTierValid = true;
        bool reenter = CanReenter(rift);
        bool recovery = rift != null && rift.RunID.Length > 0 && (rift.Claimable || rift.Phase == "interrupted" || rift.Phase == "reward_pending");
        if (reenter || recovery)
        {
            Text(new Rect(32, 92, 536, 44), (rift.Name.Length > 0 ? rift.Name : "演劫洞天") + " · 第 " + rift.Tier + " 阶", label, Gold);
            Text(new Rect(32, 145, 536, 48), reenter ? "可重返已有洞天。" : rift.Claimable ? "仍可领取已保存的所得。" : "等待服务器确认。", label);
            Text(new Rect(32, 207, 536, 44), RiftUnlockSummary(rift), small, Jade);
        }
        else
        {
            DrawRiftTierSelector(rift);
            Text(new Rect(32, 92, 300, 32), "选择劫数", heading);
            Text(new Rect(378, 96, 190, 29), "洞天符钥：" + (rift == null ? "查询中" : rift.KeyCount.ToString()) + " 把", label, Gold);
            Text(new Rect(302, 139, 266, 46), RiftUnlockSummary(rift), small, Jade);
            selectedTierValid = rift != null && tier >= 1 && tier <= RiftTierLimit(rift);
            Text(new Rect(32, 199, 536, 64), "开启消耗一把洞天符钥。队友需在开启者附近。\n通关可开放更高阶数，最高150阶。", small);
        }
        if (rift != null && rift.Members.Count > 0)
        {
            var text = new StringBuilder("同道：");
            foreach (var member in rift.Members) text.Append(member.Name.Length > 0 ? member.Name : member.CharID).Append('(').Append(member.Level).Append(") ");
            Text(new Rect(32, 306, 536, 30), text.ToString(), small);
        }
        DrawRiftActions(rift, selectedTierValid);
    }
    private void DrawRiftActions(EndgameRiftView rift, bool selectedTierValid)
    {
        if (!RiftEntryContextAvailable) return;
        bool reenter = CanReenter(rift), waiting = rift != null && (rift.Transitioning || rift.Phase == "reward_pending" || rift.Phase == "interrupted" || rift.Claimable && !reenter);
        bool showStart = reenter || !waiting;
        bool showClaim = rift != null && rift.Claimable && !reenter;
        int count = (showStart ? 1 : 0) + (showClaim ? 1 : 0);
        float width = count > 0 ? Mathf.Min(250f, (536f - (count - 1) * 12f) / count) : 0;
        float x = 32 + (536f - count * width - Math.Max(0, count - 1) * 12f) * .5f;
        if (showStart)
        {
            if (ArtworkSkin.Button(new Rect(x, 339, width, 34), reenter ? "进入已有洞天" : "消耗一钥 · 开启洞天", enabled: (reenter || RiftEntranceCurrent()) && !State.Pending && !waiting && selectedTierValid && rift != null && (reenter || rift.KeyCount > 0)))
                Send(new EndgameRequest("rift.start", State.Revision, runID: reenter ? rift.RunID : "", tier: reenter ? rift.Tier : tier, npcEntityID: reenter ? 0 : riftEntranceEntityID));
            x += width + 12;
        }
        if (showClaim && ArtworkSkin.Button(new Rect(x, 339, width, 34), rift.Phase == "interrupted" ? "领取补偿符钥" : "领取我的洞天所得", enabled: !State.Pending))
            Send(new EndgameRequest("rift.claim", State.Revision, runID: rift.RunID));
    }
    private static string RiftDepth(EndgameRiftView rift) => "第 " + rift.Tier + " 阶" + (rift.FloorIndex > 0 ? " · 第 " + rift.FloorIndex + " 层" : "");
    private bool LiveCompletedRift(EndgameRiftView rift) => State.RiftInInstance && rift != null && (rift.Phase == "completed" || rift.Phase == "closed");
    private static bool ActiveRun(EndgameRiftView rift) => rift != null && (rift.Phase == "reserved" || rift.Phase == "active" || rift.Phase == "boss_arriving" || rift.Phase == "boss");
    private static bool CanReenter(EndgameRiftView rift) => ActiveRun(rift) || rift != null && rift.Phase == "completed";
    private static bool PollRun(EndgameRiftView rift) => CanReenter(rift) || rift != null && rift.Phase == "reward_pending";
    private static string PhaseName(string phase)
    {
        switch (phase)
        {
            case "reserved": return "同道集结"; case "active": return "镇煞"; case "boss_arriving": return "劫影将至"; case "boss": return "镇境劫影";
            case "reward_pending": return "战果保存中"; case "completed": return "洞天功成"; case "closed": return "洞天已闭";
            case "failed": return "演劫未竟"; case "aborted": return "洞天中断"; case "interrupted": return "洞天中断 · 可领补偿";
            default: return "静候开境";
        }
    }
    private string Remaining(EndgameRiftView rift) => RiftClock(RiftRemainingSeconds(rift, State.RiftReceivedUtc, DateTime.UtcNow));
    private void DrawProgress(Rect rect, int progress, int target)
    {
        float scale = Mathf.Max(.4f, rect.height / 34f);
        float ratio = target > 0 ? Mathf.Clamp01((float)progress / target) : 0;
        int remaining = RiftRemainingSeconds(State.Rift, State.RiftReceivedUtc, DateTime.UtcNow);
        Rect bar = new Rect(rect.x, rect.y, 140f * scale, 34f * scale);
        DrawRiftProgressArtwork(bar, ratio, RiftElapsedRatio(State.Rift, remaining));
        Text(new Rect(bar.xMax + 8f * scale, rect.y + 8f * scale, rect.width - bar.width - 8f * scale, 22f * scale),
            "镇煞 " + progress + " / " + target + " · 余时 " + RiftClock(remaining), small, Jade);
    }
    private const string RiftCompletionNotice = "拾妥所得，可与归元道使交谈离境。";
    private string observedRun = "", observedPhase = "";
    private long observedGeneration = -1;
    private bool observedInside;
    private readonly System.Collections.Generic.HashSet<string> completedNoticeRuns = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
    // Only a live completion edge may announce the exit. Restored/replayed snapshots
    // establish a silent baseline, including completion observed while outside.
    private void ObserveRiftPhase()
    {
        if (observedGeneration != State.Generation)
        {
            observedGeneration = State.Generation; observedRun = observedPhase = ""; observedInside = false;
            completedNoticeRuns.Clear();
        }
        var rift = State.Rift;
        if (rift == null || rift.RunID.Length == 0)
        { observedRun = observedPhase = ""; observedInside = false; return; }
        bool inside = State.RiftContextKnown && State.RiftInInstance;
        bool completion = observedRun == rift.RunID && observedInside && inside && observedPhase.Length > 0 && observedPhase != "completed" && rift.Phase == "completed";
        observedRun = rift.RunID; observedPhase = rift.Phase; observedInside = inside;
        if (rift.Phase != "completed" && rift.Phase != "closed") return;
        bool firstCompletion = completedNoticeRuns.Add(rift.RunID);
        if (completion && firstCompletion) NewbieTipBox.PushText(RiftCompletionNotice);
    }
    private string riftRecoveryRequestID = "";
    private EndgameClientState riftRecoveryState;
    private long riftRecoveryGeneration = -1;
    private int riftRecoveryAttempts;
    private DateTime riftRecoveryNextUtc;
    // Entry/floor receipts may be lost after the native map has already changed.
    // Keep reconciling the exact authorized nonce without bringing back a modal.
    private void RecoverPendingRiftRequest(DateTime now)
    {
        EndgameRequest request = State?.PendingRequest;
        if (client == null || !client.EndgameInWorld || !State.Negotiated || !State.RiftContextKnown || !State.RiftInInstance ||
            request == null || !request.Op.StartsWith("rift.", StringComparison.Ordinal))
        { riftRecoveryRequestID = ""; riftRecoveryState = null; riftRecoveryAttempts = 0; return; }
        if (!ReferenceEquals(riftRecoveryState, State) || riftRecoveryGeneration != State.Generation || riftRecoveryRequestID != request.RequestID)
        {
            riftRecoveryState = State; riftRecoveryGeneration = State.Generation; riftRecoveryRequestID = request.RequestID;
            riftRecoveryAttempts = 0; riftRecoveryNextUtc = State.PendingSinceUtc.AddSeconds(3);
        }
        if (now < riftRecoveryNextUtc) return;
        client.RetryEndgame();
        riftRecoveryAttempts = Math.Min(4, riftRecoveryAttempts + 1);
        riftRecoveryNextUtc = now.AddSeconds(Math.Min(30, 3 * (1 << riftRecoveryAttempts)));
    }
    private static int RiftArrivalRemainingMS(EndgameRiftView rift, DateTime received, DateTime now)
    {
        var arrival = rift?.GuardianArrival;
        if (rift?.Phase != "boss_arriving" || arrival == null) return 0;
        double elapsed = Math.Max(0, (now - received).TotalMilliseconds);
        return (int)Math.Max(0, arrival.RemainingMS - Math.Min(int.MaxValue, Math.Floor(elapsed)));
    }
    private string RiftObjective(EndgameRiftView rift)
    {
        if (rift == null) return "静候开境";
        if (rift.Transitioning) return "正在换层：保留镇煞进度与倒计时，请稍候。";
        switch (rift.Phase)
        {
            case "reserved": return "同道集结：队友选择进入已有洞天。";
            case "active": return rift.CanAdvance ? "可换层：收取灵珠，或走入出口传送门。" : rift.FloorIndex > 0 ? "镇煞：收取灵珠；开启者可走入出口传送门换层。" : "镇煞：击败敌人，靠近灵珠收取进度。";
            case "boss_arriving":
                int arrivalRemaining = RiftArrivalRemainingMS(rift, State.RiftReceivedUtc, DateTime.UtcNow);
                return arrivalRemaining > 0 ? "劫影将至：" + Math.Ceiling(arrivalRemaining / 1000d).ToString("0") + "秒。" : "劫影将至。";
            case "boss": return "劫影现身：击败" + (rift.BossName.Length > 0 ? rift.BossName : "镇境劫影") + "。";
            case "reward_pending": return "劫影已退，战果正在保存；请稍候。";
            case "completed": return LiveCompletedRift(rift) ? RiftCompletionNotice : "洞天仍在，可重返拾取；收束后可补领。";
            case "interrupted": return rift.Claimable ? "洞天中断：可领取服务器确认的补偿符钥。" : "洞天中断：请查询服务器结算。";
            case "failed": return rift.Claimable ? "演劫未竟：仍有服务器确认的所得可领取。" : "演劫未竟：本次已结束，可返回龙城八仙使者处再战。";
            case "closed": return State.RiftInInstance ? "洞天已成，可与归元道使交谈离境。" : rift.Claimable ? "洞天已闭：战果已保存，仍可领取你的所得。" : "洞天已闭：所得请在背包查看，炼化可拜访巧匠左慈。";
            case "aborted": return "洞天已中断，请返回龙城八仙使者处查看后续。";
            default: return "静候开境";
        }
    }
    private void DrawRiftHud(float scale)
    {
        if (!RiftHudVisible) return;
        var rift = State.Rift;
        scale = RiftHudScale;
        Rect bar = RiftHudBarRect(scale);
        int remaining = RiftRemainingSeconds(rift, State.RiftReceivedUtc, DateTime.UtcNow);
        float progress = rift.ProgressMax > 0 ? Mathf.Clamp01((float)rift.Progress / rift.ProgressMax) : 0;
        DrawRiftProgressArtwork(bar, progress, RiftElapsedRatio(rift, remaining));
        // Keep only the native slot. Details, including arrival timing, stay on hover.
        if (bar.Contains(Event.current.mousePosition))
            UITip.Show("演劫 " + RiftDepth(rift) + " · " + PhaseName(rift.Phase) +
                "\n绿色：镇煞 " + rift.Progress + " / " + rift.ProgressMax +
                "\n白色：已用时间" + (rift.TotalSeconds > 0 ? " " + RiftClock(Math.Max(0, rift.TotalSeconds - remaining)) + " / " + RiftClock(rift.TotalSeconds) : "（等待服务器总时长）") +
                "\n余时 " + RiftClock(remaining) + (rift.PendingLoot > 0 ? "\n待拾取：" + rift.PendingLoot : "") + "\n" + RiftObjective(rift), scale);
    }
    private void DrawRiftMarkers(float scale)
    {
        var rift = State.Rift; Camera camera = Camera.main;
        if (!RiftWorldVisualsReady(rift) || camera == null) return;
        DrawRiftTelegraphs(rift, camera, scale);
        if (rift.Phase == "boss_arriving" && rift.GuardianArrival != null)
        {
            int remaining = RiftArrivalRemainingMS(rift, State.RiftReceivedUtc, DateTime.UtcNow);
            Vector3 point = RiftScreenPoint(camera, rift.GuardianArrival.X, rift.GuardianArrival.Y);
            if (remaining > 0 && point.z > 0)
                BitmapFont.CenterOutline(new Rect(point.x - 20 * scale, point.y - 48 * scale, 40 * scale, 30 * scale),
                    ((remaining + 999) / 1000).ToString(), Gold, scale * 1.6f);
        }
    }

}
public partial class CombatWorld
{
    internal static MonsterEntity EndgameMonster(int id)
    {
        if (I != null && I.monsters.TryGetValue(id, out MonsterEntity entity) && entity != null) return entity;
        return null;
    }
    internal static Transform EndgameMonsterTransform(int id) => EndgameMonster(id)?.transform;
}
}

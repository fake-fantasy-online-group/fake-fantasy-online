using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IEndgameClient
{
    private static readonly EndgameRetryJournal endgameRetryJournal = new EndgameRetryJournal();
    private string endgameAccount = "", endgameCharacter = "";
    private bool endgameWorldEntered;
    public EndgameClientState Endgame { get; } = new EndgameClientState();
    public bool EndgameInWorld => endgameWorldEntered && _stage == GatewayStage.InWorld && _netClient.Connected;
    private string EndgameScope => EndgameRetryJournal.Scope(_netClient.host + ":" + _netClient.port, endgameAccount, endgameCharacter);
    private void ResetEndgameConnection()
    {
        endgameRetryJournal.Remember(EndgameScope, Endgame.PendingRequest);
        Endgame.Reset(); endgameCharacter = ""; endgameWorldEntered = false;
    }
    public void TickEndgameForIndependentRuntime()
    {
        if (!EndgameInWorld || _worldEntryAdmission != null) return;
        Endgame.Tick(DateTime.UtcNow);
        if (!Endgame.HelloSent) SendEndgame(new EndgameRequest("hello"));
    }
    public bool SendEndgame(EndgameRequest request)
    {
        if (!_host.IsHostThread || !EndgameInWorld || _worldEntryAdmission != null || request == null) return false;
        if (!Endgame.Begin(request)) return false;
        if (request.Mutates) endgameRetryJournal.Remember(EndgameScope, request);
        try { _netClient.SendEndgame(request); return true; }
        catch (Exception error) when (error is FormatException || error is ArgumentException || IsWireFailure(error))
        { Endgame.NoteTransportFailure("发送未确认，请查询原请求。", request); return false; }
    }
    public bool RetryEndgame()
    {
        if (!_host.IsHostThread || !EndgameInWorld || _worldEntryAdmission != null || Endgame.PendingRequest == null) return false;
        try { _netClient.SendEndgame(Endgame.PendingRequest); return true; }
        catch (Exception error) when (IsWireFailure(error))
        { Endgame.NoteTransportFailure("连接尚未确认，请稍后重试。"); return false; }
    }
    private async Task<Result> UpgradeGrowthSkillAsync(int skillID, CancellationToken cancellationToken)
    {
        bool submitted = false;
        EndgameRequest command = null;
        try
        {
            await SendAsync(() =>
            {
                if (Endgame.CanInvestSkill(skillID))
                {
                    command = new EndgameRequest("skill.upgrade", Endgame.Revision, skillID: skillID);
                    submitted = SendEndgame(command);
                }
            }, cancellationToken).ConfigureAwait(false);
            if (submitted) return Result.Success();
            if (command != null && ReferenceEquals(Endgame.PendingRequest, command))
                return Result.Failure("skill.upgrade.uncertain", "Skill investment was not confirmed; query the original pending request.");
            return Result.Failure("skill.upgrade.unavailable", "Refresh the authoritative skill ranks and resolve any pending request before investing.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception error) when (error is OperationCanceledException || IsWireFailure(error))
        { return Result.Failure("skill.upgrade.uncertain", "Skill investment was not confirmed; query the original pending request."); }
    }
    private bool TryReceiveEndgamePacket(ushort opcode, byte[] payload)
    {
        if (opcode != PrivateServerEndgameCodec.ResponseOpcode) return false;
        try
        {
            EndgameResponse response = PrivateServerEndgameCodec.Decode(payload);
            if (response.Kind == "rift.context") { Endgame.Apply(response); return true; }
            if (!EndgameInWorld || !Endgame.HelloSent) return true;
            bool previouslyNegotiated = Endgame.Negotiated;
            Endgame.Apply(response);
            if (response.RequestID.Length > 0 && !Endgame.Pending) endgameRetryJournal.Complete(EndgameScope, response.RequestID);
            if (!previouslyNegotiated && Endgame.Negotiated)
            {
                Endgame.RestoreUncertainRequest(endgameRetryJournal.Find(EndgameScope));
                SendEndgame(new EndgameRequest("snapshot"));
                SendEndgame(new EndgameRequest("rift.state"));
                if (Endgame.GrowthV5) SendEndgame(new EndgameRequest("skill.ranks"));
            }
            if (response.Error == "stale_revision")
            {
                SendEndgame(new EndgameRequest("snapshot"));
                if (Endgame.GrowthV5) SendEndgame(new EndgameRequest("skill.ranks"));
            }
        }
        catch (Exception error) when (error is FormatException || error is ArgumentException)
        {
            Endgame.NoteInvalidResponse("服务器星图数据无效，请刷新重试。");
            _diagnosticSink?.Invoke("Endgame sidecar rejected: " + error.Message);
        }
        return true;
    }
    private bool MatchesEndgameInventory(EndgameItemView item)
    {
        if (item == null || !Endgame.Negotiated || Endgame.Pending || !EndgameInWorld || !_host.IsHostThread) return false;
        EndgameItemView current = Endgame.Find(item.Location, item.Tab, item.Slot, item.ItemID);
        if (current == null || current.UID != item.UID) return false;
        WorldInventoryState inventory = Volatile.Read(ref _inventoryState);
        if (item.Location == "bag")
        {
            WorldInventoryItemState row = inventory.ItemAt((byte)item.Tab, item.Slot);
            return row != null && row.ItemId == item.ItemID && row.EquipmentSlot > 0;
        }
        if (item.Location == "equipped") foreach (var row in inventory.EquippedItems)
            if (row.Slot == item.Slot && row.ItemId == item.ItemID) return true;
        return false;
    }
    // The native commands already carry Int32 positions. Extended slots keep their original IDs.
    public bool EquipEndgameItem(EndgameItemView item)
    {
        if (item == null || item.Location != "bag" || !MatchesEndgameInventory(item)) return false;
        _netClient.EquipItem(item.Slot); return true;
    }
    public bool UnequipEndgameItem(EndgameItemView item)
    {
        if (item == null || item.Location != "equipped" || !MatchesEndgameInventory(item)) return false;
        _netClient.UnequipItem(item.Slot); return true;
    }
}
}

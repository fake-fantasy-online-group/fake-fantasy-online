using System;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Presentation facet for the two official 1.5.8 NetClient commands that PlayerActionMenu.Fire
// needs and the product did not have yet. Both are single-argument sends:
//
//   NetClient.TradeRequest (0x06000750)  instance void TradeRequest(int)
//   NetClient.InspectPlayer (0x060006f7) instance void InspectPlayer(int)
//
// All eight commands Fire dispatches share one official shape - allocate a one-field packet struct,
// assign the argument at +0x10 and hand it to the generic Send with a per-command writer lambda -
// so these two follow it exactly. The other six (StallBrowse 0x060006f6, RideTogether 0x060006f9,
// RideInvite 0x060006fa, PartyKick 0x0600075b, PartyLeader 0x0600075c, FriendReq 0x0600075e)
// already exist on NetClient.StallCommands / SocialExtraCommands / PartyCommands / FriendCommands
// and are reused unchanged.
//
// The Presentation layer never imports Protocol: the writer delegates are bound by
// ReferenceCharacterSkillUsePort to the Application entries
// ClientSessionCoordinator.RequestTradeAsync and ClientSessionCoordinator.InspectPlayerAsync.
internal partial class NetClient
{
    private IGameServerGateway playerMenuWriter;
    internal void BindPlayerMenuWriterForIndependentRuntime(IGameServerGateway writer) =>
        playerMenuWriter = writer ?? throw new ArgumentNullException(nameof(writer));

    // Product-only forwarding names distinguish these adapters from the original Protocol
    // NetClient methods. The menu keeps its original guards/captures; the same gateway owns Send.
    public void PartyCreateForIndependentRuntime(string name) =>
        _ = ((IPartyCommands)playerMenuWriter).CreateAsync(name, CancellationToken.None);
    public void PartyInviteForIndependentRuntime(int id, string name) =>
        _ = ((IPartyCommands)playerMenuWriter).InviteAsync(id, name, CancellationToken.None);
    public void PartyJoinReqForIndependentRuntime(int id, string name) =>
        _ = ((IPartyCommands)playerMenuWriter).JoinAsync(id, name, CancellationToken.None);
    public void PartyKickForIndependentRuntime(int id) =>
        _ = ((IPartyCommands)playerMenuWriter).KickAsync(id, CancellationToken.None);
    public void PartyLeaderForIndependentRuntime(int id) =>
        _ = ((IPartyCommands)playerMenuWriter).SetLeaderAsync(id, CancellationToken.None);
    public void PartyLeaveForIndependentRuntime() =>
        _ = ((IPartyCommands)playerMenuWriter).LeaveAsync(CancellationToken.None);
    public void PartyDismissForIndependentRuntime() =>
        _ = ((IPartyCommands)playerMenuWriter).DismissAsync(CancellationToken.None);
    public void ApprenticeListForIndependentRuntime() =>
        _ = ((IApprenticeCommands)playerMenuWriter).ListAsync(CancellationToken.None);
    public void FriendReqForIndependentRuntime(string name) =>
        _ = ((IFriendCommands)playerMenuWriter).AddAsync(name, CancellationToken.None);
    public void FriendDelForIndependentRuntime(string name) =>
        _ = ((IFriendCommands)playerMenuWriter).DeleteAsync(name, CancellationToken.None);
    public void SendBlockAddForIndependentRuntime(string name, byte scope) =>
        _ = ((IBlockCommands)playerMenuWriter).AddAsync(name, scope, CancellationToken.None);
    public void SendBlockDelForIndependentRuntime(long characterId) =>
        _ = ((IBlockCommands)playerMenuWriter).DeleteAsync(characterId, CancellationToken.None);
    public void ApprenticeReqForIndependentRuntime(int id, byte role, string name) =>
        _ = ((IApprenticeCommands)playerMenuWriter).RequestAsync(id, role, name, CancellationToken.None);
    public void RideTogetherForIndependentRuntime(int id) =>
        _ = ((IRidingCommands)playerMenuWriter).RideTogetherAsync(id, CancellationToken.None);
    public void RideInviteForIndependentRuntime(int id) =>
        _ = ((IRidingCommands)playerMenuWriter).InviteAsync(id, CancellationToken.None);
    public void FamilyInviteForIndependentRuntime(int id, string name) =>
        _ = ((IFamilyCommands)playerMenuWriter).InviteAsync(id, name, CancellationToken.None);

    private Action<int> tradeRequestWriter;
    private Action<int> inspectPlayerWriter;

    internal void BindPlayerActionCommandsForIndependentRuntime(
        Action<int> tradeWriter,
        Action<int> inspectWriter)
    {
        tradeRequestWriter = tradeWriter;
        inspectPlayerWriter = inspectWriter;
    }

    /// <summary>Official NetClient.TradeRequest (0x06000750): PlayerActionMenu act 0
    /// (邀请参加交易) sends the target entity id.</summary>
    public void TradeRequestForIndependentRuntime(int targetId)
    {
        tradeRequestWriter?.Invoke(targetId);
    }

    /// <summary>Official NetClient.InspectPlayer (0x060006f7): PlayerActionMenu act 40
    /// (查看玩家装备) sends the target entity id; the equipment window stays closed until the
    /// server-authored snapshot advances.</summary>
    public void InspectPlayerForIndependentRuntime(int targetId)
    {
        inspectPlayerWriter?.Invoke(targetId);
    }
}
}

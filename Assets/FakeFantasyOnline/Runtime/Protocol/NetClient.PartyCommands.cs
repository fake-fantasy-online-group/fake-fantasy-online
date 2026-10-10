namespace FakeFantasyOnline.Protocol
{
    public sealed partial class NetClient
    {
        public void PartyInvite(int targetId, string targetName = "") =>
            Send(ProtocolOpcodes.PartyInviteRequest, b => { b.I32(targetId); b.Str(targetName ?? ""); });
        public void PartyLeave() => Send(ProtocolOpcodes.PartyLeaveRequest, _ => { });
        public void PartyKick(int targetId) => Send(ProtocolOpcodes.PartyKickRequest, b => b.I32(targetId));
        public void PartyLeader(int targetId) => Send(ProtocolOpcodes.PartyLeaderRequest, b => b.I32(targetId));
        public void PartyCreate(string name) => Send(ProtocolOpcodes.PartyCreateRequest, b => b.Str(name ?? ""));
        public void PartyDismiss() => Send(ProtocolOpcodes.PartyDismissRequest, _ => { });
        public void PartyJoinReq(int targetId, string targetName = "") =>
            Send(ProtocolOpcodes.PartyJoinRequest, b => { b.I32(targetId); b.Str(targetName ?? ""); });
    }
}

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class NetClient
    {
        public void FamilyInvite(int targetId, string targetName = "") => Send(ProtocolOpcodes.FamilyInviteRequest, b => { b.I32(targetId); b.Str(targetName ?? ""); });
        public void FamilyLeave() => Send(ProtocolOpcodes.FamilyLeaveRequest, _ => { });
        public void FamilyCreate(string name, string proclaim) => Send(ProtocolOpcodes.FamilyCreateRequest, b => { b.Str(name ?? ""); b.Str(proclaim ?? ""); });
        public void FamilyList() => Send(ProtocolOpcodes.FamilyListRequest, _ => { });
        public void FamilyApply(string name) => Send(ProtocolOpcodes.FamilyApplyRequest, b => b.Str(name ?? ""));
        public void FamilyManage(byte operation, string name, int targetId, string text) => Send(ProtocolOpcodes.FamilyManageRequest, b => { b.U8(operation); b.Str(name ?? ""); b.I32(targetId); b.Str(text ?? ""); });
        public void FamilyMail(string title, string body) => Send(ProtocolOpcodes.FamilyMailRequest, b => { b.Str(title ?? ""); b.Str(body ?? ""); });
        public void FamilyPositions() => Send(ProtocolOpcodes.FamilyPositionsRequest, _ => { });
        public void FamilyRefresh() => Send(ProtocolOpcodes.FamilyRefreshRequest, _ => { });
        public void FamilyPosName(int positionId, string name) => Send(ProtocolOpcodes.FamilyPosNameRequest, b => { b.I32(positionId); b.Str(name ?? ""); });
        public void FamilyStash(int op, int bagTab, int bagSlot, int index) => Send(ProtocolOpcodes.FamilyStashRequest, b =>
        {
            b.U8(unchecked((byte)op));
            b.U8(unchecked((byte)bagTab));
            b.I32(bagSlot);
            b.I32(index);
        });
        public void FamilyStashPerm(int pos) => Send(ProtocolOpcodes.FamilyStashPermRequest, b => b.U8(unchecked((byte)pos)));
    }
}

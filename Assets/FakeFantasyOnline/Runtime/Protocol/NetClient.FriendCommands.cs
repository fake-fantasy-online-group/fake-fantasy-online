namespace FakeFantasyOnline.Protocol
{
    public sealed partial class NetClient
    {
        public void FriendReq(string name) => Send(ProtocolOpcodes.FriendRequest, b => b.Str(name ?? ""));
        public void FriendDel(string name) => Send(ProtocolOpcodes.FriendDeleteRequest, b => b.Str(name ?? ""));
    }
}

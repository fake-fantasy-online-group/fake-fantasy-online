namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    public void SendFeedback(string text) => Send(0x1074, b => b.Str(text));
    public void SendVoteAdd(string text) => Send(0x108a, b => b.Str(text));
    public void SendVoteCast(int id, byte stance) => Send(0x108b, b =>
    { b.I32(id); b.U8(stance); });
}
}

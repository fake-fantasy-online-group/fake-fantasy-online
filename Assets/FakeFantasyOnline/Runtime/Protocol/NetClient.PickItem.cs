namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    public void PickItem(int groundId) => Send(0x1010, b => b.I32(groundId));
}
}

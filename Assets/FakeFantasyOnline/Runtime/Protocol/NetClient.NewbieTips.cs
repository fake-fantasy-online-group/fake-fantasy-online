namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Original 060006d3 and writer 060008a0.
    public void TipSeen(int tipId) => Send(0x1076, b => b.I32(tipId));
}
}

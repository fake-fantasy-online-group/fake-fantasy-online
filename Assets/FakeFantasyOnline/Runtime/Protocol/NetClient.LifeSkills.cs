namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Original LearnLife 06000705 and I32 writer 060007f1.
    public void LearnLife(int skill) => Send(0x1026, b => b.I32(skill));
}
}

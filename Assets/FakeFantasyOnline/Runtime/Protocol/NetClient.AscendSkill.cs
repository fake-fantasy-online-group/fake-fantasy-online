namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    public void AscendSkill(int skillId) => Send(0x108f, b => b.I32(skillId));
}
}

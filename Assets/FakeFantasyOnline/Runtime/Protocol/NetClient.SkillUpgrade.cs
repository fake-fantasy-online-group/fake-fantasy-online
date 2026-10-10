namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Original UpgradeSkill 0600071b and writer 06000805.
    public void UpgradeSkill(int skillId) => Send(0x1012, b => b.I32(skillId));
}
}

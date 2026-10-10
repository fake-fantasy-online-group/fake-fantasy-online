using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : IWorldLifeSkillReceiptOwner
{
    public void ReceiveLifeSkills(IWorldLifeSkillReceiptReader reader) =>
        NetClient.ReceiveLifeSkillsForIndependentRuntime(reader);
}
}

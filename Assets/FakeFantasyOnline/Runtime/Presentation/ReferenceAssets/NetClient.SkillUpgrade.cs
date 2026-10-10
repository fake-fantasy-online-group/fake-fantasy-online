using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int> writeUpgradeSkill;
    internal void BindUpgradeSkillWriterForIndependentRuntime(Action<int> writer) => writeUpgradeSkill = writer;
    public void UpgradeSkillForIndependentRuntime(int skillId) => writeUpgradeSkill(skillId);
}
}

using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int> writeAscendSkill;
    internal void BindAscendSkillWriterForIndependentRuntime(Action<int> writer) => writeAscendSkill = writer;
    public void AscendSkillForIndependentRuntime(int skillId) => writeAscendSkill(skillId);
}
}

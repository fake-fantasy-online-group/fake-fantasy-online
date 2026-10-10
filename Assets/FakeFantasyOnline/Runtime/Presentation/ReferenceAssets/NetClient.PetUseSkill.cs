using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int> writePetUseSkill;
    internal void BindPetSkillWriterForIndependentRuntime(Action<int> writer) => writePetUseSkill = writer;
    // Official 06000737 forwards PetCmd(6, skillId, ""); the existing port owns its wire write.
    public void PetUseSkillForIndependentRuntime(int skillId) => writePetUseSkill(skillId);
}

internal static partial class CharData
{
    public static int petActiveSkill;
}
}

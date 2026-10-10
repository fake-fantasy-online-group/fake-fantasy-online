using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int> writeLearnLife;
    internal void BindLearnLifeWriterForIndependentRuntime(Action<int> writer) => writeLearnLife = writer;
    public void LearnLifeForIndependentRuntime(int skill) => writeLearnLife(skill);
}
}

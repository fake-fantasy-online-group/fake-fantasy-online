using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int> writeBeginWork;
    private Action<int> writeGather;
    internal void BindWorkWritersForIndependentRuntime(Action<int> begin, Action<int> gather)
    { writeBeginWork = begin; writeGather = gather; }
    public void BeginWorkForIndependentRuntime(int skill) => writeBeginWork(skill);
    public void GatherForIndependentRuntime(int skill) => writeGather(skill);
}
}

using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int[], int[], bool> writeHotbar;
    internal void BindHotbarWriterForIndependentRuntime(Action<int[], int[], bool> writer) => writeHotbar = writer;
    public void SetHotbarForIndependentRuntime(int[] ids, int[] kinds, bool expanded) => writeHotbar(ids, kinds, expanded);
}
}

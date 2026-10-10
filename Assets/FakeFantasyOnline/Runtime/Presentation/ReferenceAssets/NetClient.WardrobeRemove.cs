using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official Win05+<>c__DisplayClass105_1.<DrawMagicStore>b__2 (0x06001071) sends inside the
    // extraction confirmation, so the facade hop has to be a single synchronous call. The bound
    // writer is the one session/gateway/Protocol route; no second writer is introduced here.
    private Action<int> writeWardrobeRemove;
    internal void BindWardrobeRemoveWriterForIndependentRuntime(Action<int> writer) => writeWardrobeRemove = writer;
    public void WardrobeRemoveForIndependentRuntime(int itemId) => writeWardrobeRemove(itemId);
}
}

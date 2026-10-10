using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official Win05.DrawMagicStore (0x0600104f) sends NetClient.I.WardrobeWear inside the same
    // left-click mouse-up branch that also clears the drag state, so the facade hop has to be a
    // single synchronous call. The bound writer is the one session/gateway/Protocol route; no
    // second writer and no second queue is introduced here.
    private Action<int> writeWardrobeWear;
    internal void BindWardrobeWearWriterForIndependentRuntime(Action<int> writer) => writeWardrobeWear = writer;
    public void WardrobeWearForIndependentRuntime(int itemId) => writeWardrobeWear(itemId);
}
}

using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official Win05.DrawMagicStore (0x0600104f) sends NetClient.I.WardrobeMove inside the same
    // drag mouse-up branch that also clears the drag state, so the facade hop has to be a single
    // synchronous call. The bound writer is the one session/gateway/Protocol route; no second
    // writer and no second queue is introduced here.
    private Action<int, int> writeWardrobeMove;
    internal void BindWardrobeMoveWriterForIndependentRuntime(Action<int, int> writer) => writeWardrobeMove = writer;
    public void WardrobeMoveForIndependentRuntime(int itemId, int toIndex) => writeWardrobeMove(itemId, toIndex);
}
}

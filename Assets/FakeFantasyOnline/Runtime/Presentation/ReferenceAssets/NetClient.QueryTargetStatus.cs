using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int> writeQueryTargetStatus;

    internal void BindTargetStatusWriterForIndependentRuntime(Action<int> writer) =>
        writeQueryTargetStatus = writer;

    // Decision-free bridge to Protocol's existing 060006f8 writer. Its task observer does
    // not participate in the native HUD deadline or target-change admission.
    public void QueryTargetStatusForIndependentRuntime(int targetId) => writeQueryTargetStatus(targetId);
}
}

using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int, int> writeCapture;
    internal void BindCaptureWriterForIndependentRuntime(Action<int, int> writer) => writeCapture = writer;
    public void Capture(int targetId, int itemId)
    {
        CombatWorld.SpawnCaptureAnim(targetId, itemId);
        writeCapture(targetId, itemId);
    }
}
}

using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int, byte, byte, int> writeUseItemOn;
    internal void BindUseItemOnWriterForIndependentRuntime(Action<int, byte, byte, int> writer) =>
        writeUseItemOn = writer;
    public void UseItemOnForIndependentRuntime(int toolId, byte targetKind, byte tab, int slot) =>
        writeUseItemOn(toolId, targetKind, tab, slot);
}
}

using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<byte, byte, int, int> writeWashAffix;
    internal void BindWashAffixWriterForIndependentRuntime(Action<byte, byte, int, int> writer) => writeWashAffix = writer;
    public void WashAffixForIndependentRuntime(byte kind, byte tab, int slot, int index) => writeWashAffix(kind, tab, slot, index);
}
}

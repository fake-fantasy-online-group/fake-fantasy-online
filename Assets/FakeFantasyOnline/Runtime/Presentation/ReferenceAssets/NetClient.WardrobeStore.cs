using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<byte, int> writeWardrobeStore;
    internal void BindWardrobeStoreWriterForIndependentRuntime(Action<byte, int> writer) => writeWardrobeStore = writer;
    public void WardrobeStoreForIndependentRuntime(byte tab, int slot) => writeWardrobeStore(tab, slot);
}
}

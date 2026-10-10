using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int> writePickItem;
    internal void BindPickItemWriterForIndependentRuntime(Action<int> write) => writePickItem = write;
    public void PickItemForIndependentRuntime(int groundId) => writePickItem(groundId);
}
}

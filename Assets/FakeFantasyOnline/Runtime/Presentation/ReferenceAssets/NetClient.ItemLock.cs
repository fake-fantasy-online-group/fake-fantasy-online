using System;
using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<ItemLockRequest> writeItemLock;
    internal void BindItemLockWriterForIndependentRuntime(Action<ItemLockRequest> writer) => writeItemLock = writer;
    public void SetItemLockForIndependentRuntime(int tab, int slot, bool on, string secCode = "", int itemId = 0) =>
        writeItemLock(new ItemLockRequest(tab, slot, on, secCode, itemId));
}
}

using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<byte, string> writeStallCreate;
    private Action<byte, int, int, int, long> writeStallAdd;
    private Action writeStallEnd;
    private Action<int> writeStallBrowse;
    internal void BindStallWritersForIndependentRuntime(Action<byte, string> create,
        Action<byte, int, int, int, long> add, Action end)
    { writeStallCreate = create; writeStallAdd = add; writeStallEnd = end; }
    internal void BindStallBrowseWriterForIndependentRuntime(Action<int> browse) => writeStallBrowse = browse;
    public void StallBrowseForIndependentRuntime(int ownerId) => writeStallBrowse(ownerId);
    public void StallCreateForIndependentRuntime(byte type, string name) => writeStallCreate(type, name);
    public void StallAddForIndependentRuntime(byte tab, int slot, int itemId, int count, long price) => writeStallAdd(tab, slot, itemId, count, price);
    public void StallEndForIndependentRuntime() => writeStallEnd();
}
}

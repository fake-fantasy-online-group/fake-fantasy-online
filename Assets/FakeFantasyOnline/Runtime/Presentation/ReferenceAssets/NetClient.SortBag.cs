using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int> writeSortBag;

    internal void BindSortBagWriterForIndependentRuntime(Action<int> writer) => writeSortBag = writer;

    // Official 0x0600073f, instance System.Void NetClient.SortBag(System.Int32).
    // The official body allocates the writer closure and sends opcode 0x1078
    // (ProtocolOpcodes.SortInventoryRequest); the exported writer 0600082d stores the captured tab as a
    // single U8, so the payload is one zero-based tab byte and nothing else. Both architectures agree.
    public void SortBagForIndependentRuntime(int tab) => writeSortBag(tab);
}
}

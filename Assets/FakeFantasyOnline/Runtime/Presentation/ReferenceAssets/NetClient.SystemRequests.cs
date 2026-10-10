using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int, byte> systemRequestWriter;
    internal void BindSystemRequestWriterForIndependentRuntime(Action<int, byte> writer) => systemRequestWriter = writer;
    public void SysMsgRespondForIndependentRuntime(int reqId, byte accept) => systemRequestWriter?.Invoke(reqId, accept);
}
}

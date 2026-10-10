using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<byte> writeRevive;

    internal void BindReviveWriterForIndependentRuntime(Action<byte> writer) => writeRevive = writer;
    public void ReviveForIndependentRuntime(byte option) => writeRevive(option);
}
}

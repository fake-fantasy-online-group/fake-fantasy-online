using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<byte, byte> writeChangeHair;
    internal void BindChangeHairWriterForIndependentRuntime(Action<byte, byte> writer) => writeChangeHair = writer;
    public void ChangeHairForIndependentRuntime(byte mode, byte targetId) => writeChangeHair(mode, targetId);
}
}

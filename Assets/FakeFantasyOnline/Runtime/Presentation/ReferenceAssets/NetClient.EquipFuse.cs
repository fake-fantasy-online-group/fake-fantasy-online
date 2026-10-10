using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<byte, byte, byte, int, byte, int, int> writeEquipFuse;
    internal void BindEquipFuseWriterForIndependentRuntime(Action<byte, byte, byte, int, byte, int, int> writer) =>
        writeEquipFuse = writer;
    public void EquipFuseForIndependentRuntime(byte op, byte mainKind, byte mainTab, int mainSlot, byte subTab, int subSlot, int sig = 0) =>
        writeEquipFuse(op, mainKind, mainTab, mainSlot, subTab, subSlot, sig);
}
}

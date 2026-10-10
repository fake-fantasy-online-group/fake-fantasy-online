using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<byte, int, int, byte, byte, byte> writeApplyAvatar;
    internal void BindApplyAvatarWriterForIndependentRuntime(Action<byte, int, int, byte, byte, byte> writer) => writeApplyAvatar = writer;
    public void ApplyAvatarForIndependentRuntime(byte bagTab, int bagSlot, int targetSlot, byte kind = 0, byte targetKind = 1, byte targetTab = 0) =>
        writeApplyAvatar(bagTab, bagSlot, targetSlot, kind, targetKind, targetTab);
}
}

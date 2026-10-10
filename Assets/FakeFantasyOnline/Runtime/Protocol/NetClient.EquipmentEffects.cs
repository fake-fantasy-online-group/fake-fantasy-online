using System;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Host binding for the Presentation-owned CharData getter. Send supplies the actual Buf;
    // the getter is not evaluated while constructing the command or its writer delegate.
    internal void SendEquipFx(Func<int> readHideMask)
    {
        Send(ProtocolOpcodes.EquipmentEffectsMaskRequest,
            buffer => WriteEquipmentEffectsForIndependentRuntime(buffer, readHideMask));
    }

    // Original callback: 1.5.8 0600079c -> 1.6.0 060007a6. CharData initialization/getter
    // occurs before the supplied Buf dereference, including when that Buf is null. The
    // Application-layer getter binding avoids a Protocol -> Presentation dependency.
    internal static void WriteEquipmentEffectsForIndependentRuntime(Buf buffer, Func<int> readHideMask)
    {
        int hideMask = readHideMask();
        buffer.I32(hideMask);
    }
}
}

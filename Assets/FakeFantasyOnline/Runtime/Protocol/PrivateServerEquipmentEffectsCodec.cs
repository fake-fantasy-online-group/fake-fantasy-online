namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Value-to-bytes compatibility codec for an already-read signed 32-bit hide mask.
/// The live SendEquipFx callback writes directly to the buffer supplied by NetClient.Send.
/// </summary>
internal static class PrivateServerEquipmentEffectsCodec
{
    public static byte[] Encode(int hideMask)
    {
        WireWriter writer = new WireWriter();
        writer.WriteInt32(hideMask);
        return writer.ToArray();
    }
}

}

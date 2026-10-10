namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Exact fixed-width writers for the official local-player movement and position-save roots.
/// </summary>
public static class PrivateServerMovementCodec
{
    public const int PlayerMovePayloadSize = sizeof(int) * 2;
    public const int SavePositionPayloadSize = sizeof(int) * 3 + sizeof(byte);

    public static byte[] EncodePlayerMove(int x, int y)
    {
        WireWriter writer = new WireWriter();
        writer.WriteInt32(x);
        writer.WriteInt32(y);
        byte[] payload = writer.ToArray();
        return payload.Length == PlayerMovePayloadSize ? payload : null;
    }

    public static bool TryEncodeSavePosition(
        int mapId,
        int x,
        int y,
        bool entered,
        out byte[] payload)
    {
        payload = null;
        WireWriter writer = new WireWriter();
        writer.WriteInt32(mapId);
        writer.WriteInt32(x);
        writer.WriteInt32(y);
        writer.WriteByte(entered ? (byte)1 : (byte)0);
        payload = writer.ToArray();
        return payload.Length == SavePositionPayloadSize;
    }
}

}

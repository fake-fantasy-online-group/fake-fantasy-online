namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Exact fixed-width writer for the official normal-attack request.
/// </summary>
public static class PrivateServerCombatCodec
{
    public const int AttackPayloadSize = sizeof(int);

    public static bool TryEncodeAttack(int targetEntityId, out byte[] payload)
    {
        payload = null;
        // Native Attack (0600074d) forwards every Int32 target id unchanged.
        WireWriter writer = new WireWriter();
        writer.WriteInt32(targetEntityId);
        payload = writer.ToArray();
        return payload.Length == AttackPayloadSize;
    }
}

}

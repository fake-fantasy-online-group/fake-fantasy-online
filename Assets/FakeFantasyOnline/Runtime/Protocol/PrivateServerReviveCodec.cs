using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Exact one-byte writer for the official ordinary revive-choice request.
/// </summary>
public static class PrivateServerReviveCodec
{
    public const int PayloadSize = sizeof(byte);

    public static bool TryEncode(WorldReviveOption option, out byte[] payload)
    {
        payload = null;
        if (option != WorldReviveOption.RespawnCharacter &&
            option != WorldReviveOption.SubstituteDoll &&
            option != WorldReviveOption.SoulReturnDoll)
        {
            return false;
        }

        NetClient.Buf writer = new NetClient.Buf();
        NetClient.WriteReviveForIndependentRuntime(writer, (byte)option);
        payload = writer.ToArray();
        return payload.Length == PayloadSize;
    }
}

}

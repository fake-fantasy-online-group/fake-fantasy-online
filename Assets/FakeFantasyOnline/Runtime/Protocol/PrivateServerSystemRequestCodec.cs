using System;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Exact outbound codec for one response to a server-issued system request.
/// </summary>
public static class PrivateServerSystemRequestCodec
{
    public static bool TryEncodeResponse(
        int requestId,
        bool accept,
        out byte[] payload) =>
        TryEncodeResponse(requestId, accept ? (byte)1 : (byte)0, out payload);

    public static bool TryEncodeResponse(
        int requestId,
        byte accept,
        out byte[] payload)
    {
        payload = null;
        // Native 0x0600076f forwards every server-issued Int32 request id unchanged.
        WireWriter writer = new WireWriter();
        writer.WriteInt32(requestId);
        writer.WriteByte(accept);
        payload = writer.ToArray();
        return true;
    }
}

}

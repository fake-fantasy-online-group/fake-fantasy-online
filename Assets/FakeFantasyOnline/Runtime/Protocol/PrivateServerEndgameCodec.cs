using System;
using System.Text;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
/// <summary>Additive schema envelope. No native inventory, login or capability-marker layout changes.</summary>
public static class PrivateServerEndgameCodec
{
    public const ushort RequestOpcode = 0x10d0;
    public const ushort ResponseOpcode = 0x80d0;
    public const ushort Schema = 1;
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
    public static byte[] Encode(EndgameRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        byte[] json = Utf8.GetBytes(request.Json);
        if (json.Length > EndgameResponse.MaximumBytes) throw new FormatException("Endgame request exceeds limit.");
        var payload = new byte[json.Length + 4]; payload[0] = 1; payload[2] = (byte)json.Length; payload[3] = (byte)(json.Length >> 8);
        Buffer.BlockCopy(json, 0, payload, 4, json.Length); return payload;
    }
    public static EndgameResponse Decode(byte[] payload)
    {
        if (payload == null || payload.Length < 4 || payload.Length > EndgameResponse.MaximumBytes + 4) throw new FormatException("Invalid endgame payload size.");
        if ((payload[0] | payload[1] << 8) != Schema) throw new FormatException("Unsupported endgame schema.");
        int size = payload[2] | payload[3] << 8;
        if (size != payload.Length - 4) throw new FormatException("Truncated or trailing endgame payload.");
        return EndgameResponse.Parse(Utf8.GetString(payload, 4, size));
    }
}
public sealed partial class NetClient
{
    public void SendEndgame(EndgameRequest request)
    {
        byte[] payload = PrivateServerEndgameCodec.Encode(request);
        Send(PrivateServerEndgameCodec.RequestOpcode, b => b.Bytes(payload));
    }
}
}

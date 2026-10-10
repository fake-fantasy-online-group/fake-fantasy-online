using System;
using System.Collections.Generic;
using System.IO;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Outbound chat payload writer, including the official 1.5.8 channel metadata tail.
/// </summary>
internal static class PrivateServerChatCodec
{
    public static bool TryEncode(
        WorldChatSendRequest request,
        out ushort opcode,
        out byte[] payload)
    {
        opcode = 0;
        payload = null;
        if (request == null)
        {
            return false;
        }

        WireWriter writer = new WireWriter();
        if (request.Kind == WorldChatSendKind.Channel)
        {
            opcode = ProtocolOpcodes.ChannelChatRequest;
            writer.WriteByte(request.Channel);
            writer.WriteNativeString(request.Text);
            WriteShares(writer, request.Shares);
            writer.WriteInt32(0);
            writer.WriteByte(0);
        }
        else if (request.Kind == WorldChatSendKind.Whisper)
        {
            opcode = ProtocolOpcodes.GossipRequest;
            writer.WriteString(request.TargetName);
            writer.WriteString(request.Text);
            WriteShares(writer, request.Shares);
        }
        else if (request.Kind == WorldChatSendKind.GmCommand)
        {
            opcode = ProtocolOpcodes.GmCommandRequest;
            writer.WriteString(request.Text);
        }
        else
        {
            return false;
        }

        payload = writer.ToArray();
        return true;
    }

    // Off the wire path - the real commands write through NetClient.WriteShares (0x060006dd). This
    // mirror keeps the same element type the official list carries so the Editor byte-shape audit
    // reads the same triple order; its own null/length guard is this codec's, not the native one.
    private static void WriteShares(
        WireWriter writer,
        List<(byte, byte, int)> shares)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (shares == null || shares.Count > WorldChatSendRequest.MaximumShares)
        {
            throw new InvalidDataException("The chat share list is invalid.");
        }

        writer.WriteByte(checked((byte)shares.Count));
        for (int index = 0; index < shares.Count; index++)
        {
            writer.WriteByte(shares[index].Item1);
            writer.WriteByte(shares[index].Item2);
            writer.WriteInt32(shares[index].Item3);
        }
    }
}

}

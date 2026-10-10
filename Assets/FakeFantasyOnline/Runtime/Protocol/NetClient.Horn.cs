using System.Collections.Generic;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 0x060006dc SendHorn (arm64 0x267454 / 256 bytes, x86_64 0x2768e0 / 240 bytes).
    // The body allocates one display class, stores text (+0x10), shares (+0x18), hornItemId
    // (+0x20) and skin (+0x24), then calls Send(0x1058, <SendHorn>b__0).
    // The captured writer 0x060008ae (arm64 0x278120 / 164 bytes, x86_64 0x2871a0 / 160 bytes)
    // fixes the payload order and the read timing:
    //   b.U8(5); b.Str(text); WriteShares(b, shares); b.I32(hornItemId); b.U8(skin);
    // The list is captured by reference and indexed inside WriteShares (0x060006dd) when the
    // writer runs, so the caller's list is never copied or converted into another shape and a
    // null list stays null: WriteShares writes the count 0 for it.
    public void SendHorn(string text, int hornItemId, byte skin,
        List<(byte, byte, int)> shares = null) =>
        Send(ProtocolOpcodes.ChannelChatRequest, b =>
        {
            b.U8(5);
            b.Str(text);
            WriteShares(b, shares);
            b.I32(hornItemId);
            b.U8(skin);
        });
}
}

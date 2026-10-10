using System.Collections.Generic;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class NetClient
{
    // Official 0x06000771 NetClient.Gossip(toName, text, shares).
    // Frozen bodies: arm64 rva 0x271978 / 260 bytes and x86_64 rva 0x281020 / 256 bytes
    // (Build/ReferenceReverseGhidra-1.5.8/queries/wbc047-{arm64,x86_64}-Gossip-06000771).
    // Both allocate one display class (NetClient+<>c__DisplayClass263_0, TypeInfo usage 3023),
    // store toName at +0x10, text at +0x18 and shares at +0x20 - every store behind the write
    // barrier, so all three arguments are captured by reference - then build the writer delegate
    // (<Gossip>b__0, MethodDef usage 9654) on the shared Action<NetClient+Buf> class
    // (TypeInfo usage 377) and tail-call NetClient.Send (0x06000772) with the opcode 0x104c,
    // which is the same constant as ProtocolOpcodes.GossipRequest.
    //
    // The captured writer 0x06000879 (arm64 rva 0x277484 / 164 bytes, x86_64 rva 0x286580 /
    // 128 bytes) fixes the payload order and the read timing:
    //   b.Str(toName); b.Str(text); WriteShares(b, shares);
    // and nothing else: unlike SendChat there is no trailing metadata. The list is never copied
    // or converted - it is indexed inside WriteShares (0x060006dd) when the writer runs, so a
    // null list still writes the count 0 and an over-long list is clamped to 4 by WriteShares.
    // The null-b guard is the official one: a null Buf reaches the non-returning
    // NullReferenceException stub before the first write, which Buf.Str reproduces.
    public void Gossip(string toName, string text, List<(byte, byte, int)> shares) =>
        Send(ProtocolOpcodes.GossipRequest, b =>
        {
            b.Str(toName);
            b.Str(text);
            WriteShares(b, shares);
        });

    // Official 0x060006db NetClient.SendChat(channel, text, shares).
    // Frozen bodies: arm64 rva 0x267354 / 248 bytes and x86_64 rva 0x2767e0 / 240 bytes
    // (Build/ReferenceReverseGhidra-1.5.8/queries/wbc047-{arm64,x86_64}-SendChat-060006db).
    // One display class (NetClient+<>c__DisplayClass86_0, TypeInfo usage 3048) stores the byte
    // channel at +0x10 with no write barrier because it is a value type, then text at +0x18 and
    // shares at +0x20 behind the write barrier; the writer delegate (<SendChat>b__0, MethodDef
    // usage 9680) goes to NetClient.Send (0x06000772) with the opcode 0x1058, the same constant
    // as ProtocolOpcodes.ChannelChatRequest and the same opcode NetClient.SendHorn (0x060006dc)
    // uses - the channel byte is what separates the two calls, not the opcode.
    //
    // The captured writer 0x060008ac (arm64 rva 0x27807c / 164 bytes, x86_64 rva 0x287110 /
    // 144 bytes) fixes the payload order:
    //   b.U8(channel); b.Str(text); WriteShares(b, shares); b.I32(0); b.U8(0);
    // The trailing I32 0 and U8 0 are literal constants on both architectures; SendHorn's writer
    // (0x060008ae) writes a real horn item id and a real skin in exactly those two slots, which
    // is why the two commands share one opcode. Channel is written as a single byte, never as
    // an Int32, and the callers own the 0..3 range (ChatUI.SendNow and GameBootstrap's OnSend
    // subscriber both pass constants).
    public void SendChat(byte channel, string text, List<(byte, byte, int)> shares) =>
        Send(ProtocolOpcodes.ChannelChatRequest, b =>
        {
            b.U8(channel);
            b.Str(text);
            WriteShares(b, shares);
            b.I32(0);
            b.U8(0);
        });
}

}

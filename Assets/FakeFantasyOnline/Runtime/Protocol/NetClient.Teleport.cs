namespace FakeFantasyOnline.Protocol
{

// Official 1.5.8 NetClient.Teleport (0x060006e9, arm64 0x2682dc / 204 bytes, x86_64 0x277620 /
// 192 bytes), restored where the official NetClient.Send (0x06000772) actually lives. Both frozen
// bodies agree line for line: three metadata-usage class-init checks first, then allocate the
// display class NetClient+<>c__DisplayClass105_0 (usage 2931; a failed allocation raises, it does
// not skip), store transList at +0x10 and index at +0x14 in that order, allocate the object of
// usage 377 for Send's third argument and hand Send(0x1072) that argument.
//
// Usage 377 stays unresolved in the index (a TypeInfo slot), but the object it allocates is the
// third argument of Send, whose official shape is Send(ushort id, Action<Buf> body) - so that slot
// is the delegate Action<Buf> built from the closure and the writer method 0x060007c1 (usage 9562,
// NetClient+<>c__DisplayClass105_0.<Teleport>b__0), not a buffer. The buffer itself is allocated
// inside Send (Buf b = new Buf(); body(buffer);), i.e. after the connection gate, so a client that
// is not connected never writes these two ints and instead goes through OnError with 未连接服务器.
// The C# closure below is exactly that pair: a display class with one int field per argument and a
// method of two Buf.I32 writes, which is why the anonymous method is the carrier of 0x060007c1.
//
// The opcode is read off both command bodies, never from the private server. The command validates
// nothing - no range, no price, no map - and its single caller is TransportUI.DoTeleport
// (0x06000f96). index only names a row of the destination list the server sent for this NPC, so
// the map change still has to come from the server's own 0x803a teleport downlink.
public sealed partial class NetClient
{
    public void Teleport(int transList, int index) =>
        Send(ProtocolOpcodes.ChooseTransportRequest, b =>
        {
            b.I32(transList);
            b.I32(index);
        });
}
}

namespace FakeFantasyOnline.Protocol
{

// Official 1.5.8 NetClient.ArenaPick (0x06000744, arm64 0x26f050 / x86_64 0x27e580, 192 bytes) and
// NetClient.ArenaReroll (0x06000745, arm64 0x26f118 / x86_64 0x27e650, 268 bytes), restored where
// the official NetClient.Send (0x06000772) actually lives. Both bodies do exactly what the native
// bodies do and not one byte more:
//
//   ArenaPick(index)   allocate the display class NetClient+<>c__DisplayClass218_0
//                      (metadata usage 2989), store index at its +0x10, wrap the writer 0x06000835
//                      (metadata usage 9620) in an Action<Buf>, Send(0x1083, writer)
//   ArenaReroll()      no display class: build the cached NetClient+<>c (metadata usage 2925)
//                      Action<Buf> 0x060007a9 (metadata usage 9531) once, Send(0x1086, writer)
//
// Evidence: the frozen exports in
// Build/ReferenceReverseGhidra-1.5.8/queries/wbb030-arenapick-{arm64,x86_64} and
// .../wbb030-arenareroll-{arm64,x86_64} for the two commands, plus
// .../wbb030-arenapick-writer-arm64 (0x06000835) and .../wbb030-arenareroll-writer-arm64
// (0x060007a9) for the two writers. Both opcodes are read off the command bodies on both
// architectures (0x1083 / 0x1086), never from the private server.
//
// Serialization stays behind NetClient.Send's connected test (_tcp != null && _tcp.Connected), so
// the payload is only written once the socket is up; the disconnected branch reports through
// OnError("未连接服务器") and returns. Encoding before that test would move the write ahead of the
// official gate.
//
// The only callers are the two ArenaDraftDlg commands: Pick (0x06000052) sends the clicked card
// index and Reroll (0x06000051) sends nothing but the opcode. Which card the server actually deals
// and what the reroll costs stay server-authored.
public sealed partial class NetClient
{
    public void ArenaPick(int index)
    {
        Send(ProtocolOpcodes.ArenaPickRequest, buffer => WriteArenaPick(buffer, index));
    }

    public void ArenaReroll()
    {
        Send(ProtocolOpcodes.ArenaRerollRequest, WriteArenaReroll);
    }

    // Official 0x06000835 (NetClient+<>c__DisplayClass218_0.<ArenaPick>b__0), the Action<Buf>
    // NetClient.ArenaPick hands to Send. The native body is 32 bytes: the null-Buf check (a null
    // Buf is the IL2CPP raise FUN_00177a74, not a skipped write) and one Buf.U8 taken from the
    // captured int - the same truncation the official Buf.U8 performs on its int argument.
    internal static void WriteArenaPick(Buf buffer, int index)
    {
        buffer.U8(index);
    }

    // Official 0x060007a9 (NetClient+<>c.<ArenaReroll>b__219_0), the Action<Buf> NetClient.ArenaReroll
    // hands to Send. Its emitted extent is 4 bytes - a bare return - so the 0x1086 frame carries the
    // opcode and no payload at all. Nothing is written here on purpose.
    internal static void WriteArenaReroll(Buf buffer)
    {
    }
}
}

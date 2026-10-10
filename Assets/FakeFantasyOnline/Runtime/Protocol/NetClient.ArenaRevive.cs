namespace FakeFantasyOnline.Protocol
{

// Official 1.5.8 NetClient.ArenaRevive (0x06000746, arm64 0x26f224 / 192 bytes, x86_64 0x27e780),
// restored where the official NetClient.Send (0x06000772) actually lives. The body does exactly
// what the native body does and not one byte more: allocate the display class
// NetClient+<>c__DisplayClass220_0 (metadata usage 2990), store the surrender flag at its +0x10,
// wrap the writer 0x06000837 (metadata usage 9621) in an Action<Buf> and call Send with opcode
// 0x1084.
//
// Serialization stays behind NetClient.Send's connected test (_tcp != null && _tcp.Connected), so
// the single U8 is only written once the socket is up; the disconnected branch reports through
// OnError("未连接服务器") and returns. Encoding before that test would move the write ahead of the
// official gate.
//
// The two OnGUI buttons of ArenaDeathDlg are the only callers: 复活 sends surrender = false and
// 认输 sends surrender = true. There is no separate "leave the arena" packet in 1.5.8 - the server
// decides the outcome and answers on its own opcodes.
public sealed partial class NetClient
{
    public void ArenaRevive(bool surrender)
    {
        Send(ProtocolOpcodes.ArenaReviveRequest, buffer => WriteArenaRevive(buffer, surrender));
    }

    // Official 0x06000837 (NetClient+<>c__DisplayClass220_0.<ArenaRevive>b__0), the Action<Buf>
    // NetClient.ArenaRevive hands to Send. Both architectures validate nothing: a null Buf is the
    // IL2CPP raise (FUN_00177a74 arm64 / FUN_0016c1b0 x86_64), not a skipped write, and the flag is
    // one Buf.U8 - 0 to revive, 1 to surrender.
    internal static void WriteArenaRevive(Buf buffer, bool surrender)
    {
        buffer.U8(surrender ? 1 : 0);
    }
}
}

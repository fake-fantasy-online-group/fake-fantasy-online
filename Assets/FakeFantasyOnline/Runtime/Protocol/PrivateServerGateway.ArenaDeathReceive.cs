using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // Official 0600077f, bounded 0x8061 branch (jump-table index 96 -> arm64 0x2528d0). Publish
    // only after every entered read succeeds; the read order and the trailing Remaining gate are
    // the native ones:
    //
    //   Buf.U8  (arm64 0x26afcc)  flag     - 0 tail-calls ArenaDeathDlg.Close, anything else shows
    //   Buf.U8                    deathNo
    //   Buf.U8                    freeLeft
    //   Buf.U8                    kind
    //   Buf.I64 (arm64 0x274eac)  amount
    //   Buf.Str (arm64 0x26b088)  label
    //   Buf.U8                    afford
    //   Buf.I32 (arm64 0x26b00c)  waitSec
    //   Buf.Remaining <= 1 ? "" : Buf.Str()   ladder
    //
    // Both architectures read all nine values before testing the flag, so the close branch is a
    // tail call after the reads and not a separate decoder.
    private static void ReceiveArenaDeath(byte[] payload, IArenaDeathReceiveContext context)
    {
        var reader = new NetClient.Buf(payload);
        int flag = reader.U8();
        int deathNo = reader.U8();
        int freeLeft = reader.U8();
        int kind = reader.U8();
        long amount = reader.I64();
        string label = reader.Str();
        bool afford = reader.U8() != 0;
        int waitSec = reader.I32();
        string ladder = reader.Remaining <= 1 ? "" : reader.Str();
        context.ReceiveArenaDeath(new ArenaDeathStateData(flag, deathNo, freeLeft, kind, amount,
            label, afford, waitSec, ladder));
    }
}
}

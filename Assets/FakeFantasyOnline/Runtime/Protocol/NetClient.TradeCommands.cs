using System.Collections.Generic;

namespace FakeFantasyOnline.Protocol
{

// Official 1.5.8 NetClient trade commands 0x06000751..0x06000754, restored where the official
// NetClient.Send (0x06000772) actually lives. Each member does exactly what the native body does -
// capture the arguments, wrap the writer in an Action<Buf>, call Send with the opcode - and not one
// byte more:
//
//   TradeOffer   0x06000751  display class +0x10 = money, +0x18 = the offer list (by reference,
//                            write barrier, arm64 0x26fbb4 / x86_64 0x27f140 agree)
//                            -> Action<Buf> 0x0600084d -> Send 0x1041
//   TradeLock    0x06000752  display class +0x10 = on -> Action<Buf> 0x0600084f -> Send 0x1042
//   TradeConfirm 0x06000753  cached Action<Buf> 0x060007aa at NetClient+<>c +0x80 -> Send 0x1043
//   TradeCancel  0x06000754  cached Action<Buf> 0x060007ab at NetClient+<>c +0x88 -> Send 0x1044
//
// Serialization timing is the point of putting them here: NetClient.Send builds the Buf and runs
// the writer only after its connected test (_tcp != null && _tcp.Connected), so while the socket is
// down nothing is serialized and a null offer list cannot raise; the disconnected branch reports
// through OnError("未连接服务器") and returns. Encoding before that test would move both the write
// and the null raise ahead of the official gate.
//
// The capture is a display class for the two commands that carry arguments and a compiler-cached
// delegate for the two that carry none, which is what the C# lambdas below produce as well. The
// list is captured by reference, never copied, and is read when the writer runs.
public sealed partial class NetClient
{
    public void TradeOffer(long money, List<(byte Tab, int Slot, int Count)> items)
    {
        Send(ProtocolOpcodes.TradeOffer, buffer => WriteOffer(buffer, money, items));
    }

    public void TradeLock(bool on)
    {
        Send(ProtocolOpcodes.TradeLock, buffer => WriteLock(buffer, on));
    }

    public void TradeConfirm()
    {
        Send(ProtocolOpcodes.TradeConfirm, buffer => { });
    }

    public void TradeCancel()
    {
        Send(ProtocolOpcodes.TradeCancel, buffer => { });
    }

    // Official 0x0600084d (NetClient+<>c__DisplayClass231_0.<TradeOffer>b__0), the Action<Buf>
    // NetClient.TradeOffer hands to Send. Both architectures store the same three fields in the
    // same order (8/1/1+4+4 bytes) and validate nothing: money and the row fields are written as
    // given, the count is the raw List _size truncated to one byte by Buf.U8 while the loop still
    // walks every element, and a null list is the IL2CPP raise (FUN_00177a74 arm64 / FUN_0016c1b0
    // x86_64) rather than an empty offer. The loop is the official List<T> struct enumerator -
    // version-checked and disposed - not an indexer, and the element order is the caller's order.
    internal static void WriteOffer(Buf buffer, long money, List<(byte Tab, int Slot, int Count)> items)
    {
        buffer.I64(money);
        buffer.U8(items.Count);
        foreach ((byte Tab, int Slot, int Count) item in items)
        {
            buffer.U8(item.Tab);
            buffer.I32(item.Slot);
            buffer.I32(item.Count);
        }
    }

    // Official 0x0600084f (NetClient+<>c__DisplayClass232_0.<TradeLock>b__0): one Buf.U8 of the
    // captured bool.
    internal static void WriteLock(Buf buffer, bool on)
    {
        buffer.U8(on ? 1 : 0);
    }
}
}

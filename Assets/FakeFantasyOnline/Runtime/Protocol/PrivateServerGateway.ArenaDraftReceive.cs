using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // Official 0600077f, bounded 0x8060 branch (jump-table index 95 -> arm64 0x24d2cc). Publish
    // only after every entered read succeeds; the read order is the native one:
    //
    //   Buf.U8  (arm64 0x26afcc)  flag       - 0 tail-calls ArenaDraftDlg.Close, anything else shows
    //   Buf.U8                    round
    //   Buf.U8                    total
    //   Buf.I32 (arm64 0x26b00c)  sec
    //   Buf.U8                    count      - capacity of the List<Card> built below
    //   count x { Card..ctor (0x06000056); Buf.I32 skillId; Buf.I32 lv;
    //             Buf.Str (arm64 0x26b088) name; Buf.Str desc }
    //   Buf.U8                    rerollFree
    //   Buf.I64 (arm64 0x274eac)  rerollCost
    //
    // Both architectures perform every read before testing the flag, so the close branch is a tail
    // call after the reads and not a separate decoder.
    private static void ReceiveArenaDraft(byte[] payload, IArenaDraftReceiveContext context)
    {
        var reader = new NetClient.Buf(payload);
        int flag = reader.U8();
        int round = reader.U8();
        int total = reader.U8();
        int sec = reader.I32();
        int count = reader.U8();
        var cards = new List<ArenaDraftCardData>(count);
        for (int i = 0; i < count; i++)
        {
            // The native branch constructs the Card first and then fills it (arm64 0x24d38c).
            int skillId = reader.I32();
            int lv = reader.I32();
            string name = reader.Str();
            string desc = reader.Str();
            cards.Add(new ArenaDraftCardData(skillId, lv, name, desc));
        }
        int rerollFree = reader.U8();
        long rerollCost = reader.I64();
        context.ReceiveArenaDraft(new ArenaDraftStateData(flag, round, total, sec, cards,
            rerollFree, rerollCost));
    }
}
}

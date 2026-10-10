using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class NetClient
    {
        // Official 0x060006dd. Declaration evidence: frozen IL dump
        // Build/ReferenceManagedRecovery-1.5.8/il/Assembly-CSharp.il line 1086584-1086588,
        // ".method private hidebysig static void WriteShares(class NetClient/Buf b,
        // class List`1<valuetype ValueTuple`3<uint8, uint8, int32>> shares)".
        // Body evidence: frozen Ghidra packet-NetClient-WriteShares/060006dd.c, arm64 rva 0x26755c
        // (528 bytes) and x86_64 rva 0x2769e0 (256 bytes); both agree, the arm64 body is unrolled
        // four times and the x86_64 body is a do-while over the same three writes.
        // Read timing: both architectures call the List indexer once per write and never hoist the
        // tuple - x86_64 FUN_0093aa80 at 060006dd.c lines 46/48/50 sits directly before
        // U8(Item1)/U8(Item2)/I32(Item3), and arm64 FUN_0092ee40 repeats the same pattern at
        // lines 47/49/51, 54/56/58, 61/63/65 and 68/70/72. The element must therefore be indexed
        // three times per entry, once inside each write expression.
        //
        // Owner: the official caller is <SendHorn>b__0 (0x060008ae), now restored in
        // NetClient.Horn.cs, which hands the captured list to this method unchanged. The chat
        // codec keeps its own WireWriter share writer and is untouched by this member.
        //
        // Faithful points kept on purpose, do not "fix":
        //   * A null shares list writes the count 0 and returns; it never throws.
        //   * The count is clamped to 4 (official tests `3 < count` on arm64, `count < 4` on
        //     x86_64); overflowing entries are dropped silently, not reported.
        //   * The count is read once up front; the entry is re-read through the List indexer
        //     before every single write, so each of the three fields is fetched separately.
        //   * Per entry the order is byte, byte, Int32, i.e. Buf.U8, Buf.U8, Buf.I32, so one entry
        //     is six bytes, not four.
        //   * A null b throws NullReferenceException after the count is computed but before the
        //     count byte is written; calling U8 on a null Buf produces exactly that, so no explicit
        //     throw is written here.
        private static void WriteShares(Buf b, List<(byte, byte, int)> shares)
        {
            int count = shares == null ? 0 : Math.Min(shares.Count, 4);
            b.U8(count);
            for (int i = 0; i < count; i++)
            {
                b.U8(shares[i].Item1);
                b.U8(shares[i].Item2);
                b.I32(shares[i].Item3);
            }
        }
    }
}

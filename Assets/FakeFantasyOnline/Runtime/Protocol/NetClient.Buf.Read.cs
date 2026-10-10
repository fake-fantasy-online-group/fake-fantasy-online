using System;
using System.Text;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class NetClient
{
    // Internal visibility connects the recovered nested reader to the independent dispatch codec.
    // partial so the official write side lives in NetClient.Buf.Write.cs without moving these readers.
    internal partial class Buf : FakeFantasyOnline.Application.ISkillBookReader
    {
        private byte[] _b;
        private int _pos;
        private int _len;

        // Official 0x06000788 (arm64 0x274624 / 72 bytes, x86_64 0x283c00 / 64 bytes):
        // the argument is stored by reference - no copy, no length clamp - and the null
        // guard sits between the two stores, so a null array throws NullReferenceException
        // (arm64 cbz x20 -> bl 0x177a74) after _b was assigned and before _len is written.
        // _pos is never assigned and stays 0.
        public Buf(byte[] data)
        {
            _b = data;
            _len = data.Length;
        }

        // Official 0x06000791 (arm64 0x26afc0 / 12 bytes): _len - _pos, unchecked and with
        // no clamp, so a reader past the end reports a negative Remaining.
        public int Remaining => unchecked(_len - _pos);

        // Official 0x06000792 (arm64 0x26afcc / 64 bytes): _b is read, _pos is read, _pos
        // is written back as _pos + 1 and only then are the null and bounds checks made
        // (arm64 str w10,[x0,#0x18] before cbz x8 and before cmp w9,w10 / b.hs). That is
        // exactly `_b[_pos++]`, and the consequence is kept: a throw still consumes one
        // byte, so _pos is one past the failing index.
        public int U8() => _b[unchecked(_pos++)];

        // Official 0x06000793 (arm64 0x26b690 / 84 bytes): the null check comes first, then
        // both bounds checks, then the two reads, then _pos + 2, and the two bytes are
        // combined only afterwards. A throw therefore leaves _pos untouched.
        public int U16()
        {
            int value = _b[_pos] | (_b[unchecked(_pos + 1)] << 8);
            _pos = unchecked(_pos + 2);
            return value;
        }

        // Official 06000794 (arm64 0x274e50 / 92 bytes): same check/read/_pos order as
        // U16, then the high byte is shifted and sign-extended as a halfword
        // (arm64 lsl w8,w8,#8; sxth w8,w8; orr w0,w8,w11) before the low byte is or-ed in.
        public int I16()
        {
            int value = _b[_pos] | (_b[unchecked(_pos + 1)] << 8);
            _pos = unchecked(_pos + 2);
            return unchecked((short)value);
        }

        // Official 0x06000795 (arm64 0x26b00c / 124 bytes): four bounds checks precede the
        // four reads and _pos advances by 4 only after all of them, so a short buffer
        // throws with _pos unchanged.
        public int I32()
        {
            int value = _b[_pos] | (_b[unchecked(_pos + 1)] << 8) |
                (_b[unchecked(_pos + 2)] << 16) | (_b[unchecked(_pos + 3)] << 24);
            _pos = unchecked(_pos + 4);
            return value;
        }

        // Official 0x06000796 (arm64 0x274eac / 192 bytes): eight bounds checks precede the
        // eight reads (arm64 ccmp chain) and _pos advances by 8 only after all of them.
        public long I64()
        {
            long value = _b[_pos] | ((long)_b[unchecked(_pos + 1)] << 8) |
                ((long)_b[unchecked(_pos + 2)] << 16) | ((long)_b[unchecked(_pos + 3)] << 24) |
                ((long)_b[unchecked(_pos + 4)] << 32) | ((long)_b[unchecked(_pos + 5)] << 40) |
                ((long)_b[unchecked(_pos + 6)] << 48) | ((long)_b[unchecked(_pos + 7)] << 56);
            _pos = unchecked(_pos + 8);
            return value;
        }

        // Official 1.5.8 NetClient.Buf.F32 (0x06000797, arm64 0x274f6c / 56 bytes,
        // x86_64 0x284160 / 48 bytes). Both architectures do exactly two things:
        //   arm64 : ldr x0,[x0,#0x10]       _b (+0x10)
        //           ldr w1,[x19,#0x18]      _pos (+0x18)
        //           mov x2,#0               null methodInfo
        //           bl #0xe8211c            System.BitConverter.ToSingle(byte[], int)
        //           ldr w8,[x19,#0x18]; add w8,w8,#4; str w8,[x19,#0x18]   _pos += 4
        //           ret                     s0 is not touched after the call
        //   x86_64: mov rdi,[rdi+0x10]; mov esi,[rbx+0x18]; xor edx,edx
        //           call 0xe81900
        //           add dword ptr [rbx+0x18],4
        //           ret                     xmm0 is not touched after the call
        // The call is a real call and not a tail jump, and neither body contains an
        // int-to-float conversion (no scvtf / cvtsi2ss): the four bytes at _pos are
        // reinterpreted, which is also what the recovered managed source says
        // (Build/ReferenceManagedRecovery-1.5.8/source/NetClient.cs:1303).
        // Callee identity: arm64 0xe8211c is labelled
        // mscorlib.dll::System.BitConverter.System.Single ToSingle(System.Byte[] value,
        // System.Int32 startIndex) (156 bytes) in the shared index labels table; it is a
        // different symbol from Mono.Security.BitConverterLE.ToSingle (arm64 0xd543f4)
        // and from BitConverter.Int32BitsToSingle (arm64 0xe824f8). The x86_64 callee
        // 0xe81900 is not labelled inside packet-WB002-NetClientBufWrite, so the second
        // architecture confirms the call shape - two managed arguments plus a null
        // methodInfo, float in xmm0 - and not a symbol; the recovered managed source is
        // the tie-breaker.
        // No byte reversal and no default: the official body neither reverses the four
        // bytes nor substitutes a default or NaN, so the product calls the same BCL
        // method and returns whatever it returns. _pos advances by 4 only after the
        // conversion succeeded - the add sits after bl/call on both architectures - so a
        // null buffer or an out-of-range _pos leaves _pos where it was and throws
        // whatever BitConverter.ToSingle throws.
        // Consumer (WB-018): the six atlas call edges were resolved to three Dispatch cases by
        // decoding the native switch jump tables, and the three matching product adapter reads now
        // go through this member instead of the independent WireReader codec:
        //   0x8003 (arm64 block 0x250698, F32 at 0x250768 and 0x250778) ->
        //       PrivateServerGateway.EnterWorldAsync, x and y stay floats
        //   0x8004 (arm64 block 0x250df8, F32 at 0x250ed4 and 0x250ee4) ->
        //       PrivateServerGateway.ReadRoster, x and y stay floats
        //   0x8023 (arm64 block 0x252714, F32 at 0x252744 and 0x252754) ->
        //       PrivateServerWorldStateDecoder.TryDecodeRemotePlayerState, both kept as raw
        //       floats across the whole read batch and converted with unchecked((int)) at the
        //       spawn use point, matching arm64 0x258b64 / 0x258b7c
        // The official NetClient.Dispatch body (0x0600077F) itself is still not restored, so the
        // connection is per branch and not a whole-method restoration.
        public float F32()
        {
            float value = BitConverter.ToSingle(_b, _pos);
            _pos = unchecked(_pos + 4);
            return value;
        }

        // Official 1.5.8 NetClient.Buf.Str (0x06000798, arm64 0x26b088 / 156 bytes,
        // x86_64 0x27a6e0 / 144 bytes). The length prefix is read inline, not through a
        // U16() call: both bodies load _b, throw NullReferenceException if it is null
        // (arm64 cbz x8 -> bl 0x177a74), compare _pos and _pos + 1 against the array
        // length and throw IndexOutOfRangeException if either is out of range
        // (arm64 b.hs -> bl 0x177a80), read the two bytes, store _pos + 2, then call
        // Encoding.get_UTF8 (arm64 bl 0xf5e1ac, methodInfo 0) with its own null guard,
        // re-read _b and _pos for the virtual Encoding.GetString(_b, _pos, count) call
        // (vtable slot +0x358) and only then add count to _pos. The recovered managed
        // source shows the same inline shape and no U16() call, so the previous product
        // `int count = U16();` - a real call the official body does not make - is
        // replaced by the inlined read. The observable sequence is unchanged: the same
        // two bounds checks, the same two bytes, _pos + 2 before the encoding call and
        // _pos + count only after it returns, so a throw from GetString still leaves
        // _pos advanced by exactly 2.
        public string Str()
        {
            int count = _b[_pos] | (_b[unchecked(_pos + 1)] << 8);
            _pos = unchecked(_pos + 2);
            string value = Encoding.UTF8.GetString(_b, _pos, count);
            _pos = unchecked(_pos + count);
            return value;
        }
    }
}

}

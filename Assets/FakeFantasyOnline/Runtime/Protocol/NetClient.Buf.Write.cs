using System.Text;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class NetClient
    {
        // Official 1.5.8 NetClient+Buf write side, recovered as one owner-only submodule.
        // Identity evidence: frozen IL dump Build/ReferenceManagedRecovery-1.5.8/il/Assembly-CSharp.il
        // lines 1064297-1067765 (nested private Buf, fields _b/_pos/_len, [opt] cap with
        // .param [1] = int32(64)); frozen Ghidra queries packet-WB002-NetClientBufWrite (arm64 and
        // x86_64) for the bodies. See .workbuddy/memory/WB-002-buf-write-evidence.md.
        //
        // Faithful points kept on purpose, do not "fix":
        //   * Ens doubles from the CURRENT array length and has no zero-length or overflow guard.
        //   * Every write advances _pos one byte at a time, before the store it belongs to.
        //   * I32 reserves four bytes once, then writes little-endian low byte first.
        //   * Str uses the replacing Encoding.UTF8, treats null as "", truncates the U16 length
        //     prefix to 16 bits and still copies the whole payload.
        //   * I64 reserves one byte eight times, never eight bytes once; Bytes writes no length
        //     prefix and raises NullReferenceException on a null array instead of treating it
        //     as empty.
        //   * No writer touches _len, so Remaining stays a read-side notion.
        internal partial class Buf
        {
            // Official 0x06000787. Metadata declares [opt] int32 cap with .param [1] = int32(64).
            public Buf(int cap = 64)
            {
                _b = new byte[cap];
            }

            // Official 0x06000789. Declared private in the official metadata. The release binary has
            // no non-inlined call site because the native compiler inlined it into every writer; the
            // source call is real, so the writers below call it.
            private void Ens(int n)
            {
                int length = _b.Length;
                int need = unchecked(_pos + n);
                if (length >= need)
                {
                    return;
                }
                do
                {
                    length = unchecked(length * 2);
                }
                while (length < need);
                System.Array.Resize(ref _b, length);
            }

            // Official 0x0600078a.
            public void U8(int v)
            {
                Ens(1);
                _b[unchecked(_pos++)] = unchecked((byte)v);
            }

            // Official 0x0600078b.
            public void U16(int v)
            {
                Ens(2);
                _b[unchecked(_pos++)] = unchecked((byte)v);
                _b[unchecked(_pos++)] = unchecked((byte)(v >> 8));
            }

            // Official 0x0600078c.
            public void I32(int v)
            {
                Ens(4);
                _b[unchecked(_pos++)] = unchecked((byte)v);
                _b[unchecked(_pos++)] = unchecked((byte)(v >> 8));
                _b[unchecked(_pos++)] = unchecked((byte)(v >> 16));
                _b[unchecked(_pos++)] = unchecked((byte)(v >> 24));
            }

            // Official 0x0600078d. Unlike I32 this member reserves one byte EIGHT times instead of
            // eight bytes once: arm64 0x2747ec (1164 bytes) unrolls eight complete one-byte Ens
            // expansions, x86_64 0x283d80 (224 bytes) loops over the same body with the counter
            // compared against 8, and the frozen IL repeats "ldfld _pos / ldc.i4 1" once per byte.
            // Low byte first, little-endian, shift (i * 8) & 0x3f.
            public void I64(long v)
            {
                for (int i = 0; i < 8; i++)
                {
                    Ens(1);
                    _b[unchecked(_pos++)] = unchecked((byte)(v >> (i * 8)));
                }
            }

            // Official 0x0600078e. Null becomes the empty string, never an exception; the length
            // prefix is U16 so it truncates above 65535 while the payload is still copied in full.
            public void Str(string s)
            {
                byte[] encoded = Encoding.UTF8.GetBytes(s ?? "");
                U16(encoded.Length);
                Ens(encoded.Length);
                System.Array.Copy(encoded, 0, _b, _pos, encoded.Length);
                _pos = unchecked(_pos + encoded.Length);
            }

            // Official 0x0600078f. No length prefix at all: the payload is copied raw. A null array
            // is not tolerated the way Str tolerates a null string - raw.Length is read first and
            // raises NullReferenceException on both architectures (arm64 0x274d94, x86_64 0x283f70),
            // before the buffer null check and before anything is written. Capacity is reserved once
            // for _pos + raw.Length, Array.Copy copies the whole array, and _pos grows by the full
            // length once, after the copy.
            public void Bytes(byte[] raw)
            {
                Ens(raw.Length);
                System.Array.Copy(raw, 0, _b, _pos, raw.Length);
                _pos = unchecked(_pos + raw.Length);
            }

            // Official 0x06000790. The written length is _pos, never _len.
            public byte[] ToArray()
            {
                byte[] result = new byte[_pos];
                System.Array.Copy(_b, result, _pos);
                return result;
            }
        }
    }
}

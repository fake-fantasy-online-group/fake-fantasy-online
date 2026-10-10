using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FakeFantasyOnline.Protocol
{

internal sealed class WireWriter
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
    private readonly List<byte> _bytes = new List<byte>();

    public void WriteByte(byte value)
    {
        _bytes.Add(value);
    }

    public void WriteUInt16(ushort value)
    {
        _bytes.Add((byte)value);
        _bytes.Add((byte)(value >> 8));
    }

    public void WriteInt16(short value)
    {
        WriteUInt16(unchecked((ushort)value));
    }

    public void WriteInt32(int value)
    {
        WriteUInt32(unchecked((uint)value));
    }

    public void WriteUInt32(uint value)
    {
        _bytes.Add((byte)value);
        _bytes.Add((byte)(value >> 8));
        _bytes.Add((byte)(value >> 16));
        _bytes.Add((byte)(value >> 24));
    }

    public void WriteUInt64(ulong value)
    {
        _bytes.Add((byte)value);
        _bytes.Add((byte)(value >> 8));
        _bytes.Add((byte)(value >> 16));
        _bytes.Add((byte)(value >> 24));
        _bytes.Add((byte)(value >> 32));
        _bytes.Add((byte)(value >> 40));
        _bytes.Add((byte)(value >> 48));
        _bytes.Add((byte)(value >> 56));
    }

    public void WriteInt64(long value)
    {
        ulong unsigned = unchecked((ulong)value);
        _bytes.Add((byte)unsigned);
        _bytes.Add((byte)(unsigned >> 8));
        _bytes.Add((byte)(unsigned >> 16));
        _bytes.Add((byte)(unsigned >> 24));
        _bytes.Add((byte)(unsigned >> 32));
        _bytes.Add((byte)(unsigned >> 40));
        _bytes.Add((byte)(unsigned >> 48));
        _bytes.Add((byte)(unsigned >> 56));
    }

    public void WriteString(string value)
    {
        byte[] encoded = Utf8.GetBytes(value ?? throw new ArgumentNullException(nameof(value)));
        if (encoded.Length > ushort.MaxValue)
        {
            throw new InvalidDataException("Wire string is too long.");
        }

        WriteUInt16((ushort)encoded.Length);
        _bytes.AddRange(encoded);
    }

    // Native NetClient.Buf.Str: replacement UTF-8 and a low-16-bit byte length.
    public void WriteNativeString(string value)
    {
        byte[] encoded = Encoding.UTF8.GetBytes(value ?? "");
        WriteUInt16(unchecked((ushort)encoded.Length));
        _bytes.AddRange(encoded);
    }

    public void WriteBytes(byte[] bytes)
    {
        _bytes.AddRange(bytes ?? throw new ArgumentNullException(nameof(bytes)));
    }

    public byte[] ToArray()
    {
        return _bytes.ToArray();
    }
}

internal sealed class WireReader
{
    // Official 1.5.8 NetClient.Buf.Str (0x06000798, arm64 0x26b088 / x86_64 0x27a6e0) decodes
    // through Encoding.get_UTF8 (arm64 bl 0xf5e1ac, x86_64 call 0xf58900) and the virtual
    // Encoding.GetString, so invalid UTF8 uses replacement (U+FFFD) rather than
    // DecoderFallbackException. Network string reads therefore use that same Encoding object
    // instead of a throwOnInvalidBytes instance, whose DecoderFallbackException dropped whole
    // packets the official client decodes. Length prefix, bounds and cursor order are unchanged.
    private static readonly Encoding Utf8 = Encoding.UTF8;
    private readonly byte[] _bytes;
    private int _position;

    public WireReader(byte[] bytes)
    {
        _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
    }

    public int Remaining => _bytes.Length - _position;

    public byte ReadByte()
    {
        Require(1);
        return _bytes[_position++];
    }

    public ushort ReadUInt16()
    {
        Require(2);
        ushort value = (ushort)(_bytes[_position] | (_bytes[_position + 1] << 8));
        _position += 2;
        return value;
    }

    public short ReadInt16()
    {
        return unchecked((short)ReadUInt16());
    }

    public int ReadInt32()
    {
        return unchecked((int)ReadUInt32());
    }

    public uint ReadUInt32()
    {
        Require(4);
        uint value = _bytes[_position]
            | ((uint)_bytes[_position + 1] << 8)
            | ((uint)_bytes[_position + 2] << 16)
            | ((uint)_bytes[_position + 3] << 24);
        _position += 4;
        return value;
    }

    public long ReadInt64()
    {
        return unchecked((long)ReadUInt64());
    }

    public ulong ReadUInt64()
    {
        Require(8);
        ulong value = _bytes[_position]
            | ((ulong)_bytes[_position + 1] << 8)
            | ((ulong)_bytes[_position + 2] << 16)
            | ((ulong)_bytes[_position + 3] << 24)
            | ((ulong)_bytes[_position + 4] << 32)
            | ((ulong)_bytes[_position + 5] << 40)
            | ((ulong)_bytes[_position + 6] << 48)
            | ((ulong)_bytes[_position + 7] << 56);
        _position += 8;
        return value;
    }

    public float ReadFloat32()
    {
        int bits = ReadInt32();
        return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
    }

    public string ReadString()
    {
        int byteCount = ReadUInt16();
        Require(byteCount);
        string value = Utf8.GetString(_bytes, _position, byteCount);
        _position += byteCount;
        return value;
    }

    public byte[] ReadBytes(int count)
    {
        Require(count);
        byte[] result = new byte[count];
        Buffer.BlockCopy(_bytes, _position, result, 0, count);
        _position += count;
        return result;
    }

    public void EnsureFinished()
    {
        if (Remaining != 0)
        {
            throw new InvalidDataException("Wire payload has trailing bytes.");
        }
    }

    private void Require(int count)
    {
        if (count < 0 || count > Remaining)
        {
            throw new EndOfStreamException("Wire payload ended unexpectedly.");
        }
    }
}
}

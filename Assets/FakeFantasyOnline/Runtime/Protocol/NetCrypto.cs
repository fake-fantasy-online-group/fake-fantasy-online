using System;
using System.Security.Cryptography;
using System.Text;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Recovered official 1.5.8 session-crypto owner.
/// </summary>
public static class NetCrypto
{
    private const string PublicModulus =
        "pe3BCe0Vr8BcesC7oGZNCBThw1C56Ixh6bPonp7wWUl5Q2pLvpU6Q1FQTTgsrTu+D7KU6f+Gv0C0ENbi77llXyBMHGBuw2/S9V0SwrQLX7lt7s5p04gdD3KEYt3lmvb4w48qhkOMUAx90WDm5ENHBgxhPXys0es03pi5xm0p5iS+XSZDq60+uhn19xCDUokTUQyE/ShDBf9xlFgBaUIBDVEMqgBvwYZP5bg3mP1SFdGnuVyhniGvWySTqpzNatr68jfWJ/WZoBd3RaKfj/lzxAQ+gwhVhMuXek86ThhsUhjzgC/w5qxRjU1OdSTMPu3ybYk66+wEEb5JW83We7uhuQ==";
    private const string PublicExponent = "AQAB";
    private const int IvLength = 16;
    private const int MacLength = 32;
    private const int KeyLength = 32;
    private const int MinimumEncryptedBodyLength = 64;

    private static readonly byte[] _ak0 =
        { 0x5a, 0xfc, 0x45, 0xe1, 0xb3, 0x93, 0xf8, 0xab };
    private static readonly byte[] _ak1 =
        { 0x20, 0x96, 0xb1, 0xa1, 0xa9, 0x8c, 0x68, 0xd3 };
    private static readonly byte[] _ak2 =
        { 0xcd, 0x0a, 0xbd, 0xa5, 0x16, 0x36, 0xf6, 0xdf };
    private static readonly byte[] _ak3 =
        { 0xc9, 0x58, 0x98, 0x24, 0x8a, 0xf2, 0x0d, 0xfa };
    private const byte AuthRotate = 0x45;
    private static readonly byte[] ClientAuthKey = ComposeClientAuthKey();

    [ThreadStatic]
    private static Aes _aesTls;

    [ThreadStatic]
    private static RandomNumberGenerator _rngTls;

    private static byte[] ComposeClientAuthKey()
    {
        byte[] result = new byte[KeyLength];
        for (int index = 0; index < 8; index++)
        {
            result[index] = _ak0[index];
            result[index + 8] = (byte)(_ak1[index] ^ AuthRotate);
            result[index + 16] = (byte)((_ak2[index] << 1) | (_ak2[index] >> 7));
            result[index + 24] = (byte)(_ak3[index] ^ _ak1[(index + 3) & 7]);
        }

        return result;
    }

    public static byte[] ComputeAuthTag(
        byte[] aesKey,
        byte[] macKey,
        string clientVersion)
    {
        if (aesKey == null)
        {
            throw new ArgumentNullException(nameof(aesKey));
        }

        if (macKey == null)
        {
            throw new ArgumentNullException(nameof(macKey));
        }

        byte[] versionBytes = Encoding.UTF8.GetBytes(clientVersion ?? string.Empty);
        using (HMACSHA256 hmac = new HMACSHA256(ClientAuthKey))
        {
            hmac.TransformBlock(aesKey, 0, aesKey.Length, null, 0);
            hmac.TransformBlock(macKey, 0, macKey.Length, null, 0);
            hmac.TransformFinalBlock(versionBytes, 0, versionBytes.Length);
            return (byte[])hmac.Hash.Clone();
        }
    }

    private static RSA CreateServerPub()
    {
        return CreateServerPub(
            Convert.FromBase64String(PublicModulus),
            Convert.FromBase64String(PublicExponent));
    }

    internal static RSA CreateServerPub(byte[] modulus, byte[] exponent)
    {
        RSA rsa = RSA.Create();
        rsa.ImportParameters(
            new RSAParameters
            {
                Modulus = (byte[])modulus.Clone(),
                Exponent = (byte[])exponent.Clone()
            });
        return rsa;
    }

    public static byte[] ClientHandshake(out byte[] aesKey, out byte[] macKey)
    {
        using (RSA serverPublic = CreateServerPub())
        {
            return ClientHandshake(serverPublic, out aesKey, out macKey);
        }
    }

    internal static byte[] ClientHandshake(
        RSA serverPublic,
        out byte[] aesKey,
        out byte[] macKey)
    {
        if (serverPublic == null)
        {
            throw new ArgumentNullException(nameof(serverPublic));
        }

        byte[] secret = new byte[KeyLength * 2];
        FillRandom(secret);
        aesKey = new byte[KeyLength];
        macKey = new byte[KeyLength];
        Buffer.BlockCopy(secret, 0, aesKey, 0, KeyLength);
        Buffer.BlockCopy(secret, KeyLength, macKey, 0, KeyLength);

        byte[] ciphertext = serverPublic.Encrypt(secret, RSAEncryptionPadding.OaepSHA1);
        byte[] result = new byte[4 + ciphertext.Length];
        WriteLen(result, ciphertext.Length);
        Buffer.BlockCopy(ciphertext, 0, result, 4, ciphertext.Length);
        Array.Clear(secret, 0, secret.Length);
        return result;
    }

    // Official 060008cb AesTls(). arm64 0x2794d0 / x86_64 0x2813d0, frozen IL
    // Assembly-CSharp.il:1119845: Aes.Create() then set_Mode with ldc.i4 1 (CipherMode.CBC)
    // and set_Padding with ldc.i4 2 (PaddingMode.PKCS7), in that order, and nothing else -
    // no KeySize and no BlockSize assignment exists in the official body. The thread-static
    // store happens AFTER both setters (arm64 lines 51-59), so a setter failure leaves the
    // cache unset; the product previously stored first and configured the field afterwards.
    private static Aes AesTls()
    {
        Aes aes = _aesTls;
        if (aes != null)
        {
            return aes;
        }

        aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        _aesTls = aes;
        return aes;
    }

    // Official 060008cc FillRandom(byte[] buf). arm64 0x2795ec / x86_64 0x28157c. The official
    // body has NO null guard: it reads the thread-static generator, creates it when absent,
    // stores it, and then calls GetBytes, so a null buf fails inside the SDK call instead of
    // at an entry guard. The entry ArgumentNullException was a product addition with no
    // official basis and is removed.
    private static void FillRandom(byte[] buf)
    {
        if (_rngTls == null)
        {
            _rngTls = RandomNumberGenerator.Create();
        }

        _rngTls.GetBytes(buf);
    }

    // Official 060008cd EncryptFrame(byte[] inner, byte[] aesKey, byte[] macKey).
    // arm64 0x271b6c (840B) / x86_64 0x2811f0 (816B), frozen IL Assembly-CSharp.il:1120324.
    // Official order: new byte[16] -> FillRandom(iv) -> AesTls() -> set_Key(aesKey) ->
    // set_IV(iv) -> CreateEncryptor() -> and only then is `inner` null-checked, so a null
    // inner raises NullReferenceException at the length read, not at an entry guard. The
    // entry ArgumentNullException was a product addition with no official basis and is
    // removed. The frame is [4-byte LE length][16-byte iv][ciphertext][32-byte tag]; the exact
    // constants and order are restored below against arm64 lines 93-108 / x86_64 lines 85-98.
    // The encryptor really is protected by a finally, not only by the normal path: the arm64
    // LSDA (0x12eab78) protects [0x271ca8,0x271ce4) - the ICryptoTransform lookup and the
    // TransformFinalBlock call - with landing pad 0x271e6c, which catches, then jumps into the
    // same null-checked IDisposable.Dispose block at 0x271cf4 the normal path falls into, and
    // joins at 0x271d54, where a pending exception is re-raised through the same noreturn
    // helper the explicit throw sites use (0x271e58); the non-catch selector path resumes with
    // __Unwind_Resume (arm64 stub 0x1126244). x86_64 repeats it: call-site [0x28131f,0x281358)
    // -> landing pad 0x2814ca -> Dispose block 0x28135b -> join 0x2813b6 -> raise 0x2814b9.
    // See Build/NetCryptoDisposeFinally-20260927/evidence-notes.md.
    public static byte[] EncryptFrame(byte[] inner, byte[] aesKey, byte[] macKey)
    {
        byte[] iv = new byte[IvLength];
        FillRandom(iv);

        byte[] ciphertext;
        Aes aes = AesTls();
        aes.Key = aesKey;
        aes.IV = iv;
        using (ICryptoTransform encryptor = aes.CreateEncryptor())
        {
            ciphertext = encryptor.TransformFinalBlock(inner, 0, inner.Length);
        }

        byte[] tag = Hmac(macKey, iv, ciphertext);

        // Frame assembly, taken instruction by instruction from arm64 lines 93-108 and x86_64
        // lines 85-98 (both agree):
        //   * ONLY ciphertext.Length is read. tag.Length is never read, so a null tag cannot
        //     raise here and no extra null test is introduced.
        //   * new byte[ciphertext.Length + 0x34]  (52 = 4 header + 16 iv + 32 tag).
        //   * The prefix is ciphertext.Length + 0x30 (48 = 16 iv + 32 tag), written as four
        //     inline little-endian byte stores with their IL2CPP bound tests - the official
        //     build does not call WriteLen here (no such call exists in the frozen IL).
        //   * Then three Buffer.BlockCopy calls: a fixed 0x10 bytes of iv at 4,
        //     ciphertext.Length bytes of cipher at 0x14, and a fixed 0x20 bytes of tag at
        //     ciphertext.Length + 0x14.
        byte[] frame = new byte[unchecked(ciphertext.Length + 0x34)];
        int frameLength = unchecked(ciphertext.Length + 0x30);
        frame[0] = (byte)frameLength;
        frame[1] = (byte)(frameLength >> 8);
        frame[2] = (byte)(frameLength >> 16);
        frame[3] = (byte)(frameLength >> 24);
        Buffer.BlockCopy(iv, 0, frame, 4, 0x10);
        Buffer.BlockCopy(ciphertext, 0, frame, 0x14, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, frame, unchecked(ciphertext.Length + 0x14), 0x20);
        return frame;
    }

    // Official 060008ce DecryptBody(byte[] body, byte[] aesKey, byte[] macKey).
    // arm64 0x272f4c (1516B) / x86_64 0x282600 (1088B), frozen Ghidra export
    // Build/ReferenceReverseGhidra-1.5.8/queries/{arm64/2292bf0f...,x86_64/eba42edd...}
    // /packet-NetCrypto/060008ce.c, cross-checked instruction by instruction.
    //
    // Entry: the only length guard is the official one. arm64 0x272fc0 `cbz x24 -> 0x273470`
    // and x86_64 0x28265a `test r14,r14; je 0x28296d` both jump to the noreturn NRE helper
    // (arm64 0x177a74 / x86_64 FUN_0016c1b0), so a null body raises NullReferenceException and
    // NOT an argument/data exception; the product's combined
    // "body == null || body.Length < 64 -> InvalidDataException" guard is removed and the
    // exception is carried by the length read. The official bound is `Length < 0x40`:
    // arm64 0x272fc4 `ldr w26,[x24,#0x18]; cmp w26,#0x3f; b.le -> 0x273494`,
    // x86_64 0x28266b `mov r13d,[r14+0x18]; cmp r13d,0x3f; jle 0x282992`. Both throw sites
    // build CryptographicException (metadata usage 1506) with the string literal "帧过短"
    // (metadata usage 17385), not InvalidDataException.
    //
    // Layout, exactly three buffers and three Buffer.BlockCopy calls in the official order
    // (arm64 0x272fe0-0x273064, x86_64 0x282679-0x282701):
    //   iv          = new byte[0x10];  BlockCopy(body, 0,                 iv, 0, 0x10)
    //   ciphertext  = new byte[len-0x30]; BlockCopy(body, 0x10,     ciphertext, 0, len-0x30)
    //   receivedTag = new byte[0x20];  BlockCopy(body, len-0x20, receivedTag, 0, 0x20)
    // The official build has NO ciphertext alignment guard: there is no `% 16`, no `& 15` and
    // no `< 16` test anywhere in the frozen body (the 0x40 minimum already makes
    // len-0x30 >= 0x10), so the product's extra "% 16 != 0 / < 16 -> InvalidDataException"
    // check had no official counterpart and is removed.
    //
    // MAC coverage and check order (x86_64 0x282706-0x2827fa, arm64 0x273068-0x2730a4):
    // a NetCrypto class-init check, then Hmac(macKey, iv, ciphertext) - macKey is argument 1,
    // iv argument 2, ciphertext argument 3, so the covered region is iv followed by the whole
    // ciphertext - then the null checks of BOTH tag buffers (each -> the same NRE helper),
    // then `receivedTag.Length != expectedTag.Length -> throw` (x86_64 0x282744
    // `cmp ecx,[rax+0x18]; jne 0x282972`, arm64 0x273098 `cmp w10,w8; b.ne 0x273474`), then
    // the byte comparison, and `difference != 0 -> throw` (x86_64 0x2827fa `jne 0x282972`).
    // Operand order: receivedTag is the left operand on both architectures - arm64 0x273090
    // reads the length of the tag copied out of the body (x22) and x86_64 0x28273f reads the
    // same buffer (r12), and both compare it against the length of the Hmac result, so the
    // product call FixedTimeEquals(receivedTag, expectedTag) is the official order.
    // Both throw sites are the same CryptographicException with the literal
    // "MAC 校验失败(篡改或密钥不符)" (metadata usage 12752). A zero-length tag skips the byte
    // loop and passes (arm64 0x2730a0 `cmp w10,#1; b.lt -> 0x2732f0`, x86_64 0x28274d
    // `test ecx,ecx; jle 0x282800`). The comparison is inlined at this call site, which is why
    // the standalone FixedTimeEquals body appears twice here in the frozen exports; the product
    // keeps the helper call, whose semantics are restored below.
    //
    // Decrypt: a second class-init check, then AesTls() -> set_Key(aesKey) (vtable 0x1f8) ->
    // set_IV(iv) (0x1d8) -> CreateDecryptor() (0x298) -> TransformFinalBlock through the
    // ICryptoTransform slot (metadata usage 1817) -> Dispose through the IDisposable slot
    // (metadata usage 1828) -> return. The transform really is protected by a finally: the
    // arm64 LSDA (0x12eae04) covers [0x273394,0x2733d0) with landing pad 0x2734f0 action=5 and
    // the x86_64 LSDA (0x12e5e2c) covers [0x2828af,0x2828e9) with landing pad 0x2829ea
    // action=5, i.e. a cleanup handler, not only the straight-line Dispose. A failed MAC still
    // throws - no payload is fabricated.
    public static byte[] DecryptBody(byte[] body, byte[] aesKey, byte[] macKey)
    {
        if (body.Length < MinimumEncryptedBodyLength)
        {
            throw new CryptographicException("帧过短");
        }

        byte[] iv = new byte[IvLength];
        Buffer.BlockCopy(body, 0, iv, 0, IvLength);
        byte[] ciphertext = new byte[body.Length - 0x30];
        Buffer.BlockCopy(body, IvLength, ciphertext, 0, ciphertext.Length);
        byte[] receivedTag = new byte[MacLength];
        Buffer.BlockCopy(body, body.Length - MacLength, receivedTag, 0, MacLength);

        byte[] expectedTag = Hmac(macKey, iv, ciphertext);
        if (!FixedTimeEquals(receivedTag, expectedTag))
        {
            throw new CryptographicException("MAC 校验失败(篡改或密钥不符)");
        }

        Aes aes = AesTls();
        aes.Key = aesKey;
        aes.IV = iv;
        using (ICryptoTransform decryptor = aes.CreateDecryptor())
        {
            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }
    }

    // Official 060008cf Hmac(byte[] macKey, byte[] iv, byte[] ct). arm64 0x2796c8 / x86_64
    // 0x2816c8. HMACSHA256 is constructed from macKey, iv is streamed with TransformBlock and
    // ct with TransformFinalBlock, so the covered region is iv followed by the whole
    // ciphertext. Each argument is null-checked immediately before its own streaming call, so
    // a null iv or ct raises NullReferenceException there. The official body reads Hash once
    // (arm64 line 49, vtable slot 0x1e8) and returns that value with no explicit Clone; the
    // product's extra (byte[])Clone() had no official counterpart and is removed. Whether the
    // SDK getter itself copies is an SDK-internal boundary question, not official behaviour.
    // The HMACSHA256 is likewise disposed from a finally, not only on the normal path: the
    // arm64 LSDA (0x12eb184) protects the three using-body calls - TransformBlock
    // [0x279750,0x27976c), TransformFinalBlock [0x27977c,0x27978c) and the slot-0x1e8 Hash
    // read [0x27979c,0x2797a0) - and all three landing pads funnel to 0x279868, which catches,
    // runs the same IDisposable.Dispose block at 0x2797ac and re-raises at 0x279834; the
    // x86_64 mirror is LSDA 0x12e61b0, landing pad 0x288869, Dispose block 0x2887d6, raise
    // 0x28884b. See Build/NetCryptoDisposeFinally-20260927/evidence-notes.md.
    private static byte[] Hmac(byte[] macKey, byte[] iv, byte[] ct)
    {
        using (HMACSHA256 hmac = new HMACSHA256(macKey))
        {
            hmac.TransformBlock(iv, 0, iv.Length, null, 0);
            hmac.TransformFinalBlock(ct, 0, ct.Length);
            return hmac.Hash;
        }
    }

    // Official 060008d0 FixedTimeEquals(byte[] a, byte[] b). arm64 0x2798b0 (680B) /
    // x86_64 0x2888c0 (240B), frozen Ghidra export .../packet-NetCrypto/060008d0.c.
    // Official semantics, both architectures agreeing:
    //   * a null argument raises NullReferenceException instead of returning false -
    //     x86_64 `if ((param_1 == 0) || (param_2 == 0)) FUN_0016c1b0();` and arm64
    //     0x2798bc `cbz x0 -> 0x279b54` / 0x2798c0 `cbz x1 -> 0x279b54`, where 0x279b54 is
    //     `bl 0x177a74` (the noreturn NRE helper). The product's
    //     "left == null || right == null -> return false" had no official basis and is removed;
    //     the exception is now carried by the length read.
    //   * unequal lengths return false before any byte is read - x86_64
    //     `uVar1 = *(uint*)(param_1+0x18); if (uVar1 != *(uint*)(param_2+0x18)) return 0;`,
    //     arm64 0x2798c4 `ldr x10,[x0,#0x18]; ldr w8,[x1,#0x18]; cmp x10,w8; b.ne 0x2798f4`
    //     where 0x2798f4 is `mov w0,#0`.
    //   * a zero length returns true - x86_64 `if ((int)uVar1 < 1) return 1;`, arm64
    //     0x2798d4 `cmp w10,#1; b.lt -> 0x279904` (mov w0,#1).
    //   * otherwise every byte is OR-ed into one accumulator and the result is
    //     `accumulator == 0`; there is no early exit on the first difference in either
    //     architecture. The official build walks the buffer in 8-byte SIMD steps
    //     (pmovzxbd/por on x86_64, ldr q + orr on arm64) and finishes the tail byte by byte.
    //     That is a vectorisation of the same accumulation over the same byte range, so the
    //     boolean result is identical for every input; the product keeps the per-byte form and
    //     states the equivalence here instead of silently assuming it.
    // No LSDA entry exists in either architecture, so the official body has no try/catch/finally.
    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int difference = 0;
        for (int index = 0; index < left.Length; index++)
        {
            difference |= left[index] ^ right[index];
        }

        return difference == 0;
    }

    // Official 060008d1 WriteLen(byte[] buf, int len). arm64 0x279478 (88B) / x86_64 0x281478.
    // The whole body is one null check and four little-endian byte stores with IL2CPP bound
    // tests: a null buf raises NullReferenceException and a buf shorter than four bytes raises
    // IndexOutOfRangeException from the bound test, never ArgumentException. The product's
    // "buffer == null || buffer.Length < 4" guard had no official basis and is removed.
    private static void WriteLen(byte[] buf, int len)
    {
        buf[0] = (byte)len;
        buf[1] = (byte)(len >> 8);
        buf[2] = (byte)(len >> 16);
        buf[3] = (byte)(len >> 24);
    }
}
}

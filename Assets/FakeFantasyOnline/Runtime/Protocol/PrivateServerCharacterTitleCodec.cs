using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Exact NetClient.SetTitle writer and 0x804c CharData title replacement reader.
/// </summary>
internal static class PrivateServerCharacterTitleCodec
{
    public static bool TryEncode(string title, out byte[] payload)
    {
        payload = null;
        try
        {
            WireWriter writer = new WireWriter();
            writer.WriteString(title ?? string.Empty);
            payload = writer.ToArray();
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is InvalidOperationException ||
            exception is OverflowException)
        {
            payload = null;
            return false;
        }
    }

    /// <summary>
    /// Runs the exact 0x804c read sequence and writes every value into the official static
    /// owner while the reads happen.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch case 0x804c (jump-table entry 0x804c -&gt; arm64
    /// 0x254534 / x86_64 0x26387a), read from the one NetClient.Buf (arm64 x24 / x86_64 r13)
    /// in this order:
    /// 1 Buf.Str (arm64 0x254544 bl 0x26b088 / x86_64 0x263888 callq 0x27a6e0) -&gt; the current
    ///   title, kept in a register (arm64 x20 / x86_64 r14) until the end of the case;
    /// 2 Buf.U8 (arm64 0x254554 bl 0x26afcc / x86_64 0x263895 callq 0x27a630) -&gt; the owned
    ///   entry count (arm64 w21 / x86_64 r15d);
    /// 3 CharData class-init (usage 1441 TypeInfo:CharData, arm64 0x148a5a8 / x86_64 0x147ea20),
    ///   static_fields at class +0xb8, the List&lt;string&gt; at offset 0x38 - CharData.ownedTitles,
    ///   field token 67114202, atlas offset 56 - is null-checked (arm64 0x254580 cbz -&gt; 0x25c960;
    ///   x86_64 0x2638c8 je 0x26b60d) and then emptied by one zero-argument instance call on it
    ///   (MethodRef[44370], arm64 0x254590 bl 0x1418 / x86_64 0x2638d8 callq 0x13d0). That call
    ///   precedes the count test, so the list is emptied even when the count is zero;
    /// 4 the count is tested only afterwards (arm64 0x254594 cmp w21,#0x1 / b.lt;
    ///   x86_64 0x2638dd testl %r15d,%r15d / jle) and then exactly count iterations run, each
    ///   re-reading the very same static list (arm64 0x2545dc / x86_64 0x26392b) after another
    ///   CharData class-init, reading one Buf.Str (arm64 0x2545e8 / x86_64 0x263938) BEFORE the
    ///   null test of that list (arm64 0x2545ec cbz -&gt; 0x25c960; x86_64 0x26393d je 0x26b60d)
    ///   and appending it with the inlined List&lt;string&gt;.Add (version at +0x1c, items at
    ///   +0x10, size at +0x18, elements at items +0x20; the capacity branch at arm64 0x254618
    ///   / x86_64 0x263961 calls the shared List`1.Add through MethodRef[44368]);
    /// 5 only after the loop - and also when the count was zero - is the string from step 1
    ///   stored into the CharData static string field at offset 0x30 (CharData.title, field
    ///   token 67114201, atlas offset 48) followed by the write barrier (arm64 0x254650
    ///   str x20,[x0,#0x30]! then b 0x17777c; x86_64 0x2639a0 movq %r14,0x30(%rdi) then
    ///   addq $0x30,%rdi and the same barrier).
    /// The case has no Remaining gate, no trailing-byte check, no empty or null string filter,
    /// no duplicate removal and no test that the current title is one of the owned entries;
    /// an extra tail is ignored.
    /// Because step 3 and step 4 mutate the owner before the packet is fully read, a failure in
    /// the middle of the packet is not rolled back: the cleared list and the entries appended
    /// so far stay in the owner, and the exception the primitive read raised keeps propagating.
    /// This method therefore has no Try shape and no whole-packet catch - it hands each read
    /// straight to the owner and lets the original primitive error out.
    /// </remarks>
    public static WorldCharacterTitleState Decode(
        byte[] payload,
        IWorldReceiveOwner owner)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        string current = reader.Str();
        int count = reader.U8();
        if (owner != null)
        {
            owner.ClearOwnedTitles();
        }

        List<string> owned = new List<string>(count);
        for (int index = 0; index < count; index++)
        {
            string entry = reader.Str();
            if (owner != null)
            {
                owner.AddOwnedTitle(entry);
            }

            owned.Add(entry);
        }

        if (owner != null)
        {
            owner.SetCurrentTitle(current);
        }

        // Native 0x804c consumes this root and leaves later bytes unread.
        return new WorldCharacterTitleState(current, owned);
    }
}

}

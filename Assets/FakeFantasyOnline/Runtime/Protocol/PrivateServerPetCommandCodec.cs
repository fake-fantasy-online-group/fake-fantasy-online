using System;
using System.Globalization;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Strict source translation of NetClient.PetCmd and its subcommand wrappers.
/// </summary>
internal static class PrivateServerPetCommandCodec
{
    private enum Subcommand : byte
    {
        AddPoint = 2,
        Rename = 3,
        SetSkill = 4,
        Switch = 5,
        UseSkill = 6,
        Recall = 7,
        Deploy = 8,
        Hatch = 9,
        Show = 10,
        Awaken = 11,
        Swap = 12
    }

    // Byte-level copy of the payload NetClient.PetAddPoint 0x06000734 produces (subcommand 2, the
    // official attribute code, empty text). Since WBD-119 the runtime path is the recovered
    // official member itself (Protocol/NetClient.PetCommands.cs), whose writer runs inside
    // NetClient.Send; this encoder stays for the byte comparison only and has no runtime caller.
    // The six codes it maps from WorldPetAttributeKind are the frozen table values, not a product
    // invention - but the mapping itself is no longer a step on the wire path, because the UI now
    // carries the code the table row holds.
    public static bool TryEncodeAddPoint(
        WorldPetAttributeKind attribute,
        out byte[] payload)
    {
        int code;
        switch (attribute)
        {
            case WorldPetAttributeKind.Strength:
                code = 1;
                break;
            case WorldPetAttributeKind.Vitality:
                code = 5;
                break;
            case WorldPetAttributeKind.Agility:
                code = 7;
                break;
            case WorldPetAttributeKind.Intelligence:
                code = 3;
                break;
            case WorldPetAttributeKind.Dexterity:
                code = 9;
                break;
            case WorldPetAttributeKind.Spirit:
                code = 61;
                break;
            default:
                payload = null;
                return false;
        }
        return TryEncode(Subcommand.AddPoint, code, string.Empty, out payload);
    }

    // Byte-level copy of the payload NetClient.PetRename 0x06000735 produces (subcommand 3, the
    // Int32 argument zero, the name as written). Since WBD-119 the runtime path is the recovered
    // official member itself, so this encoder has no runtime caller and stays for the byte
    // comparison only; its Trim and its length test are not native branches - the frozen body
    // forwards the string unchanged and Win05.<DrawPet>g__SubmitName|87_1 0x06001060 is where the
    // official trim happens.
    public static bool TryEncodeRename(string name, out byte[] payload)
    {
        string trimmed = name == null ? string.Empty : name.Trim();
        if (trimmed.Length == 0 || trimmed.Length > 10)
        {
            payload = null;
            return false;
        }
        return TryEncode(Subcommand.Rename, 0, trimmed, out payload);
    }

    // Byte-level copy of the payload NetClient.PetSetSkill 0x06000736 produces (subcommand 4, the
    // Int32 the caller handed in, empty text). Since WBD-120 the runtime path is the recovered
    // official member itself (Protocol/NetClient.PetCommands.cs), whose writer runs inside
    // NetClient.Send, so this encoder has no runtime caller and stays for the byte comparison
    // only; the "skillId < 0" test it applies is not a native branch - the frozen body forwards
    // the Int32 unchanged and the only caller passes the literal 0.
    public static bool TryEncodeSetSkill(int skillId, out byte[] payload)
    {
        return TryEncode(Subcommand.SetSkill, skillId, string.Empty, out payload);
    }

    // Byte-level copy of the payload NetClient.PetUseSkill 0x06000737 produces (subcommand 6, the
    // signed Int32 the caller handed in, empty text). Since WBD-124 the runtime path is the
    // recovered official member itself (Protocol/NetClient.PetCommands.cs), whose writer runs
    // inside NetClient.Send after its connected test, so this encoder has no runtime caller and
    // stays for the byte comparison only - Editor/ReferenceFirstWorldHotbarAudit.cs:335 reaches
    // it through PrivateServerHotbarCodec.TryEncodePetSkill. The try/catch below is not a native
    // branch either: the frozen body forwards the Int32 unchanged and cannot fail.
    public static bool TryEncodeUseSkill(int skillId, out byte[] payload) =>
        TryEncode(Subcommand.UseSkill, skillId, string.Empty, out payload);

    public static bool TryEncodeSwitch(int slot, out byte[] payload)
    {
        if (!ValidSlot(slot))
        {
            payload = null;
            return false;
        }
        return TryEncode(Subcommand.Switch, slot, string.Empty, out payload);
    }

    // Byte-level copy of the payload NetClient.PetRecall 0x06000738 produces. Since WBD-079 the
    // runtime path is the recovered official member itself (Protocol/NetClient.PetCommands.cs),
    // whose writer runs inside NetClient.Send; this encoder stays for the byte comparison only.
    public static bool TryEncodeRecall(int slot, out byte[] payload)
    {
        if (!ValidSlot(slot))
        {
            payload = null;
            return false;
        }
        return TryEncode(Subcommand.Recall, slot, string.Empty, out payload);
    }

    // Byte-level copy of the payload NetClient.PetDeploy 0x06000739 produces (subcommand 8, the
    // bag slot, empty text). Since WBD-083 the runtime path is the recovered official member
    // itself (Protocol/NetClient.PetCommands.cs), whose writer runs inside NetClient.Send; this
    // encoder stays for the byte comparison only and has no runtime caller. The ValidSlot 0..9
    // test it applies is not a native branch.
    public static bool TryEncodeDeploy(int slot, out byte[] payload)
    {
        if (!ValidSlot(slot))
        {
            payload = null;
            return false;
        }
        return TryEncode(Subcommand.Deploy, slot, string.Empty, out payload);
    }

    // Same for NetClient.PetHatch 0x0600073a (subcommand 9): byte comparison only, no runtime
    // caller since WBD-083.
    public static bool TryEncodeHatch(int slot, out byte[] payload)
    {
        if (!ValidSlot(slot))
        {
            payload = null;
            return false;
        }
        return TryEncode(Subcommand.Hatch, slot, string.Empty, out payload);
    }

    // Same for NetClient.PetShow 0x0600073b (subcommand 10): the runtime path is the recovered
    // official member since WBD-121, so this encoder keeps the byte shape for comparison only and
    // has no runtime caller.
    public static bool TryEncodeShow(bool show, out byte[] payload)
    {
        return TryEncode(Subcommand.Show, show ? 1 : 0, string.Empty, out payload);
    }

    public static bool TryEncodeAwaken(out byte[] payload)
    {
        return TryEncode(Subcommand.Awaken, 0, string.Empty, out payload);
    }

    // Byte-level copy of the payload NetClient.PetSwap 0x0600073d produces, kept for the same
    // reason as TryEncodeRecall: the runtime path is the official member, and the decimal text it
    // writes there comes from the culture-sensitive Int32.ToString() rather than from the
    // invariant text this copy uses (identical for every slot index the product sends).
    public static bool TryEncodeSwap(int fromSlot, int toSlot, out byte[] payload)
    {
        if (!ValidSlot(fromSlot) || !ValidSlot(toSlot) || fromSlot == toSlot)
        {
            payload = null;
            return false;
        }
        return TryEncode(
            Subcommand.Swap,
            fromSlot,
            toSlot.ToString(CultureInfo.InvariantCulture),
            out payload);
    }

    private static bool ValidSlot(int slot)
    {
        return slot >= 0 && slot < 10;
    }

    private static bool TryEncode(
        Subcommand subcommand,
        int argument,
        string text,
        out byte[] payload)
    {
        try
        {
            WireWriter writer = new WireWriter();
            writer.WriteByte((byte)subcommand);
            writer.WriteInt32(argument);
            writer.WriteString(text ?? string.Empty);
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
}

}

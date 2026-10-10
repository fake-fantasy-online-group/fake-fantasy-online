namespace FakeFantasyOnline.Protocol
{

// Official 1.5.8 NetClient.PetCmd 0x06000733 and the four wrappers pinned to it (WBD-079 for
// PetRecall/PetSwap, WBD-083 for PetDeploy/PetHatch), restored
// where the official NetClient.Send (0x06000772) actually lives:
//
//   PetCmd    0x06000733  instance void PetCmd(uint8 sub, int32 arg, string text)
//                         display class NetClient+<>c__DisplayClass201_0 (+0x10 sub, +0x14 arg,
//                         +0x18 text stored with a GC write barrier) -> Action<Buf>
//                         <PetCmd>b__0 0x06000829 -> Send 0x100b
//   PetAddPoint 0x06000734  PetCmd(this, 2, attributeCode, string.Empty)
//   PetRename   0x06000735  PetCmd(this, 3, 0, text)
//   PetSetSkill 0x06000736  PetCmd(this, 4, skillId, string.Empty)
//   PetUseSkill 0x06000737  PetCmd(this, 6, skillId, string.Empty)
//   PetRecall 0x06000738  PetCmd(this, 7, slot, string.Empty)
//   PetDeploy 0x06000739  PetCmd(this, 8, storeIdx, string.Empty)
//   PetHatch  0x0600073a  PetCmd(this, 9, slot, string.Empty)
//   PetShow   0x0600073b  PetCmd(this, 10, show ? 1 : 0, string.Empty)
//   PetSwap   0x0600073d  PetCmd(this, 12, from, to.ToString())
//
// The bodies are the frozen IL of Assembly-CSharp at
// Build/ReferenceManagedRecovery-1.5.8/il/Assembly-CSharp.il (PetCmd at line 1104128, PetRecall at
// 1104332, PetSwap at 1104479, all `.method public hidebysig`) plus the capstone windows in
// Build/ReferenceReverseGhidra-1.5.8/queries/WBD-079-petcmd: arm64 0x26e6c0 loads [0x148D5D8]
// (metadata usage 2983, TypeInfo NetClient+<>c__DisplayClass201_0), runs its argument-less ctor,
// stores w22 at +0x10, w21 at +0x14 and x20 at +0x18 through the write barrier thunk 0x17777c,
// allocates the Action<Buf> from [0x1488468], binds it with the method pointer at [0x149A510]
// (metadata usage 9614, MethodDef NetClient+<>c__DisplayClass201_0.<PetCmd>b__0) and tail calls
// Send at 0x2658c0 with esi/r1 = 0x100b; x86_64 0x27dc40 is the same sequence through 0x1481A50,
// 0x147C8E0, 0x148E988 and 0x274CB0.
//
// Serialization timing is the point of putting them here, and the reason PetRecall/PetSwap are not
// a payload encoded by PrivateServerPetCommandCodec: NetClient.Send builds the Buf and runs the
// writer only after its connected test (_tcp != null && _tcp.Connected), so while the socket is
// down nothing is serialized at all. Encoding before that test moves the write - and with it the
// strict-UTF-8 and null/length guards of WireWriter.WriteString - ahead of the official gate.
//
// What the writer writes is fixed by <PetCmd>b__0 in both architectures (arm64 0x276734 /
// x86_64 0x285950, 72 and 64 bytes): one null check on the Buf, then Buf.U8 (0x0600078a) of sub,
// Buf.I32 (0x0600078c) of arg and Buf.Str (0x0600078e) of text, in that order and nothing else.
public sealed partial class NetClient
{
    public void PetCmd(byte sub, int arg, string text)
    {
        Send(ProtocolOpcodes.PetCommandRequest, buffer => WritePetCmd(buffer, sub, arg, text));
    }

    // Official 0x06000734, instance System.Void NetClient.PetAddPoint(System.Int32).
    //
    // arm64 rva 0x26e7b4 (100 bytes) is one tail call behind the metadata-usage initialization
    // guard: it moves the argument to x19, runs the empty-string metadata slot at arm64
    // [0x149bb98] / x86_64 [rip + 0x12122ab] (= 0x1490010, usage 10335, kind 5) into x3/rcx, sets the
    // subcommand to 2 with `mov w1, #2` at 0x26e800 / `mov esi, 2` at 0x27dd68, restores the
    // frame and branches to PetCmd at 0x26e6c0 / 0x27dc40. There is no range test, no ToString
    // and no trim: the Int32 the caller hands in is the attribute code itself, exactly like the
    // slot is for PetRecall 0x06000738.
    //
    // The caller that proves the identity is Win05.DrawPet 0x06001043: inside its six-button
    // "add a point" loop arm64 0x3b9e48 and x86_64 0x3cd348 read NetClient.I from
    // [0x148b968] + 0xb8, feed the non-returning helper at arm64 0x3bcb28 / x86_64 0x3d07d7 when
    // it is null, and call PetAddPoint(that instance, <the per-button code>). Those codes are the first Int32 field of
    // the six table rows built at arm64 0x3b9dc0-0x3b9e10: 5, 3, 1, 7, 9 and 0x3d (61), against
    // the row rects (106,294), (223,294), (106,313), (223,313), (106,332), (223,332). So a point
    // is always one point of one attribute - there is no "amount" argument in the wire - and the
    // free-point budget is the server's, not the client's: nothing here or in the caller
    // decrements a local counter.
    public void PetAddPoint(int attributeCode)
    {
        PetCmd(2, attributeCode, string.Empty);
    }

    // Official 0x06000735, instance System.Void NetClient.PetRename(System.String).
    //
    // The smallest of the wrappers: arm64 rva 0x26e818 is 16 bytes and x86_64 rva 0x27dd80 is 32,
    // both without any usage guard, because the body only shuffles arguments and tail calls PetCmd
    // - `mov x3, x1` / `mov rcx, rsi` (the caller's text becomes PetCmd's text), `mov w1, #3` /
    // `mov esi, 3` (the rename subcommand), `mov w2, #0` / `xor edx, edx` (the Int32 argument is
    // literally zero), then `b 0x26e6c0` / `jmp 0x27dc40`.
    //
    // Nothing validates, trims or shortens the string here: the only caller,
    // Win05.<DrawPet>g__SubmitName|87_1 0x06001060, trims Win05.petRenameText once before calling
    // (arm64 0x3c0290 / x86_64 0x3d431e String.Trim with a null trimChars) and only then reaches
    // PetRename at arm64 0x3c0378 / x86_64 0x3d43b2. PrivateServerPetCommandCodec.TryEncodeRename
    // keeps applying a second Trim and a length test for byte comparison only; neither exists in
    // the frozen body, so neither is duplicated here.
    public void PetRename(string name)
    {
        PetCmd(3, 0, name);
    }

    // Official 0x06000736, instance System.Void NetClient.PetSetSkill(System.Int32).
    //
    // Both frozen bodies are one tail call behind the same metadata-usage initialization guard the
    // other wrappers use - arm64 rva 0x26e828 (100 bytes) and x86_64 rva 0x27dda0 (80 bytes) both
    // run FUN_001777e0 / FUN_0016be90 on the empty-string slot (usage 10335, kind 5) and then call
    // PetCmd 0x06000733 with the subcommand 4 and the caller's Int32 unchanged
    // (Build/ReferenceReverseGhidra-1.5.8/queries/WBD-120-06000736-{arm64,x86_64}/06000736.c).
    // The frozen IL agrees: il/Assembly-CSharp.il:1104272 is `ldarg.0; ldc.i4 4; ldarg skillId;
    // ldloc 1; call NetClient::PetCmd(uint8, int32, string)`, and managed-isil names the parameter
    // `skillId` (`Assembly-CSharp.dll::NetClient.System.Void PetSetSkill(System.Int32 skillId)`).
    //
    // What the only caller actually passes is fixed by Win05.DrawPet 0x06001043, not by the name:
    // in the skill row's right-click arm both architectures call
    // `PetSetSkill(NetClient.I, 0, 0)` (arm64 0x3bb6fc / x86_64 0x3cf1d6, the single direct call
    // edge in each slice), i.e. the Int32 is the literal 0 and the branch is the one taken when
    // CharData.petActiveSkill already equals the row's skill id. Selecting a different skill is
    // PetUseSkill 0x06000737 with that id; subcommand 4 is the unselect. Nothing here validates,
    // clamps or converts the Int32 - the server decides what the id means.
    public void PetSetSkill(int skillId)
    {
        PetCmd(4, skillId, string.Empty);
    }

    // Official 0x06000737, instance System.Void NetClient.PetUseSkill(System.Int32).
    //
    // Same one-tail-call shape as its neighbours: arm64 rva 0x26e88c (100 bytes) and x86_64 rva
    // 0x27ddf0 (80 bytes) run the metadata-usage initialization guard on the empty-string slot
    // (usage 10335, kind 5, arm64 [0x149bb98]) and then call PetCmd 0x06000733 with the
    // subcommand 6 and the caller's Int32 unchanged
    // (Build/ReferenceReverseGhidra-1.5.8/queries/WBD-124-06000737-{arm64,x86_64}/06000737.c).
    // The frozen IL agrees: il/Assembly-CSharp.il:1104302-1104329 is `ldarg.0; ldc.i4 6;
    // ldarg skillId; ldloc 1; call NetClient::PetCmd(uint8, int32, string)`, and managed-isil
    // names the parameter `skillId`.
    //
    // What the two real callers pass is a skill id, not a pet-bar slot and not a list index:
    // GameHud.UseHotSlot 0x0600043c sends the hot-bar entry itself (one direct call edge per
    // slice, Build/ReferenceReverseGhidra-1.5.8/queries/WBD-124-0600043c-{arm64,x86_64}/
    // 0600043c.c:377) and Win05.DrawPet 0x06001043 sends either the literal 13002 or the row's id
    // (two direct call edges per slice, .../WBD-070-06001043-arm64/06001043.c:1970 and :2004,
    // .../WBD-070-06001043-x86_64/06001043.c:2160 and :2191). Nothing here validates, clamps or
    // converts the Int32 - the server adjudicates the id, as it does for PetSetSkill 0x06000736.
    public void PetUseSkill(int skillId)
    {
        PetCmd(6, skillId, string.Empty);
    }

    // Official 0x06000738. The frozen IL passes ldc.i4 7, the slot argument and the empty-string
    // metadata slot (usage 10335 at arm64 0x149BB98, the same slot PetAddPoint and PetDeploy use)
    // to PetCmd; arm64 0x26e93c / x86_64 0x27de78 set the subcommand to 7 and branch straight into
    // PetCmd.
    public void PetRecall(int slot)
    {
        PetCmd(7, slot, string.Empty);
    }

    // Official 0x06000739, instance System.Void NetClient.PetDeploy(System.Int32 storeIdx).
    //
    // Both frozen bodies are one tail call - arm64 rva 0x26e954 (100 bytes) sets w1 = 8 at
    // 0x26e9a0 and x86_64 rva 0x27de90 (80 bytes) sets esi = 8 at 0x27dec8 - so the deploy
    // subcommand is 8 and, exactly like PetRecall, the text argument is the empty-string
    // metadata slot (usage 10335, kind 5, arm64 [0x149bb98]). The frozen exports
    // (Build/ReferenceReverseGhidra-1.5.8/queries/{arm64/2292bf0f...,x86_64/eba42edd...}/
    // packet-NetClient/06000739.c) render it as
    // `PetCmd(param_1, 8, param_2, metadata_usage_10335_kind5)` behind the single metadata-usage
    // initialization guard. There is no branch, no range test and no ToString in the body.
    public void PetDeploy(int storeIdx)
    {
        PetCmd(8, storeIdx, string.Empty);
    }

    // Official 0x0600073a, instance System.Void NetClient.PetHatch(System.Int32 slot).
    //
    // Same shape one token later: arm64 rva 0x26e9b8 (100 bytes) sets w1 = 9 at 0x26ea04,
    // x86_64 rva 0x27dee0 (80 bytes) sets esi = 9 at 0x27df18, and both tail call PetCmd with
    // the same empty-string slot, rendered in the frozen exports as
    // `PetCmd(param_1, 9, param_2, metadata_usage_10335_kind5)`. Subcommand 9 is the hatch the
    // pet-egg row of InventoryDlg.OnGUI asks for; nothing is validated or converted here.
    public void PetHatch(int slot)
    {
        PetCmd(9, slot, string.Empty);
    }

    // Official 0x0600073b, instance System.Void NetClient.PetShow(System.Boolean show).
    //
    // Same one-tail-call shape one token earlier: arm64 rva 0x26ea1c (108 bytes) and x86_64 rva
    // 0x27df30 (96 bytes) both run the usage-10335 empty-string initialization guard and then
    // call PetCmd 0x06000733 with subcommand 10 and the caller's Boolean in the Int32 slot
    // (Build/ReferenceReverseGhidra-1.5.8/queries/WBD-121-0600073b-{arm64,x86_64}/0600073b.c).
    // The frozen IL is `ldarg.0; ldc.i4 10; ldarg show; ldloc 2; call NetClient::PetCmd(uint8,
    // int32, string)` (il/Assembly-CSharp.il:1104421-1104449), i.e. the flag travels as 0 or 1
    // with no conversion in the body. The only branch is the metadata-usage guard; the null
    // instance arm is the non-returning helper FUN_00177a74 / FUN_0016c1b0, reached when `this`
    // is null - the caller in Win05.DrawPet 0x06001043 is what decides whether that can happen.
    public void PetShow(bool show)
    {
        PetCmd(10, show ? 1 : 0, string.Empty);
    }

    // Official 0x0600073d. The frozen IL converts the second argument first -
    // `ldloca to; call instance string System.Int32::ToString()` - and only then calls
    // PetCmd(this, 12, from, <that string>); arm64 0x26eb00-0x26eb10 spills w2 and calls
    // Int32.ToString at 0xeb4c70 with a null format provider, x86_64 0x27dff0-0x27dff9 the same at
    // 0xeb2420. The conversion is the culture-sensitive Int32.ToString() and it runs before the
    // send, not inside the writer.
    public void PetSwap(int from, int to)
    {
        PetCmd(12, from, to.ToString());
    }

    // Official 0x06000829 (NetClient+<>c__DisplayClass201_0.<PetCmd>b__0), the Action<Buf>
    // NetClient.PetCmd hands to Send. Both architectures read the same three display-class fields
    // in the same order and validate nothing: sub is one byte, arg is four little-endian bytes and
    // text is the replacing UTF-8 encoding Buf.Str uses (null is "", the U16 length prefix is
    // truncated while the payload is still copied in full).
    internal static void WritePetCmd(Buf buffer, byte sub, int arg, string text)
    {
        buffer.U8(sub);
        buffer.I32(arg);
        buffer.Str(text);
    }
}
}

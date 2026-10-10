using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class CharData
{
    private static Action<int> petSwitch;

    internal static void BindPetSwitchWriterForIndependentRuntime(Action<int> switchPet) =>
        petSwitch = switchPet;

    // Official 0x06000f21, static System.String CharData.get_petIcon().
    //
    // arm64 rva 0x394968 (228 bytes) and x86_64 rva 0x3a7c70 (240 bytes) read the System.Int32 at
    // +0x150 (petModel); a positive model yields metadata usage 12750 ("M") concatenated with
    // Int32.ToString(), otherwise usage 10335 (the empty string) is returned.
    public static string petIcon => petModel > 0 ? "M" + petModel : string.Empty;

    // Official 0x06000f22, static System.Void CharData.SwitchPet(System.Int32).
    //
    // arm64 rva 0x394a4c (292 bytes) and x86_64 rva 0x3a7d60 (240 bytes) run, after the three cctor
    // guards, exactly four steps in this order:
    //   1. -1 < idx (arm64 0x394a98 tbnz w19, #0x1f -> 0x394b5c / x86_64 0x3a7da0-0x3a7da2
    //      test/js -> 0x3a7e3b): a negative index leaves without a command.
    //   2. the FIRST read of NetClient.I - the NetClient static field at +0x00, ordinal 8 in the
    //      atlas fields table - compared through UnityEngine.Object.op_Inequality
    //      (arm64 0x394ad8 bl 0x104b39c, the arm64 label table pins that rva to
    //      UnityEngine.Object.op_Inequality / x86_64 0x3a7dec call 0x103dde0, the same
    //      two-argument Unity comparison): a false result leaves without a command.
    //   3. a SECOND, separate read of the same static field (arm64 0x394ae0-0x394af8 / x86_64
    //      0x3a7df5-0x3a7e10), which is the receiver of the tail call. Its guard is a raw
    //      reference test - arm64 0x394afc cbz / x86_64 0x3a7e13-0x3a7e16 test/je, no
    //      op_Inequality - and a null there reaches the non-returning null helper
    //      (arm64 0x394b6c bl 0x177a74 / x86_64 0x3a7e46 call 0x16c1b0, the same helper
    //      NetCrypto 0x060008ce reaches for a null body), so that path raises
    //      NullReferenceException instead of returning. The two reads are therefore two statements
    //      here and not one conditional access: no field is written between them.
    //   4. the only call, NetClient.PetCmd(I, 5, idx, string.Empty): subcommand 5 is set at
    //      arm64 0x394b0c / x86_64 0x3a7e22 and the text is the empty-string metadata slot usage
    //      10335 at arm64 [0x149bb98] / x86_64 [rip + 0x10e81f1]. idx (arm64 x2 at 0x394b10) and
    //      the text (arm64 x3 at 0x394b08) are both taken after the second I read.
    // Subcommand 5 is exclusive to this caller among the eleven PetCmd edges and equals
    // PrivateServerPetCommandCodec.Subcommand.Switch, so the bound writer - installed by
    // ReferenceCharacterSkillUsePort over ClientSessionCoordinator.SwitchPetAsync - is the
    // independent-runtime stand-in for that tail call, exactly as NetClient.PetHatch 0x0600073a
    // stands in for subcommand 9. The stand-in replaces the receiver of the call, not the order of
    // the two reads or either gate, so both gates stay in the body they belong to.
    public static void SwitchPet(int idx)
    {
        if (idx < 0) return;
        bool present = NetClient.I != null;
        if (!present) return;
        NetClient receiver = NetClient.I;
        if (ReferenceEquals(receiver, null)) throw new NullReferenceException();
        petSwitch(idx);
    }
}

}

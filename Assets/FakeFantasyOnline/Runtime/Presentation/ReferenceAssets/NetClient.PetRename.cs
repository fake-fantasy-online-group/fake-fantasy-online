using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official 0x06000735, instance System.Void NetClient.PetRename(System.String).
    //
    // arm64 rva 0x26e818 is 16 bytes and x86_64 rva 0x27dd80 is 32 bytes, and neither has a usage
    // guard, because the whole body is argument shuffling and one tail call:
    //   NetClient.PetCmd(this, 3, 0, name)
    // arm64 moves the caller's text into x3, sets w1 = 3 and w2 = 0; x86_64 moves it into rcx and
    // sets esi = 3 with edx zeroed. So the rename subcommand is 3, the Int32 argument is literally
    // zero, and the string is forwarded without being trimmed, shortened or defaulted - the trim
    // the product used to apply twice belongs to the only caller, Win05.<DrawPet>g__SubmitName|87_1
    // 0x06001060, which trims once and skips empty results before reaching this member.
    //
    // PetCmd is the recovered Protocol member since WBD-079, so the bytes appear inside
    // NetClient.Send 0x06000772 after its connected test and not before it. This facet writes no
    // wire either: the bound writer installed by ReferenceCharacterSkillUsePort over
    // ClientSessionCoordinator.RenamePetAsync reaches PrivateServerGateway.RenamePetAsync, which
    // invokes the official member inside the single send gate.
    private Action<string> writePetRename;

    internal void BindPetRenameWriterForIndependentRuntime(Action<string> writer) =>
        writePetRename = writer;

    public void PetRenameForIndependentRuntime(string name) => writePetRename(name);
}
}

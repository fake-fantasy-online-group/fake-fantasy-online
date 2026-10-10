using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official 0x06000734, instance System.Void NetClient.PetAddPoint(System.Int32).
    //
    // arm64 rva 0x26e7b4 (100 bytes) and x86_64 rva 0x27dd30 (80 bytes) are both one tail call
    // after the metadata-usage guard:
    //   NetClient.PetCmd(this, 2, attributeCode, "")
    // arm64 0x26e800 sets w1 = 2 and x86_64 0x27dd68 sets esi = 2, both with the empty-string
    // metadata slot (usage 10335, arm64 [0x149bb98] / x86_64 [rip + 0x12122ab]) as the text
    // argument, before branching to PetCmd 0x06000733 at arm64 0x26e6c0 / x86_64 0x27dc40. The
    // Int32 travels unchanged: it is the official attribute code (5 VIT, 3 INT, 1 STR, 7 AGI,
    // 9 DEX, 61 SPI), not a slot index and not an amount, so nothing here validates it and no
    // local free-point counter is touched - the budget is the server's.
    //
    // PetCmd itself is a recovered Protocol member since WBD-079
    // (Assets/FakeFantasyOnline/Runtime/Protocol/NetClient.PetCommands.cs): the writer
    // <PetCmd>b__0 0x06000829 runs inside NetClient.Send 0x06000772 after its connected test, so
    // nothing is serialized while the socket is down. This facet writes no wire: the bound writer
    // installed by ReferenceCharacterSkillUsePort over
    // ClientSessionCoordinator.AllocatePetPointAsync reaches PrivateServerGateway
    // .AllocatePetPointAsync, which invokes the official member inside the single send gate. Same
    // shape NetClient.PetRecall 0x06000738 already established.
    private Action<int> writePetAddPoint;

    internal void BindPetAddPointWriterForIndependentRuntime(Action<int> writer) =>
        writePetAddPoint = writer;

    public void PetAddPointForIndependentRuntime(int attributeCode) => writePetAddPoint(attributeCode);
}
}

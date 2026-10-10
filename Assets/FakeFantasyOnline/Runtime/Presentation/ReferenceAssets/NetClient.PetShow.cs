using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official 0x0600073b, instance System.Void NetClient.PetShow(System.Boolean show).
    //
    // arm64 rva 0x26ea1c (108 bytes) and x86_64 rva 0x27df30 (96 bytes) are one tail call behind
    // the metadata-usage guard:
    //   NetClient.PetCmd(this, 10, show ? 1 : 0, "")
    // (Build/ReferenceReverseGhidra-1.5.8/queries/WBD-121-0600073b-{arm64,x86_64}/0600073b.c).
    // The Boolean is the whole payload: it travels in the Int32 slot as 0 or 1 with no conversion
    // in the frozen body and no toggle, negation or local state - the display flag belongs to the
    // server, and Win05.DrawPet 0x06001043 is the only caller.
    //
    // PetCmd itself is a recovered Protocol member since WBD-079
    // (Assets/FakeFantasyOnline/Runtime/Protocol/NetClient.PetCommands.cs): the writer
    // <PetCmd>b__0 0x06000829 runs inside NetClient.Send 0x06000772 after its connected test, so
    // nothing is serialized while the socket is down. This facet writes no wire: the bound writer
    // installed by ReferenceCharacterSkillUsePort over
    // ClientSessionCoordinator.SetPetShownAsync reaches PrivateServerGateway.SetPetShownAsync,
    // which invokes the official member inside the single send gate.
    private Action<bool> writePetShow;

    internal void BindPetShowWriterForIndependentRuntime(Action<bool> writer) =>
        writePetShow = writer;

    public void PetShowForIndependentRuntime(bool show) => writePetShow(show);
}
}

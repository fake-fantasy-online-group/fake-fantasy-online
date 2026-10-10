using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official 0x06000738, instance System.Void NetClient.PetRecall(System.Int32).
    //
    // arm64 rva 0x26e8f0 (100 bytes) and x86_64 rva 0x27de40 (80 bytes) are both a single tail
    // call after the NetClient cctor guard:
    //   NetClient.PetCmd(this, 7, slot, string.Empty)
    // arm64 0x26e93c sets w1 = 7 and 0x26e938 loads the empty-string metadata slot into x3 before
    // branching to PetCmd 0x06000733 at 0x26e6c0; x86_64 0x27de78 sets esi = 7, 0x27de75 loads the
    // same slot into rcx and jmps to 0x27dc40. The frozen IL of Assembly-CSharp says the same
    // (line 1104332: ldc.i4 7, ldarg slot, the empty-string metadata slot, then PetCmd).
    //
    // PetCmd itself is a recovered Protocol member since WBD-079
    // (Assets/FakeFantasyOnline/Runtime/Protocol/NetClient.PetCommands.cs): it captures the triple
    // in NetClient+<>c__DisplayClass201_0, hands the writer <PetCmd>b__0 0x06000829 to
    // NetClient.Send 0x06000772 with opcode 0x100b, and Send runs that writer only after its
    // connected test - so the capture happens here at the click and the bytes are produced behind
    // the official gate, not before it. This facet writes no wire: the bound writer installed by
    // ReferenceCharacterSkillUsePort over ClientSessionCoordinator.RecallPetAsync reaches
    // PrivateServerGateway.RecallPetAsync, which invokes the official member inside the single
    // send gate. Same shape NetClient.PetHatch 0x0600073a already established.
    private Action<int> writePetRecall;

    internal void BindPetRecallWriterForIndependentRuntime(Action<int> writer) => writePetRecall = writer;

    public void PetRecallForIndependentRuntime(int slot) => writePetRecall(slot);
}
}

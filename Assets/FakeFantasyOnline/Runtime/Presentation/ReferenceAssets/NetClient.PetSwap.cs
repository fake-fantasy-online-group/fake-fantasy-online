using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official 0x0600073d, instance System.Void NetClient.PetSwap(System.Int32,System.Int32).
    //
    // arm64 rva 0x26eae8 (100 bytes) and x86_64 rva 0x27dfe0 (80 bytes): the second argument is
    // spilled and turned into its decimal string before the single PetCmd tail call -
    //   NetClient.PetCmd(this, 12, fromSlot, toSlot.ToString())
    // arm64 0x26eb00-0x26eb10 stores w2 and calls Int32.ToString at 0xeb4c70, then 0x26eb18 sets
    // w1 = 12 and 0x26eb20 calls PetCmd 0x06000733 at 0x26e6c0; x86_64 0x27dff0-0x27dff9 does the
    // same through 0xeb2420 and sets esi = 12 at 0x27e001 before calling 0x27dc40. The IL agrees
    // (line 1104479): ldloca to, `call instance string System.Int32::ToString()`, then
    // PetCmd(this, 12, from, <that string>), so the conversion is the culture-sensitive
    // ToString() and it runs before the send, outside the writer.
    //
    // PetCmd is a recovered Protocol member since WBD-079, so the triple (12, from,
    // to.ToString()) is what the official body captures in NetClient+<>c__DisplayClass201_0 and
    // <PetCmd>b__0 0x06000829 writes inside NetClient.Send 0x06000772 after its connected test.
    // This facet writes no wire either: the bound writer installed by
    // ReferenceCharacterSkillUsePort over ClientSessionCoordinator.SwapPetAsync reaches
    // PrivateServerGateway.SwapPetAsync, which invokes that official member inside the single send
    // gate.
    private Action<int, int> writePetSwap;

    internal void BindPetSwapWriterForIndependentRuntime(Action<int, int> writer) => writePetSwap = writer;

    public void PetSwapForIndependentRuntime(int fromSlot, int toSlot) => writePetSwap(fromSlot, toSlot);
}
}

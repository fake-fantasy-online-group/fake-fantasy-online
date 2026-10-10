using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int> petHatch;

    internal void BindPetHatchWriterForIndependentRuntime(Action<int> hatch) => petHatch = hatch;

    // Official 0x0600073a, instance System.Void NetClient.PetHatch(System.Int32).
    //
    // arm64 rva 0x26e9b8 (100 bytes) and x86_64 rva 0x27dee0 (80 bytes) are both a single tail call:
    //   NetClient.PetCmd(this, 9, slot, "")
    // so PetHatch is the pet-egg hatch subcommand: subcommand code 9, the bag slot, and the empty
    // string that PetCmd carries as its free-form argument. PrivateServerPetCommandCodec names the
    // same code Subcommand.Hatch and PrivateServerGateway.HatchPetAsync encodes exactly this triple,
    // which is what the independent runtime binds here.
    public void PetHatchForIndependentRuntime(int slot) => petHatch(slot);
}
}

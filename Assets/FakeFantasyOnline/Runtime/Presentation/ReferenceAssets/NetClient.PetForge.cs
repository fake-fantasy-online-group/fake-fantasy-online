using System;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action writePetAwaken;
    internal void BindPetAwakenWriterForIndependentRuntime(Action writer) => writePetAwaken = writer;
    public void PetAwaken() => writePetAwaken();
    private Action<PetForgeRequest> writePetForge;
    internal void BindPetForgeWriterForIndependentRuntime(Action<PetForgeRequest> writer) => writePetForge = writer;
    public void PetForgeForIndependentRuntime(byte op, byte kind, byte petTab, int petSlot, byte itemTab,
        int itemSlot, int skillId, byte pet2Tab = 0, int pet2Slot = -1) =>
        writePetForge(new PetForgeRequest(op, kind, petTab, petSlot, itemTab, itemSlot, skillId, pet2Tab, pet2Slot));
}
}

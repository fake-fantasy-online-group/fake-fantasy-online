using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceWorldReceiveOwner : IHotbarReceiveOwner
{
    // Dispatch 0x8016 shared set_Item (arm64 24f3c0 -> 6678d0), after this row's
    // kind/id/icon gate. Null native dictionary must throw here, before any later read.
    public void ReceiveHotbarPetSkillIcon(int id, int icon) =>
        CharData.petSkillIcons[id] = icon;

    // Dispatch 0x8016 successful tail (arm64 256f78 -> 44197c). The native body is unchanged.
    public void ReceiveHotbarSlots(int[] ids, int[] kinds, bool expanded, int[] icons) =>
        GameHud.LoadHotbar(ids, kinds, expanded, icons);
}

}

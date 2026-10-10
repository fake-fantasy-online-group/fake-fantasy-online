using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    internal GameObject SpawnNpcEntryForIndependentRuntime(
        NpcEntry entry, Transform parent, float worldW, float worldH, float ppu) =>
        SpawnNpcEntry(entry, parent, worldW, worldH, ppu);
}

}

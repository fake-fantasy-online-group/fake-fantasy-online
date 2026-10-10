namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    // Original SyncHotbar 0600043d.
    private static void SyncHotbar()
    {
        if (NetClient.I != null) NetClient.I.SetHotbarForIndependentRuntime(hotSkill, hotKind, hotbarExpanded);
    }
    internal static void SyncHotbarForIndependentRuntime() => SyncHotbar();
}
}

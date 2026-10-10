namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class SuitFxDB
{
    public static string SoulTFX(int id)
    {
        EnsureLoaded();
        return _soulFx != null && _soulFx.TryGetValue(id, out string value) ? value : string.Empty;
    }
}

}

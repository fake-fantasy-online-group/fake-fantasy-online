namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Presentation only: no task definitions, eligibility, NPCs, rewards or protocol state.
internal static class CubeQuestTitles
{
    internal static string MissingTitle(int id)
    {
        switch (id)
        {
            case 1900030001: return "问鼎周天";
            case 1900030002: return "幻翼初展";
            default: return null;
        }
    }
}
}

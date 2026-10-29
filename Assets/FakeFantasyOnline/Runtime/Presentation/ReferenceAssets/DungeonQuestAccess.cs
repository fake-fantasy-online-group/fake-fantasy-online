namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Same explicit task-only bindings as server domain.DungeonEntryQuest.
// Active includes both unfinished/ready-to-hand-in rows in existing 0x8027;
// 29200/355 is held until its disabled definition/native receipt path is repaired.
// Done is completed history. Ordinary reward dungeons are deliberately absent.
internal static class DungeonQuestAccess
{
    internal static int RequiredQuest(int map)
    {
        switch (map)
        {
            case 29400: return 356;
            case 29900: return 359;
            case 30000: return 361;
            default: return 0;
        }
    }

    internal static bool CanEnter(int map)
    {
        int quest = RequiredQuest(map);
        return quest == 0 || (!TaskClient.Done.Contains(quest) && TaskClient.GetActive(quest) != null);
    }
}
}

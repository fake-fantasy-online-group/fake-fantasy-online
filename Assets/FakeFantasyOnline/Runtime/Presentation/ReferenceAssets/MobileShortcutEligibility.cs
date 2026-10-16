namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static class MobileShortcutEligibility
{
    // Match the original bag drop: pet cards never bind, fabao binds its skill,
    // and ordinary equipment-tab rows cannot become item shortcuts.
    internal static bool Bag(int tab, int id, bool pet, int fabaoSkill) =>
        id > 0 && !pet && (fabaoSkill > 0 || tab != 2);
    internal static bool Pet(int id, int level, bool active) => id > 0 && level > 0 && active;
}
}

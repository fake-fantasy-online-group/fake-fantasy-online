namespace FakeFantasyOnline.Application
{
public sealed partial class WorldPlayerPetSkillState
{
    // Native 800b stores signed values without the public adapter's input validation.
    // This constructor is used only when observing those already-received native rows.
    private WorldPlayerPetSkillState(int id, string displayName, int iconId, string description,
        bool fight, bool active, int level, int maximumLevel, byte received)
    {
        Id = id;
        DisplayName = displayName ?? string.Empty;
        IconId = iconId;
        Description = description ?? string.Empty;
        Fight = fight;
        Active = active;
        Level = level;
        MaximumLevel = maximumLevel;
    }

    public static WorldPlayerPetSkillState FromReceipt(int id, string displayName, int iconId,
        string description, bool fight, bool active, int level, int maximumLevel) =>
        new WorldPlayerPetSkillState(id, displayName, iconId, description,
            fight, active, level, maximumLevel, 0);
}
}

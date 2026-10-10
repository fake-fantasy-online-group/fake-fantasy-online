namespace FakeFantasyOnline.Application
{
// One ordered DuelHud.Apply receive edge, including inactive/phase transitions.
public sealed class WorldDuelHudState
{
    public WorldDuelHudState(bool active, byte phase, byte role, int countdown, int centerX, int centerY,
        int radius, int gap, string aName, string bName, string winnerName, string reason, string lootName,
        int mode, int entryGold)
    {
        Active = active; Phase = phase; Role = role; Countdown = countdown;
        CenterX = centerX; CenterY = centerY; Radius = radius; Gap = gap;
        AName = aName; BName = bName; WinnerName = winnerName; Reason = reason; LootName = lootName;
        Mode = mode; EntryGold = entryGold;
    }
    public bool Active { get; }
    public byte Phase { get; }
    public byte Role { get; }
    public int Countdown { get; }
    public int CenterX { get; }
    public int CenterY { get; }
    public int Radius { get; }
    public int Gap { get; }
    public string AName { get; }
    public string BName { get; }
    public string WinnerName { get; }
    public string Reason { get; }
    public string LootName { get; }
    public int Mode { get; }
    public int EntryGold { get; }
}
}

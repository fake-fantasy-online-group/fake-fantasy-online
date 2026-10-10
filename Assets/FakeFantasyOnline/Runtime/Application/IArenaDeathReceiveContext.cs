namespace FakeFantasyOnline.Application
{
public interface IArenaDeathReceiveContext
{
    void ReceiveArenaDeath(ArenaDeathStateData state);
}

// Transient typed receive value for the official 1.5.8 NetClient.Dispatch case 0x8061. No Unity
// objects and no wire reader cross this boundary; the field order is the official read order of the
// native branch, so nothing here reorders or defaults a value the server did not send.
public sealed class ArenaDeathStateData
{
    public ArenaDeathStateData(int flag, int deathNo, int freeLeft, int kind, long amount,
        string label, bool afford, int waitSec, string ladder)
    {
        Flag = flag;
        DeathNo = deathNo;
        FreeLeft = freeLeft;
        Kind = kind;
        Amount = amount;
        Label = label;
        Afford = afford;
        WaitSec = waitSec;
        Ladder = ladder;
    }

    /// <summary>The leading byte: 0 closes the window, every other value opens it.</summary>
    public int Flag { get; }
    public int DeathNo { get; }
    public int FreeLeft { get; }
    public int Kind { get; }
    public long Amount { get; }
    public string Label { get; }
    public bool Afford { get; }
    public int WaitSec { get; }
    public string Ladder { get; }
}
}

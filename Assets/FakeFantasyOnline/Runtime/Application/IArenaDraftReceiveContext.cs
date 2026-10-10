using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
public interface IArenaDraftReceiveContext
{
    void ReceiveArenaDraft(ArenaDraftStateData state);
}

// Transient typed receive value for the official 1.5.8 NetClient.Dispatch case 0x8060 (jump-table
// index 95, arm64 case 0x24d2cc). No Unity objects and no wire reader cross this boundary; the
// field order is the official read order of the native branch, so nothing here reorders or
// defaults a value the server did not send.
public sealed class ArenaDraftCardData
{
    public ArenaDraftCardData(int skillId, int level, string name, string desc)
    { SkillId = skillId; Level = level; Name = name; Desc = desc; }

    public int SkillId { get; }
    public int Level { get; }
    public string Name { get; }
    public string Desc { get; }
}

public sealed class ArenaDraftStateData
{
    public ArenaDraftStateData(int flag, int round, int total, int sec,
        IReadOnlyList<ArenaDraftCardData> cards, int rerollFree, long rerollCost)
    {
        Flag = flag;
        Round = round;
        Total = total;
        Sec = sec;
        Cards = cards;
        RerollFree = rerollFree;
        RerollCost = rerollCost;
    }

    /// <summary>The leading byte: 0 closes the window, every other value opens it.</summary>
    public int Flag { get; }
    public int Round { get; }
    public int Total { get; }
    public int Sec { get; }
    public IReadOnlyList<ArenaDraftCardData> Cards { get; }
    public int RerollFree { get; }
    public long RerollCost { get; }
}
}

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.Application
{

public interface IWorldArenaResultReceiveOwner
{
    void ReceiveArenaResult(WorldArenaResultState state);
}

public sealed class WorldArenaResultRow
{
    public WorldArenaResultRow(
        string name,
        int rank,
        int kills,
        int damage,
        int taken,
        int surviveSec,
        bool isSelf)
    {
        Name = name;
        Rank = rank;
        Kills = kills;
        Damage = damage;
        Taken = taken;
        SurviveSec = surviveSec;
        IsSelf = isSelf;
    }

    public string Name { get; }
    public int Rank { get; }
    public int Kills { get; }
    public int Damage { get; }
    public int Taken { get; }
    public int SurviveSec { get; }
    public bool IsSelf { get; }
}

public sealed class WorldArenaResultState
{
    public WorldArenaResultState(
        List<WorldArenaResultRow> rows,
        int myRank,
        int players,
        int kills,
        int damage,
        int taken,
        int boxes,
        int surviveSec,
        string build,
        int rewardExp,
        long rewardCoin,
        bool firstWin,
        int pointsGain,
        long pointsTotal,
        string title,
        long toNext,
        int streak,
        int bestStreak,
        int matches,
        int wins,
        int seasonId,
        int seasonRank,
        long seasonPoints,
        int seasonDaysLeft)
    {
        Rows = new ReadOnlyCollection<WorldArenaResultRow>(new List<WorldArenaResultRow>(rows));
        MyRank = myRank;
        Players = players;
        Kills = kills;
        Damage = damage;
        Taken = taken;
        Boxes = boxes;
        SurviveSec = surviveSec;
        Build = build;
        RewardExp = rewardExp;
        RewardCoin = rewardCoin;
        FirstWin = firstWin;
        PointsGain = pointsGain;
        PointsTotal = pointsTotal;
        Title = title;
        ToNext = toNext;
        Streak = streak;
        BestStreak = bestStreak;
        Matches = matches;
        Wins = wins;
        SeasonId = seasonId;
        SeasonRank = seasonRank;
        SeasonPoints = seasonPoints;
        SeasonDaysLeft = seasonDaysLeft;
    }

    public IReadOnlyList<WorldArenaResultRow> Rows { get; }
    public int MyRank { get; }
    public int Players { get; }
    public int Kills { get; }
    public int Damage { get; }
    public int Taken { get; }
    public int Boxes { get; }
    public int SurviveSec { get; }
    public string Build { get; }
    public int RewardExp { get; }
    public long RewardCoin { get; }
    public bool FirstWin { get; }
    public int PointsGain { get; }
    public long PointsTotal { get; }
    public string Title { get; }
    public long ToNext { get; }
    public int Streak { get; }
    public int BestStreak { get; }
    public int Matches { get; }
    public int Wins { get; }
    public int SeasonId { get; }
    public int SeasonRank { get; }
    public long SeasonPoints { get; }
    public int SeasonDaysLeft { get; }
}

}

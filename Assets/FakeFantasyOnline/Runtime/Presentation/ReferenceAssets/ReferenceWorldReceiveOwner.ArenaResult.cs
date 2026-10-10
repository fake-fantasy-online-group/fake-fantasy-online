using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceWorldReceiveOwner : IWorldArenaResultReceiveOwner
{
    // Dispatch 0600077f: exact atlas Row/Data slots, no sorting, clamp or empty-row gate;
    // all rows precede Data publication, one Show call replaces the previous settlement.
    public void ReceiveArenaResult(WorldArenaResultState state)
    {
        var rows = new System.Collections.Generic.List<ArenaResultDlg.Row>(state.Rows.Count);
        foreach (WorldArenaResultRow source in state.Rows)
        {
            var row = new ArenaResultDlg.Row();
            row.name = source.Name;
            row.rank = source.Rank;
            row.kills = source.Kills;
            row.damage = source.Damage;
            row.taken = source.Taken;
            row.surviveSec = source.SurviveSec;
            row.isSelf = source.IsSelf;
            rows.Add(row);
        }
        var data = new ArenaResultDlg.Data();
        data.rows = rows;
        data.myRank = state.MyRank;
        data.players = state.Players;
        data.kills = state.Kills;
        data.damage = state.Damage;
        data.taken = state.Taken;
        data.boxes = state.Boxes;
        data.surviveSec = state.SurviveSec;
        data.build = state.Build;
        data.rewardExp = state.RewardExp;
        data.rewardCoin = state.RewardCoin;
        data.firstWin = state.FirstWin;
        data.pointsGain = state.PointsGain;
        data.pointsTotal = state.PointsTotal;
        data.title = state.Title;
        data.toNext = state.ToNext;
        data.streak = state.Streak;
        data.bestStreak = state.BestStreak;
        data.matches = state.Matches;
        data.wins = state.Wins;
        data.seasonId = state.SeasonId;
        data.seasonRank = state.SeasonRank;
        data.seasonPoints = state.SeasonPoints;
        data.seasonDaysLeft = state.SeasonDaysLeft;
        ArenaResultDlg.Show(data);
    }
}

}

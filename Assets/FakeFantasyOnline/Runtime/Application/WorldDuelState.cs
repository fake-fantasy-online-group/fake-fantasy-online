using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.Application
{
    /// <summary>Immutable mirror of the official DuelDlg.Row value type.</summary>
    public sealed class WorldDuelRowState
    {
        public WorldDuelRowState(int id, string a, string b, int mode, int phase,
            int entryGold, int spectators, bool betsOpen)
        {
            Id = id; A = a ?? string.Empty; B = b ?? string.Empty; Mode = mode;
            Phase = phase; EntryGold = entryGold; Spectators = spectators; BetsOpen = betsOpen;
        }
        public int Id { get; }
        public string A { get; }
        public string B { get; }
        public int Mode { get; }
        public int Phase { get; }
        public int EntryGold { get; }
        public int Spectators { get; }
        public bool BetsOpen { get; }
    }

    /// <summary>Server-authored DuelDlg.SetList state and the current open target.</summary>
    public sealed class WorldDuelState
    {
        public static readonly WorldDuelState Empty = new WorldDuelState(
            false, 0, string.Empty, -1, 0, 0, 0, 0, Array.Empty<WorldDuelRowState>());
        public WorldDuelState(bool visible, int targetId, string targetName, int mode,
            int entryGold, int feePercent, int betMin, int betMax,
            IReadOnlyList<WorldDuelRowState> rows)
        {
            Visible = visible; TargetId = targetId; TargetName = targetName ?? string.Empty;
            Mode = mode; EntryGold = entryGold; FeePercent = feePercent; BetMin = betMin; BetMax = betMax;
            Rows = new ReadOnlyCollection<WorldDuelRowState>(new List<WorldDuelRowState>(rows ?? Array.Empty<WorldDuelRowState>()));
        }
        public bool Visible { get; }
        public int TargetId { get; }
        public string TargetName { get; }
        public int Mode { get; }
        public int EntryGold { get; }
        public int FeePercent { get; }
        public int BetMin { get; }
        public int BetMax { get; }
        public IReadOnlyList<WorldDuelRowState> Rows { get; }
    }

    public interface IDuelStateSource
    {
        WorldDuelState DuelState { get; }
        long DuelStateVersion { get; }
    }
}

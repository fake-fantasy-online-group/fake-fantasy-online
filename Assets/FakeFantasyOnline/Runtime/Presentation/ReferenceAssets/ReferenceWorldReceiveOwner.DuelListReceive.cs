using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : IDuelListReceiveOwner
{
    public void ReceiveDuelList(WorldDuelState state) =>
        ReceiveDuelListForIndependentRuntime(state);

    internal static void ReceiveDuelListForIndependentRuntime(WorldDuelState state)
    {
        var rows = new List<DuelDlg.Row>(state.Rows.Count);
        foreach (WorldDuelRowState row in state.Rows)
            rows.Add(new DuelDlg.Row { id = row.Id, a = row.A, b = row.B, mode = row.Mode,
                phase = row.Phase, entryGold = row.EntryGold, spectators = row.Spectators, betsOpen = row.BetsOpen });
        // SetList owns the ordered selection transition. It uses static state only and must
        // not be replaced by a latest-snapshot write from constructor, Draw or map startup.
        DuelDlg.SetList(state.EntryGold, state.FeePercent, state.BetMin, state.BetMax, rows);
    }
}
}

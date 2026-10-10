using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IArenaRosterReceiveContext
{
    void IArenaRosterReceiveContext.ReceiveArenaRoster(IReadOnlyList<ArenaRosterRowData> rows,
        Dictionary<int, string> descriptions)
    {
        // Reify the native data types without changing order, values or description identity.
        var nativeRows = new List<ArenaRosterDlg.Row>(rows.Count);
        foreach (ArenaRosterRowData data in rows)
        {
            var row = new ArenaRosterDlg.Row
            {
                entityId = data.EntityId,
                name = data.Name,
                alive = data.Alive,
                deaths = data.Deaths
            };
            foreach (ArenaRosterCardData card in data.Cards)
                row.cards.Add(new ArenaRosterDlg.Card
                    { skillId = card.SkillId, lv = card.Level, name = card.Name });
            nativeRows.Add(row);
        }
        ArenaRosterDlg.Set(nativeRows, descriptions);
    }
}
}

using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Only the Application/Presentation type boundary: no state, initialization or UI decisions.
internal static class ReferenceTargetStatusAdapter
{
    // Original 805e: ARM25469c..254808/2564f4..25651c and
    // x862639d5..263b65/265c8e..265cb6. The list has the wire count capacity;
    // each zero-initialized TgSt is filled in read order and added as a value.
    // No immutable observer allocation may precede the native setter.
    internal static void ReceiveIntoNativeOwner(ITargetStatusReader reader)
    {
        int entityId = reader.I32();
        int count = reader.U8();
        var rows = new List<GameHud.TgSt>(count);
        for (int index = 0; index < count; index++)
        {
            GameHud.TgSt row = default;
            row.icon = reader.I32();
            row.good = reader.U8() != 0;
            row.remainMs = reader.I32();
            row.name = reader.Str();
            row.desc = reader.Str();
            rows.Add(row);
        }
        GameHud.SetTargetStatus(entityId, rows);
    }

    // Explicit diagnostic callers retain their existing DTO conversion surface.
    internal static List<GameHud.TgSt> ToNativeRows(IReadOnlyList<WorldTargetStatusRow> rows)
    {
        if (rows == null) return null;
        var list = new List<GameHud.TgSt>(rows.Count);
        for (int index = 0; index < rows.Count; index++)
            list.Add(ToNativeRow(rows[index]));
        return list;
    }

    internal static GameHud.TgSt ToNativeRow(WorldTargetStatusRow row)
    {
        if (row == null) throw new ArgumentNullException(nameof(row));
        return new GameHud.TgSt
        {
            icon = row.IconId,
            good = row.Beneficial,
            remainMs = row.RemainingMilliseconds,
            name = row.DisplayName,
            desc = row.Description
        };
    }
}

}

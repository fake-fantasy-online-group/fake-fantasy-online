using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : ICombatStatusReceiveOwner
{
    int ICombatStatusReceiveOwner.SelfEntityId => NetClient.SelfEntityId;

    public WorldEntityStatusEffect ReceiveEntityStatusEffect(ICombatEntityReader reader, out int entityId)
    {
        // Official 0x801b: I32, Str, U8, I32, then 06000158 with the raw arguments.
        // The current native self/monster/player owners choose the outcome at this receipt.
        entityId = reader.I32();
        string effect = reader.Str();
        int mode = reader.U8();
        int duration = reader.I32();
        CombatWorld.ShowEntityStatus(entityId, effect, mode, duration);
        return new WorldEntityStatusEffect(effect, (byte)mode, duration);
    }

    public IReadOnlyList<WorldEntityStatusIcon> ReceiveEntityStatusIcons(
        ICombatEntityReader reader, out int entityId)
    {
        // Official 0x8064 always creates a native value list with the declared capacity,
        // including count zero. Each row requires 13 remaining bytes; residue is unread.
        entityId = reader.I32();
        int count = reader.U8();
        var icons = new List<MonsterEntity.StatusIcon>(count);
        for (int index = 0; index < count && reader.Remaining >= 13; index++)
            icons.Add(new MonsterEntity.StatusIcon
            {
                status = reader.I32(),
                icon = reader.I32(),
                good = reader.U8() != 0,
                remainMs = reader.I32()
            });
        // SetStatusIcons retains this exact list. No DTO conversion, mirror admission or
        // scene-frame wait can precede the original current-registry lookup or its failure.
        CombatWorld.SetEntityStatusIcons(entityId, icons);

        var observed = new WorldEntityStatusIcon[icons.Count];
        for (int index = 0; index < icons.Count; index++)
        {
            MonsterEntity.StatusIcon row = icons[index];
            observed[index] = new WorldEntityStatusIcon(row.status, row.icon,
                row.good ? (byte)1 : (byte)0, row.remainMs);
        }
        return observed;
    }
}
}

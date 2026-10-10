using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner
{
    public WorldEntityState ReceiveWorldEntity(ICombatEntityReader reader,
        IWorldReceiveContext context, out object identity)
    {
        identity = null;
        int entityId = reader.I32();
        int kind = reader.U8();
        int modelOrSkillId = reader.I32();
        string displayName = reader.Str();
        int level = reader.I32();
        int x = reader.I32();
        int y = reader.I32();
        int hitPointsOrRadiusPixels = reader.I32();
        int maximumHitPoints = reader.I32();
        int ownerEntityId = reader.I32();
        int eliteFlags = reader.U8();

        if (context != null && context.Switching) return null;

        int mapIdGuard = 0;
        if (reader.Remaining >= 4)
        {
            mapIdGuard = reader.I32();
            if (context != null && mapIdGuard > 0 && context.CurrentMapId > 0 &&
                mapIdGuard != context.CurrentMapId) return null;
        }

        int friendlyFlag = reader.Remaining >= 1 ? reader.U8() : 0;
        (string effect, int positionMode, int durationMilliseconds)[] effects = null;
        if (reader.Remaining >= 1)
        {
            int count = reader.U8();
            if (count > 0)
            {
                effects = new (string, int, int)[count];
                for (int index = 0; index < count && reader.Remaining > 0; index++)
                    effects[index] = (reader.Str(), reader.U8(), reader.I32());
            }
        }

        List<MonsterEntity.StatusIcon> icons = null;
        if (reader.Remaining >= 1)
        {
            int count = reader.U8();
            if (count > 0)
            {
                icons = new List<MonsterEntity.StatusIcon>(count);
                for (int index = 0; index < count && reader.Remaining >= 13; index++)
                    icons.Add(new MonsterEntity.StatusIcon
                    {
                        status = reader.I32(),
                        icon = reader.I32(),
                        good = reader.U8() != 0,
                        remainMs = reader.I32()
                    });
            }
        }

        // Both original collections exist before kind/Spawn admission, including traps,
        // duplicate ids and hidden pets. The exact decoded icon list reaches its native owner.
        if (kind == 5)
            CombatWorld.SpawnTrap(entityId, modelOrSkillId, x, y, hitPointsOrRadiusPixels);
        else
        {
            CombatWorld.Spawn(entityId, modelOrSkillId, displayName, level, x, y,
                hitPointsOrRadiusPixels, maximumHitPoints, kind, ownerEntityId,
                eliteFlags, friendlyFlag);
            if (effects != null)
                foreach (var effect in effects)
                    if (!string.IsNullOrEmpty(effect.effect) && effect.durationMilliseconds > 0)
                        CombatWorld.ShowEntityStatus(entityId, effect.effect,
                            effect.positionMode, effect.durationMilliseconds);
            if (icons != null) CombatWorld.SetEntityStatusIcons(entityId, icons);
            identity = CombatWorld.MonsterForIndependentRuntime(entityId);
        }

        // Optional immutable observations start after all native calls. Their allocation or
        // normalization cannot reject a native argument, alter its list, or replay its effects.
        WorldEntityStatusEffect[] observedEffects = effects == null
            ? Array.Empty<WorldEntityStatusEffect>()
            : new WorldEntityStatusEffect[effects.Length];
        if (effects != null)
            for (int index = 0; index < effects.Length; index++)
                observedEffects[index] = new WorldEntityStatusEffect(effects[index].effect,
                    (byte)effects[index].positionMode, effects[index].durationMilliseconds);

        WorldEntityStatusIcon[] observedIcons = icons == null
            ? null
            : new WorldEntityStatusIcon[icons.Count];
        if (icons != null)
            for (int index = 0; index < icons.Count; index++)
            {
                MonsterEntity.StatusIcon row = icons[index];
                observedIcons[index] = new WorldEntityStatusIcon(row.status, row.icon,
                    row.good ? (byte)1 : (byte)0, row.remainMs);
            }
        return new WorldEntityState(entityId, (WorldEntityKind)kind, modelOrSkillId,
            displayName, level, x, y, hitPointsOrRadiusPixels, maximumHitPoints,
            ownerEntityId, (byte)eliteFlags, mapIdGuard, (byte)friendlyFlag,
            observedEffects, observedIcons);
    }
}
}

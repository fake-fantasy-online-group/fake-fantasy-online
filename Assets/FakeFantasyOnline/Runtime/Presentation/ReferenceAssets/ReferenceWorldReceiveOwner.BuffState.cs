using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : IWorldBuffReceiveOwner
{
    // Zero is missing artwork metadata, not an instruction to erase a known mapping.
    // Keep the wire identity distinct from the resource id; never pass skillId as iconId.
    internal static int ResolveBuffIcon(int skillId, string name, int iconId)
    {
        if (iconId > 0) return iconId;
        if (CharData.buffs != null)
            foreach (CharData.BuffView previous in CharData.buffs)
                if (previous.skillId == skillId && previous.name == name && previous.iconId > 0)
                    return previous.iconId;
        return skillId > 0 ? Win05.SkillIconId(skillId) : 0;
    }

    public bool ReceiveBuffState(IWorldBuffReader reader, Func<float> clock, Func<bool> customMap)
    {
        int count = reader.U16();
        // ARM2512fc..251314 / x8625ff41..25ff5d: allocation precedes the map probe.
        var rows = new List<CharData.BuffView>(count);
        bool isCustomMap = customMap != null
            ? customMap() : CustomMaps.IsCustom(GameBootstrap.CurMapId);
        bool invisible = false;
        byte immobile = 0;
        for (int index = 0; index < count; index++)
        {
            // The original object exists before its first read, even for a later dropped row.
            var row = new CharData.BuffView();
            row.skillId = reader.I32();
            row.iconId = reader.I32();
            row.remainSec = reader.I32();
            row.name = reader.Str();
            row.desc = reader.Str();
            row.iconId = ResolveBuffIcon(row.skillId, row.name, row.iconId);
            row.good = reader.U8() != 0;
            row.posMode = unchecked((sbyte)(byte)reader.U8());
            row.recvTime = clock != null ? clock() : Time.realtimeSinceStartup;
            bool rowInvisible = reader.U8() != 0;
            byte rowImmobile = (byte)reader.U8();
            invisible |= rowInvisible;
            if (rowImmobile > immobile) immobile = rowImmobile;
            if (isCustomMap || unchecked((uint)(row.skillId - 90000)) >= 10000u)
                rows.Add(row);
        }

        // Original publication order is list, two aggregates, optional tail read, doubleExp.
        CharData.buffs = rows;
        CharData.invisible = invisible;
        CharData.immobile = immobile;
        bool doubleExperience = false;
        if (reader.Remaining >= 1) doubleExperience = reader.U8() != 0;
        CharData.doubleExp = doubleExperience;
        return count != 0 || doubleExperience;
    }
}
}

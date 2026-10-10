using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class SuitFxDB
{
    [Serializable] private class GlowFile { public int[] models; public int[] premul_max; }

    private static Dictionary<int, Item> _byId;
    private static Spec[] _specs;
    private static HashSet<int> _fuseIds;
    private static HashSet<int> _soulIds;
    private static Dictionary<int, string> _soulFx;
    private static Dictionary<int, string> _dragon;
    private static HashSet<int> _dualModels;
    private static HashSet<int> _glowPremul;
    private static HashSet<int> _glowModels;

    private static string LoadTable(string name) => GameTables.LoadText(name);

    private static void EnsureLoaded()
    {
        if (_byId != null) return;
        _byId = new Dictionary<int, Item>();
        _specs = new Spec[0];
        _fuseIds = new HashSet<int>();
        string text = LoadTable("suit_fx");
        if (text != null)
        {
            JFile file = JsonUtility.FromJson<JFile>(text);
            if (file != null)
            {
                if (file.items != null) foreach (Item item in file.items) _byId[item.id] = item;
                if (file.specs != null) _specs = file.specs;
            }
        }
        else Debug.LogWarning("[SuitFx] suit_fx.json 缺失(StreamingAssets/gameassets/tables/ 与 Resources 都没找到)");

        text = LoadTable("avatar_fuse");
        if (text != null)
        {
            FuseFile file = JsonUtility.FromJson<FuseFile>(text);
            if (file != null && file.items != null) foreach (FuseItem item in file.items) _fuseIds.Add(item.id);
        }
        _soulIds = new HashSet<int>();
        _soulFx = new Dictionary<int, string>();
        text = LoadTable("soul_fx");
        if (text != null)
        {
            foreach (Match match in Regex.Matches(text, @"""(\d+)""\s*:\s*""([^""]*)"""))
            {
                int id = int.Parse(match.Groups[1].Value);
                _soulIds.Add(id);
                _soulFx[id] = match.Groups[2].Value;
            }
        }
        _dragon = new Dictionary<int, string>();
        text = LoadTable("dragon_fuse");
        if (text != null)
        {
            DragonFile file = JsonUtility.FromJson<DragonFile>(text);
            if (file != null && file.items != null) foreach (DragonItem item in file.items) _dragon[item.id] = item.slot;
        }
        _dualModels = new HashSet<int>();
        text = LoadTable("weapon_dual");
        if (text != null)
        {
            DualFile file = JsonUtility.FromJson<DualFile>(text);
            if (file != null && file.models != null) foreach (int model in file.models) _dualModels.Add(model);
        }
        _glowModels = new HashSet<int>();
        _glowPremul = new HashSet<int>();
        text = LoadTable("weapon_glow");
        if (text == null) return;
        GlowFile glow = JsonUtility.FromJson<GlowFile>(text);
        if (glow == null) return;
        if (glow.models != null) foreach (int model in glow.models) _glowModels.Add(model);
        if (glow.premul_max != null) foreach (int model in glow.premul_max) _glowPremul.Add(model);
    }

    // Official 1.5.8 0x06000ddc: load before checking the model and shared table.
    public static bool IsDualWeaponModel(int model)
    {
        EnsureLoaded();
        return model > 0 && _dualModels != null && _dualModels.Contains(model);
    }

    // Official 1.5.8 0x06000ddd: loading precedes even the nonpositive-model gate.
    public static bool IsGlowWeaponModel(int model)
    {
        EnsureLoaded();
        return model > 0 && _glowModels != null && _glowModels.Contains(model);
    }

    // Official 1.5.8 0x06000de1.
    public static int GlowTableCount
    {
        get
        {
            EnsureLoaded();
            return _glowModels == null ? 0 : _glowModels.Count;
        }
    }

    public static bool IsGlowPremulModel(int model)
    {
        EnsureLoaded();
        return model > 0 && _glowPremul != null && _glowPremul.Contains(model);
    }
}

}
